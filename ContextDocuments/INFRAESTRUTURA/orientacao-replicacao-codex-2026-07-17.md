# Orientação ao Claude — redesenho seguro da replicação do ErpPharma

**Destinatário:** Claude, responsável pela próxima etapa de implementação  
**Data da revisão:** 17/07/2026  
**Projeto auditado:** `C:\repositorios\ErpPharma`  
**Baseline auditada:** branch `dev-pc1`, commit `a96336f5bdf28ef3028ea7a50b654027c1a9352a`  
**Observação:** os arquivos centrais do sync auditados estão sem diferença em relação a `origin/main` nesta baseline.

## 1. Parecer executivo

A topologia em estrela e o outbox transacional já construídos são uma base aproveitável. A implementação atual, porém, **não deve ser ativada com um segundo nó real**. Há falhas P0 que permitem perda silenciosa, divergência permanente, mistura de dados entre filiais e uso indevido das rotas de sincronização.

O problema não deve ser tratado como “replicar todas as tabelas em todas as direções”. O ERP precisa de **sincronização de domínio com autoridade por tipo de dado**:

- cadastros compartilhados podem ser disseminados a todos;
- dados operacionais pertencem a uma filial e normalmente só devem existir no nó dono e na nuvem;
- estoque, caixa, banco e fiscal não podem usar LWW de snapshots mutáveis;
- infraestrutura, cursores, credenciais e certificados nunca devem entrar no sync genérico;
- receber uma cópia de um dado não significa ter autoridade para alterá-lo.

Minha recomendação é implementar um **Sync V2 atrás de configuração explícita**, preservando as partes boas do código atual, mas não tentar tornar o mecanismo existente produtivo apenas com correções pontuais. Segurança, cursor, identidade, conflito, bootstrap e contratos de agregado precisam nascer como um protocolo coerente e testado em PostgreSQL real.

## 2. Escopo e método da auditoria

Foram inspecionados:

- os 44 arquivos Markdown do repositório, fora de dependências e artefatos gerados;
- os textos arquiteturais pertinentes (`sinc.txt`, `instruções.txt`, `atualização de preços.txt` e a parte aplicável de `produtos.txt`);
- o modelo EF Core, suas 134 propriedades `DbSet`, relacionamentos e classificação atual;
- `SyncController`, `SyncBackgroundService`, `SyncApplicator`, `AppDbContext`, entidades de sync, migrations, autenticação, seeder e sequências;
- serviços que removem e recriam coleções filhas;
- regras de produto, filial, fiscal, venda, estoque, caixa e configurações;
- estado do Git, sem modificar arquivos existentes do usuário.

Os achados abaixo distinguem:

- **evidência do projeto:** acompanhada de arquivo e linha;
- **inferência arquitetural:** explicitamente indicada;
- **recomendação:** baseada nas garantias documentadas do PostgreSQL/.NET e em padrões de sistemas distribuídos citados ao final.

## 3. Premissas que precisam ficar explícitas

### 3.1 Unidade de tenant

Os documentos atuais pressupõem **um banco de dados na nuvem por cliente/tenant** (`synAteAqui.md:39,50`). Esta revisão adota essa premissa.

Se a intenção for colocar clientes diferentes no mesmo banco, pare: o modelo atual não tem isolamento de tenant suficiente para isso. Nesse cenário, `TenantId` teria de participar de todas as entidades, chaves, queries, envelopes, credenciais e políticas de acesso. Não é uma alteração pequena do sync.

### 3.2 O que “alta disponibilidade” significa aqui

Servidor local + banco em nuvem fornece principalmente **continuidade durante queda de internet**. Isso, sozinho, não torna o ERP altamente disponível contra falha do computador local.

Se o navegador puder escrever na nuvem enquanto o servidor local também continua aceitando vendas offline, existe split-brain. Sob partição de rede não é possível garantir, ao mesmo tempo, disponibilidade de escrita em ambos os lados e consistência imediata. Essa é a consequência prática formalizada pelo resultado de Gilbert e Lynch sobre consistência, disponibilidade e partições.

Decisão recomendada:

- o nó local é autoridade das transações operacionais da filial;
- a nuvem pode editar cadastros globais e consultar consolidados;
- failover de escrita operacional para a nuvem deve ser **manual e cercado por epoch/fencing**;
- depois do takeover, o nó antigo não volta a escrever nem simplesmente “sincroniza o que ficou”; ele precisa ser reconciliado/rebootstrapado;
- fiscal, caixa e estoque mantêm uma única autoridade ativa por filial.

Replicação também não substitui backup, PITR, monitoramento, UPS, hardware redundante, roteamento de failover nem definição de RPO/RTO.

### 3.3 Topologia recomendada

Manter estrela:

```text
                         Banco/Hub do cliente
                                  nó 0
                            /       |       \
                           /        |        \
                    edge filial A  edge B   edge C
                         nó 1       nó 2     nó 3
```

As folhas não conversam diretamente. O hub autentica, valida, persiste, publica e acompanha ACK. Isso reduz combinações de conexão e fornece um ponto de observabilidade, mas **não transforma o hub em vencedor automático de todo conflito**.

### 3.4 Decisão confirmada pelo dono: sincronização gerenciada pela aplicação

O mecanismo nativo de replicação do PostgreSQL **não será usado como protocolo funcional do ERP**. Esta é uma decisão intencional, não uma pendência arquitetural.

Motivos confirmados pelo dono do produto:

- topologia e filiais atendidas precisam ser configuráveis dinamicamente;
- cada tabela/agregado possui escopo, autoridade e regra de conflito próprios;
- o sistema precisa exibir no painel o que ainda está pendente, o que foi aceito pelo hub, aplicado, publicado, entregue e aplicado em cada nó;
- erros precisam ser visíveis, reprocessáveis e auditáveis;
- é necessário consultar fila, quarentena, conflitos, tentativas, atraso e backlog;
- operadores precisam suspender nó, reprocessar evento, executar bootstrap e acompanhar recuperação sem acessar diretamente o PostgreSQL.

Portanto o PostgreSQL permanece responsável por transações, constraints, locks e durabilidade, enquanto o **protocolo de sincronização é da aplicação**. Ele deve ser tratado como um subsistema de mensageria/replicação de domínio, não como cópia genérica de linhas.

O painel não pode se apoiar em um booleano `Enviado`. Cada `EventId` precisa de uma máquina de estados persistida, por exemplo:

```text
PendingLocal
  -> AcceptedByHub | Rejected | DeadLetter
  -> AppliedAtHub | Conflict
  -> Published
  -> DeliveredToNode
  -> ReceivedByNode
  -> AppliedAtNode | NodeConflict
```

Esses estados não devem ser inferidos de logs ou variáveis estáticas. O painel consulta outbox, inbox, evento canônico, entregas por nó, ACKs e conflitos persistidos. Deve mostrar ao menos:

- quantidade e idade do backlog local e central;
- último push/pull/ACK com sucesso por nó;
- lag entre `PublishedSeq` e `DeliverySeq` recebido;
- eventos em retry, conflito, rejeição e dead-letter;
- tentativas, próximo retry e erro estruturado;
- versão do app/schema/protocolo e heartbeat do nó;
- progresso de bootstrap/rebootstrap;
- filtros por filial, nó, agregado, `EventId`, período e estado;
- ações autorizadas de retry, resolução e suspensão, sempre auditadas.

Separar dois planos:

- **data plane:** push, aplicação, publicação, pull e ACK, acessível apenas por identidade de nó;
- **control plane:** painel, cadastro de nós, políticas, conflitos e operações administrativas, acessível por usuários autorizados e sem expor payload sensível por padrão.

As referências à logical replication do PostgreSQL neste documento servem somente para demonstrar por que ela não resolve automaticamente conflito, sequence, DDL e regras de domínio. Elas **não são recomendação de substituição** do mecanismo gerenciado pela aplicação.

## 4. O que já está correto e deve ser preservado

1. **Topologia estrela** edge → hub → destinatários.
2. **Outbox no mesmo commit da mutação de negócio** para alterações EF usuais (`AppDbContext.cs:2316-2391`). Essa é a garantia central do transactional outbox.
3. **`OpUid` como identidade da operação**, em vez do `Id` reciclável da fila.
4. **`AplicandoSync` para evitar eco** ao aplicar uma operação remota (`AppDbContext.cs:2272-2278`).
5. **Quarentena e retry de dependências** como conceitos operacionais.
6. **Tombstone** como conceito anti-ressurreição, desde que passe a usar a mesma versão do estado e retenção por ACK.
7. **Exclusão de sequência fiscal do sync genérico**.
8. A percepção, já registrada em `synAteAqui.md`, de que `Id > ultimoId`, reconciliação cega de filhos e `MIN(cursor)` não são seguros.

Essas peças são fundações; não são, isoladamente, uma garantia de convergência.

## 5. Bloqueadores P0 encontrados no código atual

### P0.1 — Qualquer usuário autenticado alcança o plano de dados do sync

**Evidência:** `SyncController` usa somente `[Authorize]` (`SyncController.cs:13-16`). Sem policy de nó, um JWT humano autenticado pode, em princípio:

- enviar tabela, operação, JSON, timestamp, origem e filial dona arbitrários em `/api/sync/enviar` (`:31-145`);
- informar `filialId` e uma lista de `filiais` no GET e baixar escopo que não pertence ao chamador (`:157-203`);
- consultar `/fila`, que inclui `DadosJson` integral (`:261-297`);
- reprocessar, resetar cursor e limpar dados (`:310-440`).

O applicator contém tabelas como usuários. O payload pode conter PII e hashes. Portanto isso não é apenas uma fragilidade de painel: é uma fronteira de autorização quebrada.

**Correção obrigatória:**

- data plane do sync com esquema/policy exclusivo, por exemplo `ReplicationNode`;
- identidade de máquina, não de usuário;
- claims de tenant, `NodeInstanceId`, role, schema/protocolo e filiais autorizadas;
- origem e destinatários derivados pelo servidor, nunca confiados do body/query;
- painel humano com policy separada, DTOs redigidos e sem payload bruto;
- limites de lote por quantidade e bytes.

### P0.2 — O usuário virtual `SISTEMA` amplia o comprometimento

**Evidência:** a senha diária possui oito caracteres hexadecimais derivados de data + segredo compartilhado e gera token admin (`AuthService.cs:187-226`). Existe endpoint que devolve essa senha mediante chave em query string (`AuthController.cs:74-93`). Há credenciais e chaves com defaults versionados em `appsettings.json:3,6,32-33`.

Um edge comprometido conhece o segredo compartilhado e pode obter poder administrativo na API do hub. Segredo em Git deve ser tratado como comprometido, mesmo que o repositório seja privado.

**Correção obrigatória:**

- rotacionar senha do banco, JWT secret, `SistemaKey` e `SistemaApiKey`;
- remover valores reais do Git e do histórico operacional; usar secret store/variáveis;
- remover o endpoint de revelação da senha;
- credencial única e revogável por instância de nó;
- preferir mTLS ou client assertion assimétrica; o token emitido só autoriza rotas de replicação;
- um edge nunca recebe token de administrador humano.

### P0.3 — Origem do nó e filial dona estão misturadas

**Evidência:** no hub, o código calcula:

```csharp
var noOrigem = _noCodigo > 0 ? _noCodigo : GetFilialIdFromContext();
```

`AppDbContext.cs:2280-2282`.

Em updates, `NoOrigemId` da entidade não é atualizado; a operação volta a usar esse valor (`AppDbContext.cs:2291-2307,2331-2333`). O PULL exclui `NoOrigemId == filialId` (`SyncController.cs:168-170`).

Caso concreto: um registro criado no nó 1 e alterado posteriormente na nuvem pode continuar parecendo originado no nó 1; o anti-eco então impede que a alteração da nuvem chegue justamente ao nó 1.

**Correção:** separar campos e conceitos:

- `CreatedByNodeId` — histórico de criação, se necessário;
- `LastWriterNodeId` — escritor da versão atual;
- `OriginNodeInstanceId` — emissor imutável do evento;
- `OwnerBranchId` — proprietário do dado;
- `Recipients`/resolver — quem deve receber.

O nó do hub permanece nó 0. Claim de filial nunca substitui identidade do nó.

### P0.4 — O cursor `SyncFila.Id` perde operações

**Evidência:** o PULL usa `Id > ultimoId`, ordena pelo identity e avança o cursor (`SyncController.cs:168-202`; `SyncBackgroundService.cs:270-286`).

Caso determinístico:

1. transação A reserva `Id=100` e fica aberta;
2. transação B reserva `Id=101` e commita;
3. o nó recebe 101 e grava cursor 101;
4. A commita;
5. 100 nunca mais satisfaz `Id > 101`.

O PostgreSQL documenta que sequences são não transacionais: valores não são devolvidos no rollback e a alocação não representa ordem de commit. `CreatedAt`, janela de segurança ou `MAX(Id)` não consertam essa prova.

**Correção:** `PublishedSeq` atribuído apenas a eventos canônicos já aplicados e commitados por um publicador exclusivo. Ele é o corte global de snapshot/auditoria. Na mesma transação de publish, o hub materializa uma entrega com `DeliverySeq` contíguo para cada `(NodeInstanceId, SubscriptionGeneration)` destinatário. O cursor do edge usa `DeliverySeq`, nunca o identity do outbox nem o `PublishedSeq` global esparso.

Invariante exigida:

> Todo evento canônico elegível recebe eventualmente um `PublishedSeq` único e imutável; somente depois disso são criadas entregas imutáveis aos destinatários da geração vigente. Um commit tardio pode legitimamente receber sequência maior. Nenhum evento sem aplicação canônica commitada pode ser servido.

Implementação aceitável:

- worker exclusivo no hub;
- `pg_advisory_xact_lock` ou linha de controle com lock transacional;
- selecionar resultados canônicos `Applied/Resolved` ainda não publicados;
- atribuir sequências sob o mesmo lock;
- commit antes de outro publicador numerar o próximo conjunto;
- servir apenas `PublishedSeq IS NOT NULL`.

`nextval` pode ter buracos; isso é aceitável para cursor `>`. O lock de publicador é o que impede inversão entre publicadores. Não numerar na transação de negócio.

Advisory lock só coordena sessões no mesmo primary PostgreSQL. Ele não impede dois primaries do hub em split-brain; promoção/restore do hub exige fencing de infraestrutura e nova `HubLogGenerationId`.

Se o hub normalizou payload, resolveu conflito ou atribuiu revisão canônica, o conteúdo publicado é esse resultado canônico, não a proposta bruta recebida do edge.

### P0.5 — O LWW não é atômico e usa o nó errado no desempate

**Evidência:** `AplicarUpdateComLww` faz SELECT, compara em memória e depois `SetValues/SaveChanges` sem concurrency token, predicado CAS ou lock (`SyncApplicator.cs:274-286`). Dois pushes concorrentes podem ler a mesma versão antiga; o update mais velho pode commitar por último e sobrescrever o mais novo.

O desempate usa `entidade.NoOrigemId`, que é preenchido apenas no INSERT (`AppDbContext.cs:2291-2294`). Logo ele representa normalmente o criador da linha. O escritor real da operação está em `SyncFila.NoOrigemId` (`:2368`), mas não participa do comparador.

**Correção:** uma versão explícita e atômica, por exemplo `(HlcPhysical, HlcLogical, LastWriterNodeId)`, aplicada por CAS/upsert condicional no banco. O mesmo comparador precisa governar row e tombstone na mesma transação.

HLC resolve ordenação causal/determinística melhor que relógio de parede; não decide sozinho qual alteração faz sentido para o negócio.

### P0.6 — UPDATE e DELETE não convergem quando chegam em ordens diferentes

**Evidência:** DELETE remove e grava tombstone (`SyncApplicator.cs:46-59`). Se depois chegar um UPDATE mais novo, o registro está ausente e qualquer operação `U` retorna `PrecisaRetry` (`:105-120`).

Em outro nó, se o UPDATE chegar primeiro, um DELETE mais velho será `Stale`. Resultado: um nó fica vivo e outro morto indefinidamente.

**Correção:** modelar upsert/delete como versões do mesmo estado replicado. Se a política for LWW:

- upsert mais novo que tombstone pode recriar;
- tombstone mais novo que row remove;
- empates usam o escritor real;
- a decisão é uma operação atômica.

Se o domínio escolher delete-wins, a regra deve ser explícita e testada em todas as permutações. Não deixar o resultado depender de o envelope dizer `I` ou `U`.

### P0.7 — Agregados com filhos POCO duplicam e ressuscitam itens

**Evidência:** o applicator apenas insere/atualiza filhos por ID; não remove ausentes (`SyncApplicator.cs:150-213`). A captura varre apenas `Entries<BaseEntity>` (`AppDbContext.cs:2287-2313`). Uma alteração somente no filho pode nem gerar evento se o pai permanecer `Unchanged`.

Ao mesmo tempo, serviços removem todos os filhos e recriam a coleção, entre eles:

- `ClienteService.cs:181-197`;
- `ConvenioService.cs:218-233`;
- `PromocaoService.cs:185-200`;
- `HierarquiaComissaoService.cs:104-106`;
- `HierarquiaDescontoService.cs:112-116`;
- `VendaService.cs:203-209`;
- `SelfCheckoutVendaService.cs:249-255`;
- `AdquirenteService.cs:79-82`.

Um nó cria IDs novos; o outro preserva os antigos que “sumiram” do JSON e adiciona os novos. Depois pode devolver ambos à origem.

**Correção:** contrato versionado de agregado:

```text
AggregateGlobalId
AggregateVersion
Collections:
  - Name
  - Authoritative: true|false
  - Items com identidade estável
```

- coleção ausente = não carregada, preservar;
- coleção presente e autoritativa vazia = remover todos;
- somente coleção autoritativa pode executar delete-missing;
- pai + filhos + estado de versão são aplicados em uma transação;
- serviços aplicam diff e preservam IDs de filhos;
- mudança de filho incrementa/toca a versão do agregado.

### P0.8 — Classificação de escopo falha aberta

**Evidência:** tipo não classificado ou owner não resolvido retorna `null`, e `null` significa GLOBAL (`AppDbContext.cs:2140-2163`). Uma tabela futura de caixa esquecida no dicionário pode ser enviada a todas as filiais.

**Correção:** registry exaustivo e validado no startup. “Não sei a filial” deve falhar/quarentenar, nunca virar global. Captura e applicator devem consumir a mesma fonte de verdade.

### P0.9 — Produto mistura escopo da linha, autoridade e comando de propagação

`classificacao-replicacao.md:47-50` declara `ProdutoDados`, `ProdutoFiscal` e `ProdutoFornecedor` como POR-FILIAL. Já `docs/specs/cadastro-produto.md:10-25,31-45` determina que preços, fiscal e fornecedores sejam copiados para todas as filiais. Isso não é logicamente impossível: uma linha pode continuar pertencendo à filial B e ter sido criada por um comando de propagação. O código/protocolo atual, porém, não representa essa diferença.

No roteamento atual, um edge A não recebe linhas das outras filiais e não deveria escrever diretamente uma linha cujo owner é B. Portanto ele não tem visão/autoridade suficiente para fazer fan-out seguro: pode criar duplicata ou sobrescrever estado invisível.

Há também autoridades diferentes dentro da mesma linha: `ProdutoDados` mistura estoque, custo médio/última compra, preço, promoção, localização e flags (`ProdutoDados.cs:16-91`). `CopiarDadosSemEstoque` copia inclusive custos e localização para outras filiais (`ProdutoService.cs:576-602`). Um registry apenas por tabela não consegue tornar essa linha segura.

**Modelagem recomendada:**

- `Produto` e atributos realmente uniformes: GLOBAL;
- política comercial uniforme: GLOBAL ou template global;
- estoque, mínimos, máximos e localização: `ProdutoEstoqueFilial`, owner-write;
- custo/estatística comercial por filial: estrutura própria; não copiar implicitamente junto do preço;
- fiscal: perfil global + override por filial/UF/regime quando necessário;
- fornecedor global versus custo/última compra por filial: separar campos;
- propagação para todas as filiais é um **comando de negócio idempotente no hub**, com lista de campos, destinos, versão e vigência; não é um efeito colateral do transporte.

Não codificar essa parte antes de o dono confirmar quais campos podem variar por filial.

### P0.10 — Identidade de nó/PK não possui alocação permanente e pode reciclar IDs

Não existe registro autoritativo de nós nem detecção de duas instalações com o mesmo `No:Codigo`. `SyncGuid` tem índice não unique (`AppDbContext.cs:1864-1869`). Se a mesma PK já existe, o applicator entra no caminho de update sem validar que o `SyncGuid` é o mesmo (`SyncApplicator.cs:108-110`).

`ConfigurarSequences` decide `RESTART` olhando somente `MAX(Id)` (`DatabaseSeeder.cs:427-437`). Se os maiores registros forem apagados, um boot pode fazer a sequência voltar e reutilizar uma PK já replicada/tombstonada.

**Decisão de compatibilidade recomendada para o V2:** manter `long Id` global por faixa como PK/FK, com o mesmo valor em todos os bancos. `SyncGuid` passa a ser guard imutável `NOT NULL UNIQUE` por tabela. Qualquer par `Id ↔ SyncGuid` divergente é colisão P0 e deve ser rejeitado; não executar `SetValues`. Migrar todas as FKs para GUID canônico seria outro projeto e não deve ficar implícito nesta correção.

**Correção:**

- cadastro de nós com `NodeInstanceUuid`, `NodeCode`, geração/incarnation e status;
- `NodeCode` alocado permanentemente, único e nunca reutilizado; lease/epoch pertence à credencial/autoridade, não ao número;
- sequence nunca diminui;
- limitar a **sequence geradora local** à faixa inferior/superior do nó, com alerta de exaustão; a tabela precisa aceitar IDs remotos inseridos explicitamente;
- `SyncGuid` obrigatório e unique nas entidades replicáveis;
- validar `envelope.EntityId == payload.Id` e identidade global antes de update;
- clone/restauração de instalação exige nova incarnation e protocolo de reentrada.

### P0.11 — Não existe bootstrap consistente nem mudança segura de assinatura

O cursor local sozinho não cria um nó. Alterar `No:Filiais` depois de o cursor avançar faz o nó deixar para trás eventos antigos que passaram a ser elegíveis. Também não há tratamento de restore antigo ou nó que volta abaixo do watermark compactado.

**Correção inicial prática e verificável:** bootstrap em janela de manutenção:

> Para fotografia `S` e corte global `C`, toda mutação ausente de `S` terá evento canônico com `PublishedSeq > C`; todo evento com `PublishedSeq <= C` já está representado em `S`. Replay acima de `C` precisa ser stale/idempotente.

1. registrar nó como `Bootstrapping`;
2. criar retention pin para a nova subscription generation;
3. pausar writers do tenant/filiais envolvidos;
4. drenar ingress/applier e publicar todos os eventos aplicados;
5. registrar o watermark `SnapshotPublishedSeq`;
6. tirar uma fotografia consistente que inclua dados, revisões canônicas, fences/tombstones e aliases exatamente nesse estado, excluindo outbox/inbox/credenciais locais;
7. produzir pacote escopado com subscription generation, schema/protocol version e checksum;
8. restaurar em banco vazio com nova incarnation e validar contagens/hashes do mesmo corte;
9. materializar/backfillar entregas posteriores ao corte, iniciar o stream e liberar os writers;
10. ativar atomicamente a subscription generation somente depois do catch-up.

Bootstrap online pode vir depois, mas exige snapshot `REPEATABLE READ`/exportado coordenado com uma barreira do publisher. Deve ser provado que todo evento `<= watermark` já tem efeito visível na fotografia e que toda mutação fora da fotografia será publicada `> watermark`. “Ler as tabelas e depois salvar o MAX” não atende essa invariante.

Mudança de filiais atendidas gera nova `SubscriptionGeneration` e exige backfill/bootstrap. Se o cursor estiver abaixo do watermark retido, responder `RebootstrapRequired`; nunca devolver um lote parcial como se estivesse íntegro.

### P0.12 — Seeds e correções de startup não são seguros para múltiplos nós

Achados concretos em `DatabaseSeeder.cs`:

- SQL bruto cria `ProdutosFiscal` com `FilialId = 1` em todos os deployments (`:31-44`), fora da captura do ChangeTracker/outbox;
- tipos de pagamento são criados em faixas diferentes enquanto `AplicandoSync=true` (`:281-297`) e o enfileiramento manual posterior envia apenas filial e usuário (`:299-348`); vendas podem referenciar um tipo inexistente no hub;
- os 27 registros `IcmsUf` são criados depois que `AplicandoSync=false` (`:350-369`), então edges diferentes podem publicar o mesmo UF com IDs/`SyncGuid`s distintos e colidir na chave natural.

**Correção:** seeds replicáveis têm IDs globais determinísticos ou são hub-authoritative e distribuídos por dataset versionado. Correções SQL de startup precisam ser migrations idempotentes e conscientes de escopo; não podem criar dados de uma filial fixa em cada nó.

### P0.13 — Não há suíte de testes de replicação

Não foi encontrado projeto de testes backend ou harness versionado para sync. Um protocolo distribuído não pode ser validado apenas com dois PCs e inspeção visual. Os cenários de commit invertido, duplicação, reordenação e crash precisam ser determinísticos e automatizados.

### P0.14 — O PUSH confirma e espalha operações que não foram aceitas pelo domínio

**Evidência:** `/enviar` devolve HTTP 200 mesmo quando `errosDb > 0` e depois enfileira todas as operações, inclusive as que entraram em conflito/quarentena (`SyncController.cs:55-145`). No edge, qualquer 2xx faz o lote inteiro virar `Enviado=true` (`SyncBackgroundService.cs:193-205`). A origem abandona sua cópia pendente e um estado ainda não aceito pode ser distribuído a outros nós.

**Correção V2:**

- `accepted` significa somente “`EventId` persistido duravelmente no inbox do hub”;
- a resposta é por `EventId`; o edge marca apenas `accepted`/`duplicate`;
- erro transitório permanece pendente na origem;
- rejeição terminal vai para dead-letter local e conflito central, ambos com diagnóstico e ação humana; nunca desaparece como sucesso;
- publisher só publica eventos `Applied` ou resoluções explícitas, jamais `Received`, `Conflict` ou `Rejected`.

### P0.15 — Algumas operações locais não formam uma transação de domínio única

Replicação confiável não conserta um estado local parcialmente commitado. `VendaService.FinalizarAsync` executa validação/baixa de estoque, venda, caixa e outros saves sem uma transação única (`VendaService.cs:289-630`). Fluxos de suprimento/sangria também separam commits de caixa e banco (`CaixaMovimentoService.cs:131-146,311-329`). Crash ou concorrência podem produzir um fato parcial, e o outbox apenas replicará essa parcialidade corretamente.

Antes de publicar esses domínios, a operação local precisa ter uma fronteira transacional clara: dados, movimentos e outbox commitam juntos; chamadas externas viram saga/outbox posterior; compensações são idempotentes.

## 6. Riscos P1 que devem entrar no mesmo programa de trabalho

1. `Sync:Habilitado=false` para o background, mas o `AppDbContext` continua gerando outbox. Cliente cloud-only acumula fila indefinidamente (`SyncBackgroundService.cs:70-74`; `AppDbContext.cs:2270-2377`).
2. HTTP não-2xx em push/pull é apenas logado/ignorado; o ciclo pode zerar falhas e mostrar `OK` (`SyncBackgroundService.cs:97-104,193-225`).
3. A quarentena agrupa por `(Tabela, RegistroId, Operacao)` e sobrescreve payload/timestamp de versões distintas (`SyncApplicator.cs:312-363`). Deve usar `EventId/OpUid` e preservar histórico.
4. O hub redistribui até operações que entraram em conflito/quarentena (`SyncController.cs:55-145`). Um conflito natural pode se espalhar antes de ser resolvido.
5. A deduplicação no hub ocorre depois da aplicação e usa check-then-insert. Requests concorrentes do mesmo `OpUid` podem colidir; a reserva idempotente precisa ser atômica antes do efeito.
6. Tombstones são apagados após 90 dias, independentemente de ACK (`SyncApplicator.cs:219-258`). Um nó offline/restaurado após esse período pode ressuscitar dados.
7. A fila central não é limpa: a limpeza exige `Enviado=true`, mas linhas de redistribuição do hub permanecem `false` (`SyncController.cs:411-440`). Isso retém JSON sensível indefinidamente.
8. O pull não devolve `OpUid/EventId` (`SyncController.cs:173-177`), dificultando dedup e auditoria no receptor.
9. `limite` do pull é controlado pelo cliente sem teto servidor; lote HTTP considera quantidade, não bytes. O request body aceita até 60 MB (`Program.cs:13-15`).
10. `Codigo` concatena nó e contador sem delimitador (`AppDbContext.cs:2394-2426`): nó 1/seq 11 e nó 11/seq 1 produzem o mesmo texto.
11. `Configuracao` mistura configuração global, filial e local. O cursor `sync.ultimo.id.recebido` vive na mesma tabela e pode ser enfileirado pelo SaveChanges.
12. `LogsAcao` e `LogsErro` são efetivamente globais; podem redistribuir stack traces e PII a edges. Devem ser ingest-only para o hub.
13. Não há handshake de schema/protocolo. Tipo novo pode virar `TipoDesconhecido` durante rollout desigual.
14. Estado do background é estático/volátil, não por nó; `/forcar-envio` responde sucesso sem forçar ciclo. O painel pode ficar verde sem prova de entrega.
15. Não há filtro global EF por filial no banco consolidado; isolamento depende de cada service lembrar do filtro.
16. A NF-e usa `MAX(Numero)+1` em um caminho (`VendaFiscalService.cs:373-379`), enquanto NFC-e usa serviço com lock. Sem constraint fiscal adequada, duas emissões concorrentes podem obter o mesmo número.

## 7. Arquitetura-alvo recomendada

### 7.1 Modos de deployment explícitos

Não inferir comportamento apenas de `No:Codigo`.

```text
StandaloneCloud  -> um banco, sem captura/transport de sync
Hub              -> consolidado do tenant, ingress, publisher, ACK e bootstrap
Edge             -> servidor local, outbox/push/inbox/pull
```

Um cliente pode ter mistura de filiais com e sem edge. Isso deve estar no registro do tenant/nó, não em defaults de `appsettings`.

O `appsettings.json` versionado hoje contém `No:Codigo=0`, tornando o fail-fast ilusório (`Program.cs:266-274`; `appsettings.json:38-40`). A role deve vir de variável obrigatória sem default produtivo e ser validada contra o cadastro do nó no hub.

### 7.2 Registro de nós

Modelo mínimo:

```text
ReplicationNode
  NodeInstanceId UUID (imutável)
  NodeCode INT (único, nunca reutilizado)
  IncarnationId UUID
  Mode/Role
  Status: Provisioning | Bootstrapping | Active | Suspended | Decommissioned
  CredentialFingerprint/PublicKey
  AppVersion / SchemaVersion / ProtocolVersion
  LastHeartbeatAt
  LastReceivedDeliveryAck por SubscriptionGeneration
  BootstrapGeneration

ReplicationNodeBranch
  NodeInstanceId
  BranchId
  AuthorityDomain: Operational | Fiscal | Bank | ...
  WriterEpoch
  Authority: Edge | Hub
  SubscriptionGeneration
  EffectiveFromPublishedSeq
```

O hub deriva os destinatários e a autoridade dessas tabelas. O edge não manda uma lista de filiais em query string. Filial sem edge pode ter o hub como writer; portanto a autoridade é um assignment versionado, não a regra fixa “edge sempre escreve”.

O hub também possui `HubLogGenerationId`. O cursor completo do edge é `(HubLogGenerationId, NodeInstanceId, SubscriptionGeneration, DeliverySeq)`. Restore/promoção do banco do hub não pode simplesmente reutilizar números menores: uma nova geração força handshake e reconciliação/rebootstrap, impedindo que um cursor antigo pule eventos.

`WriterEpoch` entra na credencial e em todo comando/evento branch-owned. Takeover manual incrementa o epoch; o hub não aceita evento de epoch antigo como escrita normal. Isso é fencing lógico no hub, não magia: um edge desconectado ainda pode continuar gravando localmente. Por isso não usar lease curto que expire durante queda de internet. Antes do takeover é necessário fence operacional/manual; ao reconectar, eventos do writer antigo vão para reconciliação e o edge é rebootstrapado.

Runbook mínimo de takeover:

1. tentar congelar e drenar o edge;
2. registrar último evento aceito e revogar a incarnation/credencial antiga;
3. isolar operacionalmente a instalação antiga;
4. incrementar `WriterEpoch` para o domínio/filial;
5. ativar o hub como writer;
6. colocar qualquer evento tardio do epoch anterior em reconciliação;
7. no failback, incrementar epoch novamente e bootstrapar o edge em banco limpo.

### 7.3 Registry único de política de replicação

Substituir denylist no outbox + allowlist no applicator + dicionários de owner por uma única fonte executável. Toda entidade/tipo de evento declara obrigatoriamente:

```text
Scope             Global | BranchOwned | MultiBranch | HubOnly | NodeLocal | ExternalDataset
Authority         Hub | AnyAuthorizedNode | OwnerBranch | FiscalNode | ExternalProvider
MutationModel     Snapshot | VersionedAggregate | AppendOnly | StateMachine | Projection
OwnerResolver
RecipientsResolver
ConflictPolicy    Reject | OptimisticVersion | ManualMerge | HlcLww | IdempotentAppend
DeletePolicy      SoftDelete | AckedTombstone | ReversalOnly | LocalPurge
AggregateRoot
NaturalKeys
PayloadContract/SchemaVersion
ContainsSensitiveData
RetentionPolicy
```

Startup falha quando:

- uma entidade não está classificada;
- uma entidade branch-owned não resolve owner;
- um POCO não pertence a agregado nem está marcado local;
- uma entidade replicável não tem identidade global;
- o applicator não tem handler para o contrato;
- uma FK GLOBAL → BranchOwned não possui semântica explícita.

### 7.4 Matriz de autoridade por classe de dado

| Classe | Exemplos | Autoridade | Conflito | Disseminação |
|---|---|---|---|---|
| Cadastro global | produto base, pessoa, fornecedor, fabricante, NCM | hub por padrão; multi-writer somente se necessário | optimistic version/manual merge; HLC apenas em campo benigno | todos os edges |
| Regra global/segurança | promoções, usuários, grupos, permissões | hub | versão do agregado; sem LWW cego | todos ou edges autorizados |
| Estado da filial | ProdutoFilial, configuração operacional, conta/caixa da loja | nó dono | CAS; rejeitar escritor não dono | dono + hub |
| Fato imutável | movimento de estoque/caixa/banco | nó originador | append idempotente por `OperationId` | hub e projeções necessárias |
| Documento com estado | venda, compra, conta a pagar/receber | nó dono + state machine | versão otimista e transições válidas | dono + hub |
| Fiscal/SNGPC | documento fiscal, numeração, inventário | nó fiscal designado | sem merge; cancelamento/inutilização por evento | saída para hub, nunca write-back genérico |
| Operação multifilial | transferência | cada filial escreve seu próprio ledger | dois eventos idempotentes correlacionados | origem + destino + hub |
| Dataset externo | IBPT/ABC e tabelas de provedor | hub/provedor | versão + checksum | pacote versionado |
| Infra/segredo | outbox, inbox, cursor, certificado, credencial, jobs | nó local | não aplicável | nunca |
| Logs | ação/erro | nó originador | append | ingestão para hub, sem fan-out |

### 7.5 Estoque, dinheiro e fiscal

`ProdutoDados.EstoqueAtual` é snapshot mutável. `MovimentoEstoque` parece um ledger, mas hoje ainda aceita mutação, não possui `OperationId`/unique de idempotência e carrega `SaldoApos` concorrente (`MovimentoEstoque.cs:12-38`; `AppDbContext.cs:1018-1031`). LWW de dois incrementos perde um deles.

Além do sync, há risco transacional local: `VendaService.FinalizarAsync` faz check-then-update de estoque e vários `SaveChanges` sem uma transação única (`VendaService.cs:289-630`). Duas finalizações podem perder baixa; um crash pode deixar venda finalizada sem estoque/caixa. `ProdutoLoteService.cs:53-109,124-148` repete read-modify-write e múltiplos commits.

Exigências:

- movimento append-only com `OperationId UNIQUE`;
- transação local única para venda/compra + movimentos + outbox;
- lock/CAS por `(Filial, Produto, SKU)` e lote ao validar/baixar estoque;
- `SaldoApos` apenas informativo; saldo verdadeiro é projeção da soma dos deltas;
- transitoriamente, snapshot com único escritor por filial e versão CAS;
- correção por movimento de ajuste/estorno, nunca sobrescrever história;
- handler de ledger rejeita UPDATE/DELETE normal;
- caixa físico/cofre segue owner-write + lançamentos idempotentes; conta bancária corporativa compartilhada pode ser hub/bank-integration-authoritative, pois `ContaBancaria.FilialId` é nullable (`ContaBancaria.cs:15-20`);
- `CaixaMovimento` e `MovimentoContaBancaria` precisam de chaves idempotentes; suprimento/sangria não podem commitar caixa e banco em operações separadas (`CaixaMovimentoService.cs:131-146,311-329`);
- baixa/estorno de contas a pagar/receber também é lançamento append-only que projeta status;
- venda finalizada/fiscalizada torna-se imutável; cancelamento é transição/evento compensatório;
- chamadas externas SEFAZ/Farmácia Popular saem por saga/outbox depois do commit local, sem segurar a transação do banco;
- cancelamento fiscal autorizado precisa orquestrar estornos idempotentes de estoque, caixa e financeiro; hoje o caminho em `VendaFiscalService.cs:706-770` altera principalmente o estado fiscal/XML;
- sequência fiscal tem uma autoridade por `(CNPJ, modelo, série, ambiente)` e constraint unique; reservar número atomicamente, tratar retry de serialization/unique e gaps/inutilização;
- `SequenciaCentral` significa central dentro do banco autoritativo da filial, não contador multi-writer no hub cloud;
- XML fiscal fica em contrato/blob separado com hash, acesso e retenção; não no payload genérico.

Transferência merece agregado/saga próprio, não apenas `Venda.TipoOperacao=Transferencia`. Estados recomendados: `Criada -> Despachada -> Recebida | Recusada | Cancelada`. Saída ocorre na expedição, entrada no aceite físico do destino, preservando lote/validade/Registro MS. Cada perna usa unique `(TransferId, Leg)` e o mesmo `CorrelationId`; compensação substitui transação distribuída. A NF de transferência é emitida na origem. O código atual registra `FilialDestinoId`, mas a finalização baixa essencialmente a origem (`Venda.cs:31-51`; `VendaService.cs:486-562`).

### 7.6 Envelope de evento explícito

Não serializar um grafo EF arbitrário por reflection como contrato de rede. Exemplo mínimo:

```json
{
  "protocolVersion": 2,
  "schemaVersion": 17,
  "eventId": "uuid",
  "tenantId": "uuid-ou-implícito-pelo-banco",
  "originNodeInstanceId": "uuid",
  "originIncarnationId": "uuid",
  "branchAuthorityEpoch": 8,
  "entityType": "Product",
  "entityGlobalId": "uuid",
  "entityDatabaseId": 1000000123,
  "ownerBranchId": 7,
  "eventKind": "ProductSnapshotV2",
  "baseCanonicalRevision": 41,
  "proposedDomainVersion": 42,
  "hlc": { "physicalUtcMs": 1784300000000, "logical": 3 },
  "createdAtUtc": "2026-07-17T12:00:00Z",
  "payload": {},
  "payloadHash": "sha256"
}
```

Regras:

- `eventId` é imutável e idempotente fim a fim;
- duplicate só é válido se `eventId` **e hash do envelope** coincidirem; o mesmo ID com payload diferente é violação de integridade;
- origem vem da credencial; divergência com o body rejeita;
- `branchAuthorityEpoch` precisa coincidir com o assignment atual para eventos branch-owned;
- `entityDatabaseId` (`long`) continua sendo a PK/FK global preservada; `entityGlobalId` é o `SyncGuid` guard e o par precisa ser imutável;
- owner e recipients são validados/derivados pela policy;
- contrato e versão são allowlisted;
- payload não inclui segredo nem navegação acidental;
- timestamps são UTC; HLC é persistido por nó;
- ao receber HLC remoto, o nó faz o merge antes de emitir seu próximo evento;
- eventos incompatíveis ficam pendentes, não são descartados após cinco tentativas.

### 7.7 Fluxo de entrega V2

```text
Transação de negócio no edge
  -> grava dado + Outbox(eventId) atomicamente

Push
  -> hub autentica NodeInstance
  -> valida contrato, autoridade e escopo
  -> INSERT idempotente em HubInbox/EventLog no estado Received
  -> responde accepted/duplicate/rejected POR eventId
  -> edge marca somente accepted/duplicate

Hub applier
  -> Received -> Applying -> Applied | Conflict | Rejected
  -> aplica CAS/state machine/append + marca Applied na mesma transação
  -> conflito vira ReplicationConflict preservando todas as versões

Hub publisher exclusivo
  -> numera somente eventos Applied/Resolved elegíveis e já commitados com PublishedSeq
  -> materializa NodeDelivery imutável com DeliverySeq contíguo por NodeInstance + SubscriptionGeneration

Pull
  -> servidor lê NodeDelivery > ReceivedDeliverySeq
  -> edge grava o lote inteiro no Inbox + ReceivedDeliverySeq na mesma transação (tudo ou nada)
  -> ACK após commit local
  -> applicator local aplica efeito + marca Inbox.Applied na mesma transação
```

Separar duas marcas:

- `ReceivedDeliverySeq`: maior prefixo contíguo duravelmente no inbox local; pode sustentar retenção do payload;
- estado por evento: efeito de negócio aplicado, em retry ou conflito;
- `AppliedThrough`, se existir, é somente o maior **prefixo contíguo** resolvido, nunca `MAX` dos eventos aplicados fora de ordem.

Modelo de cursor:

```text
CanonicalEvent(PublishedSeq, EventId, CanonicalPayload, ...)
NodeDelivery(NodeInstanceId, SubscriptionGeneration, DeliverySeq, EventId, ...)
UNIQUE(NodeInstanceId, SubscriptionGeneration, DeliverySeq)
UNIQUE(NodeInstanceId, SubscriptionGeneration, EventId)
```

O ACK inclui `HubLogGenerationId`, nó, incarnation, subscription generation e `DeliverySeq`. Ele é monotônico, não regride e não pode exceder o último lote oferecido pelo hub.

Os recipients históricos não podem ser recalculados no GET com a configuração atual. Eles são congelados/materializados quando o evento é publicado. Mudança de filial atendida cria nova `SubscriptionGeneration` e executa snapshot/backfill antes de começar a receber a nova audiência.

Eventos do mesmo agregado são aplicados por revisão canônica/base causal. `PublishedSeq` e `DeliverySeq` ordenam log/entrega; não autorizam aplicar a revisão 44 antes de resolver a 43.

Se a resposta/ACK se perder, o hub reenvia e o inbox deduplica por `EventId`. O transporte é at-least-once; “efeito único” vem das transações e chaves idempotentes, não de uma promessa de exactly-once da rede.

### 7.8 Retenção e tombstones

Idade fixa não prova consumo.

- hub persiste ACK por entrega/destinatário materializado;
- payload do log pode ser compactado depois do ACK de todos os destinatários daquele evento e da janela de recuperação do hub;
- decommission é explícito e auditado;
- nó ausente além do SLA não bloqueia para sempre: muda para `RebootstrapRequired` e deixa de participar da retenção somente por ato explícito;
- evento/payload sensível tem política de minimização, criptografia e retenção separada.

ACK do delete **não prova** que não exista update antigo no outbox de outro writer, backup velho ou mensagem atrasada. Portanto o fence de versão deletada não deve ser purgado com o payload. Manter indefinidamente um metadado compacto por identidade:

```text
EntityType + Id + SyncGuid
State = Live | Deleted
CanonicalRevision/HLC
AuthorityEpoch
LastWriterNodeInstanceId
```

Recriação legítima usa nova identidade ou transição explícita acima da revisão deletada. Só seria seguro apagar esse fence provando que todas as gerações escritoras antigas foram revogadas e que a identidade nunca será reutilizada; para este ERP, manter o registro compacto é mais seguro e barato.

Há ainda uma dependência de durabilidade: depois de `accepted`, o hub vira a cópia autoritativa do evento. O edge deve reter outbox confirmado por pelo menos a janela de recuperação/PITR do hub, e mudança de `HubLogGenerationId` precisa permitir replay/reconciliação. Caso contrário, restore do hub com RPO maior que zero pode perder evento que já foi confirmado à origem.

### 7.9 Conflitos

Não misturar políticas diferentes sob o nome “LWW”:

- **cadastro global:** a proposta envia `baseCanonicalRevision`; o hub faz CAS e atribui a próxima revisão. Duas propostas da mesma base: uma aplica, a outra vira conflito;
- **HLC-LWW:** somente em campos benignos explicitamente aprovados; persistir HLC entre reinícios, incorporar clocks recebidos, limitar timestamp absurdamente futuro e desempatar pelo writer real;
- **owner-write/state machine:** validar `WriterEpoch`, autoridade e transição de domínio; versão proposta pelo edge não é automaticamente a revisão canônica;
- **append-only:** `OperationId UNIQUE`; reenvio é no-op e UPDATE/DELETE normal é rejeitado.

Live row e delete compartilham uma única linha de estado/versionamento, ou o mesmo lock/CAS por `(EntityType, Id, SyncGuid)`. SELECT seguido de updates em estruturas separadas continua sujeito a corrida. DELETE de linha ausente grava o fence; não vira retry.

Merge automático só é permitido para campos disjuntos e policy conhecida. CPF/CNPJ/EAN/login duplicado vai para centro de reconciliação. No V2 inicial, não fundir automaticamente dois `long Id`s: escolher o canônico e remapear todas as FKs exige uma migration/fluxo explícito com aliases, não um `SetValues` durante o sync.

CRDT não deve ser aplicado genericamente ao banco relacional. Ele é útil somente quando existe um tipo matemático adequado, como contador sem limite de negócio; estoque e dinheiro possuem invariantes e autoridade, portanto ledger/owner-write é mais seguro.

## 8. Correções específicas de modelo antes do piloto

1. `Configuracao`: separar em configuração tenant-global, filial e deployment-local. Cursor e segredo são locais e não replicam.
2. `Feriado`: possui `FilialId?`, mas não está classificado por filial (`Feriado.cs:24-28`). Definir global versus filial por linha.
3. `ProdutoLocal`: é descrito como localização por filial, mas não tem `FilialId` (`ProdutoLocal.cs:3-7`). Corrigir o modelo.
4. `LogAcao`/`LogErro`: ingest-only ao hub; não fan-out.
5. `SyncGuid`: `NOT NULL UNIQUE` em entidades replicáveis; migração precisa diagnosticar duplicados antes da constraint.
6. `Codigo`: formato não ambíguo, por exemplo `0001-0000000011`, e unique composto adequado.
7. NF-e: eliminar `MAX+1`; reservar número por UPSERT/lock atômico com retry explícito de serialization/unique e criar unique fiscal. Revisar também a corrida de criação da primeira linha no `SequenciaCentralService`.
8. Transferência: agregado/saga e duas pernas idempotentes; um `FilialDonoId` escalar não cobre origem + destino (`Venda.cs:48-51`).
9. `Compra.XmlConteudo` usa `[JsonIgnore]` (`Compra.cs:43-46`); decidir se o hub precisa do documento e criar contrato/armazenamento seguro, em vez de presumir réplica completa.
10. Datasets externos como IBPT não devem produzir milhares de eventos genéricos; distribuir versão/checksum e aplicar atomicamente.

## 9. Plano de execução para o Claude

### Fase 0 — ADR e testes que demonstram as falhas

Antes de alterar o protocolo:

- registrar as respostas da seção 11;
- criar ADR de topologia, autoridade e consistência;
- criar projeto de testes com PostgreSQL real;
- implementar testes vermelhos para segurança, cursor, CAS, update/delete e filhos;
- arquivar documentos históricos conflitantes, sem apagá-los.

**Gate:** nenhum segundo nó habilitado.

### Fase 1 — Contenção e identidade

- policy exclusiva do data plane;
- credencial por nó e rotação de segredos;
- `ReplicationNode` e assignments server-side;
- modos `StandaloneCloud/Hub/Edge` explícitos;
- corrigir origem do hub;
- registry fail-closed;
- desativar captura em standalone;
- limites de requests e DTOs sem payload no painel.

**Gate:** testes provam que usuário humano não usa sync e nó A não acessa filial B.

### Fase 2 — Log de eventos e entrega sem gap

- inbox idempotente no hub antes do efeito;
- `EventId` unique;
- state machine do inbox com aplicação/estado no mesmo commit;
- publisher exclusivo + `PublishedSeq` canônico;
- `NodeDelivery.DeliverySeq` por nó/generation;
- inbox durável no edge;
- ACK server-side;
- `HubLogGenerationId` e retenção do outbox compatível com o RPO do hub;
- status persistido por nó;
- falha HTTP altera status/backoff honestamente.

**Gate:** teste de commit invertido, resposta perdida, retry concorrente e crash não perde evento nem duplica efeito.

### Fase 3 — Concorrência e deleção

- HLC persistente onde realmente houver multi-writer;
- CAS atômico;
- writer real no comparador;
- row/tombstone com o mesmo estado/versionamento;
- conflitos preservam versões, não sobrescrevem a quarentena.

**Gate:** todas as permutações de upsert/update/delete convergem.

### Fase 4 — Domínio e agregados

- classificar os 134 conjuntos/tipos persistidos;
- separar produtos globais e por filial;
- contratos explícitos por agregado;
- IDs estáveis/diff dos 27 filhos POCO;
- movimentos append-only de estoque/caixa/banco;
- state machines de venda/compra/financeiro;
- transferência e fiscal com autoridade correta;
- seeds determinísticos.

**Gate:** invariantes de estoque, caixa e fiscal permanecem válidas sob reorder/duplicate.

### Fase 5 — Bootstrap, retenção e rollout

- snapshot + watermark;
- restore/rejoin/incarnation;
- subscription generation;
- ACK-based retention e rebootstrap;
- handshake de protocolo/schema;
- métricas, alertas e ferramenta de reconciliação;
- piloto com dados sintéticos e depois um tenant não crítico.

**Gate:** comparação por contagem/hash entre hub e edge conforme escopo, após teste de caos e período offline definido pelo SLA.

### Migração do Sync V1 para o V2

Não fazer V1 e V2 publicarem simultaneamente. Com o sync atual desabilitado:

1. inventariar e exportar `SyncFila`, quarentena e tombstones V1 para auditoria;
2. resolver ou declarar descartáveis os conflitos existentes por `OpUid` e entidade;
3. instalar o schema V2 sem ativar workers;
4. escolher fonte de verdade por classe/filial e gerar bootstrap completo;
5. iniciar V2 a partir do corte do bootstrap, sem reexecutar cegamente o backlog V1;
6. comparar hashes/contagens e somente então arquivar tabelas V1;
7. rollback operacional retorna ao snapshot; nunca ativa dupla escrita nos dois protocolos.

### Estratégia de branches/commits

- um commit por preocupação;
- não misturar migration de protocolo, auth, regra de produto e refactor de filhos;
- cada PR descreve invariantes e inclui o teste que falhava antes;
- não ligar `Sync:Habilitado` no mesmo commit que introduz o mecanismo;
- não fazer backfill destrutivo sem relatório de dry-run e backup.

## 10. Testes de aceitação obrigatórios

Usar PostgreSQL real em containers/processos isolados; EF InMemory não reproduz sequences, locks, constraints, isolamento ou ordem de commit.

| Grupo | Cenário | Resultado exigido |
|---|---|---|
| Cursor | tx A reserva menor e commita depois de B | A e B recebem log canônico e `DeliverySeq` correto; nada fica atrás do ACK |
| Entrega filtrada | eventos não destinados entre dois destinados ao edge | `DeliverySeq` local permanece contíguo e ACK não bloqueia outros nós |
| Idempotência | hub aceita e resposta se perde | retry recebe duplicate; efeito único |
| Integridade | mesmo `EventId` com hash/payload diferente | rejeição crítica; nenhuma reaplicação |
| Concorrência | dois updates simultâneos no hub | CAS escolhe conforme policy, nunca “último SaveChanges físico” |
| Delete | U/D em todas as ordens, com empate e clock skew | todos os nós terminam no mesmo estado |
| Escopo | usuário/nó falsifica filial/origem | 403/rejeição; nenhum payload vazado |
| Cloud write | hub edita dado criado no edge | edge dono recebe a nova versão |
| Agregado | coleção ausente | filhos preservados |
| Agregado | coleção vazia e autoritativa | filhos removidos uma única vez |
| Agregado | remove/recria item | IDs/diff convergem, sem órfão |
| FK | pai e filho chegam em lotes/ordens diferentes | retry termina; nenhum descarte silencioso |
| Restore | backup antigo retorna com mesma config | incarnation antiga é bloqueada; exige rebootstrap |
| Restore hub | hub volta com log/sequência anterior | nova `HubLogGenerationId`; edge não pula evento confirmado/perdido |
| Nó gêmeo | duas instâncias usam mesmo NodeCode | segunda não ativa |
| Assinatura | edge passa a atender nova filial | bootstrap/backfill ocorre antes de ativar |
| Retenção | nó offline ultrapassa SLA | hub exige rebootstrap; não serve histórico incompleto |
| Fence de delete | update antigo aparece após compactar payload do delete | fence de versão rejeita ressurreição |
| Schema | edge antigo recebe contrato novo | evento pausa com diagnóstico; não é aposentado |
| Estoque | vendas/ajustes duplicados e reordenados | `OperationId` evita efeito duplo; saldo = ledger |
| Venda local | crash entre finalizar, baixar estoque, caixa e outbox | transação reverte tudo ou commita tudo |
| Transferência | retry após saída e antes da entrada | cada lado recebe um movimento correlacionado, sem duplicar |
| Fiscal | emissões concorrentes | número unique por chave fiscal |
| Sequence | apagar maior ID e reiniciar app | sequence nunca recua/reutiliza identidade |
| Chaos | matar processo em cada fronteira de commit/ACK | retomada sem perda e sem efeito duplicado |
| Takeover | edge antigo retorna após hub assumir novo epoch | evento antigo vai para reconciliação; não altera estado automaticamente |
| Hub split-brain | duas instâncias tentam publicar como primary | fencing da infraestrutura impede dois logs canônicos |

Adicionar teste baseado em propriedades: gerar operações, duplicar, atrasar e reordenar mensagens com seed determinístico; depois de entregar todas, comparar estado e tombstones de todos os nós e validar invariantes do domínio.

## 11. Decisões que o dono do produto precisa confirmar

Estas perguntas não devem ser respondidas pelo código por suposição. Incluo a recomendação para destravar a conversa.

1. **Cada cliente terá banco próprio?**  
   Recomendação: sim. Se não, abrir projeto separado de multi-tenancy.

2. **Um edge deve armazenar vendas/caixa/estoque de outras filiais?**  
   Recomendação: não. GLOBAL vai para todos; operacional vai ao dono + hub. Dashboards entre filiais usam projeções.

3. **Um edge pode atender várias filiais?**  
   Recomendação: suportar mapping N:N, embora a instalação comum atenda uma.

4. **O cliente pode misturar filiais com e sem servidor local?**  
   Recomendação: sim, modelado explicitamente no registro de deployment.

5. **Durante queda do edge, a nuvem pode finalizar venda/fiscal da mesma filial?**  
   Recomendação: não automaticamente. Somente takeover manual com epoch/fencing e rebootstrap do edge antigo.

6. **Qual o maior período offline suportado?**  
   Não escolher 90 dias por conveniência. Definir SLA a partir da operação real; acima dele, rebootstrap.

7. **Preço, fiscal e fornecedor são uniformes ou admitem override por filial?**  
   Recomendação: separar template/global de override/estoque por filial.

8. **Como resolver dois cadastros offline da mesma pessoa/produto?**  
   Recomendação: conflito por chave natural e tela de merge com alias/remapeamento; não LWW silencioso.

9. **Quais configurações são globais, por filial e locais?**  
   Exigir inventário por chave. Cursor, credencial, certificado e URL são locais.

10. **Logs vão para as lojas?**  
    Recomendação: não; ingestão para hub apenas, com retenção/redação.

11. **Atualizações serão simultâneas?**  
    Recomendação: assumir que não. Protocolo precisa aceitar janela de versões compatíveis e bloquear incompatíveis.

12. **Qual RPO/RTO para internet, falha do edge e falha do hub?**  
    Sem esses números, “alta disponibilidade” não possui critério verificável.

## 12. O que não fazer

- Não corrigir o cursor usando apenas timestamp, safety window, `MAX(Id)` ou polling dos últimos N minutos.
- Não manter cursor em `Id` e adicionar somente `pg_snapshot_xmin`; as ordens de XID e identity continuam independentes.
- Não atribuir `PublishedSeq` dentro da transação de negócio.
- Não usar `PublishedSeq` global esparso como cursor do edge; materializar `DeliverySeq` por destinatário/generation.
- Não confiar em `filialId`, `filiais`, `NoOrigemId` ou `FilialDonoId` enviados pelo cliente.
- Não usar uma senha compartilhada de administrador como credencial de nó.
- Não aplicar delete-missing quando a coleção não foi comprovadamente carregada/autoritativa.
- Não usar LWW de linha inteira em estoque, saldo bancário, caixa, venda finalizada ou documento fiscal.
- Não apagar fence de versão/tombstone apenas por idade ou ACK; compactar payload e preservar o estado mínimo de delete.
- Não reciclar `NodeCode`, PK, sequence ou identity após restore.
- Não espalhar evento em conflito aos peers antes da decisão de conflito.
- Não assumir que replicação lógica nativa do PostgreSQL resolverá multi-master: conflitos podem parar o subscriber, e DDL/sequences não são replicados automaticamente.
- Não habilitar produção multi-nó antes dos gates e testes acima.

## 13. Limpeza documental necessária

Ordem de confiança atual:

1. `ContextDocuments/INFRAESTRUTURA/synAteAqui.md` — retrato mais recente;
2. este documento — revisão independente e arquitetura recomendada;
3. `sync.md` — detalhe útil, mas com contradições;
4. `classificacao-replicacao.md` — taxonomia a reconciliar com o modelo;
5. `auditoria-sync.md`, `multi-filial.md`, `sinc.txt` — históricos.

Contradições que precisam ser corrigidas:

- `sync.md:88-99` diz que a lápide local foi corrigida; `:146-158` repete o diagnóstico antigo de que ela não existe;
- `multi-filial.md` permite PK diferente por PC, enquanto o código atual preserva PK por faixa;
- docs antigos dizem “tudo para todos”, enquanto a classificação atual diz branch-owned → dono + hub;
- `Configuracao` aparece como local, por filial e global em três fontes;
- o comentário `AppDbContext.cs:2072-2075` ainda sugere “TUDO replica”;
- `PADROES_CODIGO.md:19` aponta para um registry antigo que não representa a captura/applicator atuais;
- regra de produto conflita com a classificação de suas subtabelas.

Mover documentos antigos para `docs/archive` com cabeçalho **HISTÓRICO — NÃO IMPLEMENTAR**. A matriz versionada do registry deve se tornar a fonte de verdade, com teste de consistência contra o modelo EF.

## 14. Fontes técnicas primárias

- PostgreSQL — sequences não são transacionais e não fornecem numeração sem gaps:  
  https://www.postgresql.org/docs/current/functions-sequence.html
- PostgreSQL — transaction isolation e anomalias de serialização:  
  https://www.postgresql.org/docs/current/transaction-iso.html
- PostgreSQL — advisory locks transacionais:  
  https://www.postgresql.org/docs/current/explicit-locking.html
- PostgreSQL — conflitos de logical replication param o worker e exigem resolução:  
  https://www.postgresql.org/docs/current/logical-replication-conflicts.html
- PostgreSQL — logical replication não replica DDL nem estado de sequence:  
  https://www.postgresql.org/docs/current/logical-replication-restrictions.html
- Microsoft — concorrência otimista e concurrency tokens no EF Core:  
  https://learn.microsoft.com/en-us/ef/core/saving/concurrency
- Microsoft — `[Authorize]` sem role/policy apenas exige usuário autenticado:  
  https://learn.microsoft.com/en-us/aspnet/core/mvc/security/authorization/simple
- Microsoft — policy-based authorization no ASP.NET Core:  
  https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies
- AWS Prescriptive Guidance — transactional outbox, ordem e consumidor idempotente:  
  https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/transactional-outbox.html
- IETF RFC 8705 — autenticação mTLS e tokens vinculados ao certificado:  
  https://datatracker.ietf.org/doc/html/rfc8705
- Kulkarni et al. — Hybrid Logical Clocks:  
  https://cse.buffalo.edu/~demirbas/publications/hlc.pdf
- Gilbert e Lynch — limites de consistência/disponibilidade sob partição:  
  https://pld.cs.luc.edu/database/gilbert_lynch_brewer_proof.pdf
- Preguiça, Baquero e Shapiro — visão formal de CRDTs e suas condições de convergência:  
  https://arxiv.org/abs/1805.06358

## 15. Instrução final ao Claude

Não comece pelo `SeqEntrega` isoladamente. Primeiro transforme as decisões da seção 11 em ADR e escreva os testes P0. Em seguida implemente, nesta ordem: identidade/autorização de nó, separação de origem-owner-recipient, log idempotente, publisher/ACK, CAS/delete convergente, registry fail-closed, contratos de agregado, domínio append-only/state machine, bootstrap e retenção.

Para cada etapa, entregue:

1. invariante que a mudança pretende garantir;
2. teste que reproduz a falha anterior;
3. migration/backfill com dry-run e rollback operacional;
4. telemetria que permite provar a garantia em execução;
5. documentação atualizada sem misturar estado presente com histórico.

Até esses gates serem cumpridos, mantenha o sync desabilitado em produção multi-nó. O código já avançou bastante, mas hoje ele fornece eventualidade de transporte sem ainda provar segurança, autoridade e convergência de domínio — justamente as propriedades que um ERP não pode deixar implícitas.
