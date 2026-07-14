# Auditoria do Sincronismo — o que há × o que mudar (multi-master)

> Diagnóstico do mecanismo de sync atual frente ao alvo multi-master (ver
> `classificacao-replicacao.md` e a memória do assistente). Auditoria adversarial
> em 8 dimensões, 68 gaps verificados. Data: **2026-07-14**. Ainda é ANÁLISE — sem código.

## O que está SÓLIDO (preservar)

- **PK faixa-por-nó** (bigint IDENTITY + offset `filialCodigo*1e9`, `DatabaseSeeder.cs:14,367-405`)
  é o fundamento certo de unicidade cross-nó: o apply preserva o Id de origem e é idempotente por Id
  (`SyncApplicator.cs:42-52`), FK sempre válida sem remapeamento. Manter o eixo; consertar só os buracos.
- **Padrão OUTBOX** (SyncFila alimentada no `SaveChangesAsync`, `AppDbContext.cs:2093-2109`) é o desenho
  correto pra replicação caseira observável/seletiva. Falta torná-lo **atômico** e **roteável**.
- **Ordenação topológica por FK no apply** (`GetOrdemTabela`, `SyncApplicator.cs:114-199`) garante
  integridade referencial dentro do lote. Preservar — só não pode ser usada como critério de vencedor.
- **Infra de LWW já existe, dormente**: `AtualizadoEm` (`BaseEntity.cs:8`) e `SyncGuid`+índice
  (`BaseEntity.cs:15`). Não precisa de coluna nova — precisa **passar a LER** esses campos no applicator.
- **Transporte estrela HTTP com PUSH at-least-once** (marca Enviado só após 2xx,
  `SyncBackgroundService.cs:129-133`) + cursor high-water-mark: central offline retém e retenta.
- **`AplicandoSync`** já isola a reaplicação (não re-gera Codigo nem re-enfileira). Só precisa parar de
  recarimbar `AtualizadoEm`.
- **`FilialOrigemId` já vem do config do servidor** (eixo Nó) — metade da separação de eixos já feita.
- **Numeração fiscal com Serializable + FOR UPDATE** (`SequenciaCentralService.cs:16-58`) é o rigor certo
  DENTRO de um banco. O que muda é a **autoridade**, não o mecanismo.

## O que precisa MUDAR — CRÍTICOS (corrompem/perdem dado ou impedem o multi-master)

1. **LWW real por `AtualizadoEm` no apply (UPDATE + DELETE).** A doc promete LWW mas
   `SyncApplicator.cs:62` faz `SetValues` cego = *last-received-wins*: edição ANTIGA que chega depois
   sobrescreve a nova → **perda silenciosa de update** na única superfície de conflito (cadastros GLOBAL).
   DELETE é incondicional e sem tombstone → delete vence update mais novo, e INSERT antigo **ressuscita**
   linha deletada. *Depende de:* preservar o `AtualizadoEm` de origem (item 2).
2. **Parar de destruir o `AtualizadoEm` de origem + padronizar UTC.** No apply, o override AINDA carimba
   `AtualizadoEm=Agora()` nos Modified (`AppDbContext.cs:2051-2052`) → cada nó guarda um horário diferente
   pra mesma linha, **quebrando qualquer LWW**. E `Agora()` é horário de Brasília, não UTC → incomparável
   entre nós. Preservar o timestamp que veio no JSON; auditar aplicação num `AplicadoEm` separado.
3. **Reconciliação por identidade estável (`SyncGuid`/chave natural) + tratar 23505 como merge.** INSERT
   só checa Id (`SyncApplicator.cs:44-45`); mesmo cliente cadastrado offline em 2 nós nasce com Ids
   diferentes e mesmo CpfCnpj → viola o unique → **23505 e a operação é DESCARTADA pra sempre** (ponteiro
   avança no erro). Duas identidades pra mesma pessoa = divergência permanente. `SyncGuid` existe, é
   indexado, e nunca é lido.
4. **Quarentena/retry no apply — parar de avançar o ponteiro em erro.** No PULL o `lastSuccessId` avança
   MESMO em exceção (`SyncBackgroundService.cs:188,193` "avançar mesmo em falha") e a op nunca é
   reprocessada. Falha transitória (FK cujo pai chega em outro lote, deadlock, o 23505) vira **perda
   definitiva** — e o registro de erro nem guarda o `DadosJson` pra reaplicar.
5. **Furo do dicionário + escopo GLOBAL×POR-FILIAL no PULL — OBRIGATORIAMENTE JUNTOS.** Escrita é
   *denylist* (tudo BaseEntity fora de 5 tabelas enfileira) mas aplicação é *allowlist* de ~61 tabelas:
   **Caixa, ContaReceber, MovimentoEstoque, VendaItem etc. viajam na fila e são descartadas em silêncio**
   (`SyncApplicator.cs:30-31`, `return false` sem log) → estoque/caixa/financeiro **nunca chegam**. Mas
   corrigir o furo SEM escopo (hoje o PULL só filtra `FilialOrigemId != filialId`) faz Caixa/ContaReceber
   da filial A **vazar** pra filial B. Os dois consertos criam o bug um do outro se subirem separados.
   *Depende de:* separar os eixos (item 6).
6. **Separar os dois eixos: Nó/Origem (config) vs FilialId-dono (JWT) — na BaseEntity e na SyncFila.** Um
   único `FilialOrigemId` carrega os dois conceitos: quando a NUVEM escreve uma venda da filial 5,
   `FilialOrigemId=nó-nuvem` e **não existe campo dizendo que o dado É DA filial 5**. Sem o eixo dono como
   coluna de 1ª classe na `SyncFila` (não tem hoje), o roteador não entrega por-filial. **Pré-requisito
   estrutural** do escopo por-filial.
7. **Contador fiscal (`SequenciaCentral`) fora do LWW + autoridade única por (filial,série).**
   `SequenciaCentral` é BaseEntity e NÃO está em `_tabelasSemSync` → a linha `ProximoNumero` **replica sob
   LWW**. O FOR UPDATE só trava dentro de um banco; no alvo a nuvem também vende pela filial (celular na
   rua) → nuvem e nó local incrementam em paralelo e o sync sobrescreve um → **NÚMERO FISCAL DUPLICADO**.
   Fiscal precisa ser gapless/único — o oposto de LWW e de nextval. Fiscal **NÃO** migra pra nextval.
8. **Fundação multi-tenant: `TenantId` como identidade de roteamento + provisionamento + auth por
   tenant.** ZERO conceito de tenant: AppDbContext único com connection fixa, auth de sync é usuário
   SISTEMA global com chave fallback compartilhada, token sem claim de tenant. Numa central multi-org,
   operações de tenants distintos aterrissam na mesma tabela. Pré-requisito do "1 banco por tenant".

## ALTA severidade

9. **`nextval` nativo + prefixo do Nó (com separador) no lugar do row-bump.** `GerarCodigo` retorna
   `$"{_filialCodigo}{ultimo}"` por **concatenação sem separador** e o índice de Codigo é NÃO-único →
   nó=1/seq=23 e nó=12/seq=3 geram ambos `"123"` (Codigo ambíguo). E o `ON CONFLICT DO UPDATE Ultimo+1`
   **segura lock** da linha até o commit → gargalo global (LogsAcao) e foi o motivo da transação da
   finalização ter sido revertida. `nextval` não segura lock e tolera gaps.
10. **Transação única outbox: entidade + SyncFila num só commit.** Dois `SaveChangesAsync` separados
    (`AppDbContext.cs:2090` e `2093-2108`): crash entre eles = registro persistido localmente que **nunca
    entra na fila** → nunca replica. *Depende de:* nextval (senão a transação reacende o lock).
11. **Nuvem como Nó de 1ª classe com faixa reservada; unificar default de `Filial:Codigo` (fail-fast).**
    A nuvem roda sem `Filial:Codigo` → `filialCodigo=0` → `ConfigurarSequences` nunca roda → identity da
    nuvem começa em 1,2,3 e **colide** com a faixa da filial 1. Default divergente (0 no Program/Sync vs 1
    no AppDbContext). Nó mal configurado vira "nó 1" silenciosamente. RESTART no seed pode até **recuar** a
    sequence e reemitir Ids já replicados.
12. **Agregado Venda atômico.** `VendaItem/VendaPagamento/VendaItemDesconto` são POCO (não-BaseEntity) →
    nunca enfileirados; a venda replica só o cabeçalho → **venda fantasma** no destino, estoque/caixa
    impossíveis de reconstruir. Serializar itens+pagamentos DENTRO do `DadosJson` do cabeçalho.
13. **Idempotência de transporte fim-a-fim (chave Nó+SeqLocal) + cursor à prova de gap.** PUSH sem chave
    de idempotência → resposta perdida após commit = **duplica** linhas na central. E o cursor `Id>ultimoId`
    **pula itens**: sob escrita concorrente, Id=105 pode ficar visível antes de 104 commitar → 104 nunca é
    puxada = perda permanente.

## MÉDIA / BAIXA

14. **Preservar proveniência + serializar só escalares** (`SetValues` escreve linha inteira → lost-update
    de campo concorrente; snapshot arrasta navigations).
15. **Classificar Configuracao/LogAcao/LogErro como POR-FILIAL** e tirar config de Nó da tabela replicável
    (`Configuracoes` hoje replica GLOBAL — o oposto do alvo; o próprio ponteiro do sync é uma Configuracao).
16. **Robustez do transporte**: retenção/compactação da fila central (hoje cresce indefinidamente),
    backoff+jitter (hoje 30s fixo martela central offline), TLS real (usa
    `DangerousAcceptAnyServerCertificateValidator` = MITM), lote adaptativo, ordem causal cross-batch.
17. **(baixa) Decidir destino do `SyncGuid`**: promover a chave de reconciliação (item 3) ou aposentar.

## ORDEM DE MIGRAÇÃO recomendada

- **FASE 0 — fundação de identidade** (pré-requisito de quase tudo): (a) separar eixos Nó/Origem vs
  FilialId-dono na BaseEntity + coluna FilialDono na SyncFila; (b) unificar "código do nó" numa fonte só
  com fail-fast no boot + nuvem como nó com faixa reservada; (c) `TenantId` + catálogo/provisionamento +
  auth por tenant (se a central for multi-org).
- **FASE 1 — corretude de conflito** (só mexe no applicator, sobe cedo): (a) parar de recarimbar
  `AtualizadoEm` + UTC; (b) LWW real no UPDATE; (c) tombstone/soft-delete com LWW no DELETE; (d)
  reconciliação por SyncGuid/chave natural + 23505 vira merge; (e) quarentena/retry, não avançar ponteiro
  em erro.
- **FASE 2 — integridade do outbox e captura**: (a) nextval + prefixo do nó (pré-req da transação); (b)
  transação única outbox; (c) serializar escalares + agregado Venda atômico; (d) isolar contador fiscal.
- **FASE 3 — dicionário + escopo** (juntos, senão um cria o vazamento do outro): (a) fonte única de
  classificação validada no startup (op sem tipo = erro visível); (b) escopo GLOBAL×POR-FILIAL no PULL;
  (c) reclassificar Configuracao/Logs.
- **FASE 4 — confiabilidade e escala do transporte**: idempotência fim-a-fim, cursor à prova de gap,
  retenção/compactação, backoff+jitter, TLS real, lote adaptativo, ordem causal cross-batch.

## DECISÕES do dono (2026-07-14)

1. **Codigo visível = ÚNICO-POR-NÓ** ✅ (curto/legível é a intenção). `Codigo` guarda só o sequencial
   (sem embutir o nó); a unicidade vem de **índice composto (NoOrigem, Codigo)**. Sequencial via
   `nextval` nativo por (tabela, nó), local — funciona offline (o mito de "global quebra sem internet"
   não procede: cada nó já tem faixa própria, sem coordenação). **FK nunca corre risco** porque FK usa
   o `Id` bigint (globalmente único por faixa), não o Codigo — o Codigo é só cosmético/humano.
2. **Autoridade fiscal = PINAR AO NÓ DONO** ✅. A nuvem NÃO emite documento fiscal. Venda mobile na rua =
   pré-venda/não-fiscal; o documento é emitido/numerado pelo nó da filial ao sincronizar. `SequenciaCentral`
   fica não-replicável; zero risco de número duplicado. Fiscal NÃO migra pra nextval (mantém Serializable+
   FOR UPDATE, autoridade única por (filial,série)).
3. **Conflito = LWW POR LINHA** ✅ (por `AtualizadoEm`, tie-break por nó). Aceita lost-update de campo
   concorrente (raro em cadastro); documentar. Sem merge por coluna.

## PERGUNTAS AINDA ABERTAS (operacionais — resolver depois)

4. **Tenant**: 1-banco-por-tenant confirma que `TenantId` NÃO precisa virar coluna em toda entidade?
   Onde o tenant é resolvido (subdomínio / chave de instalação / claim no token)? Onde vive o catálogo de
   connection strings e como cada segredo de sync por tenant é provisionado?
5. **Faixa de 1 bilhão de Id por nó/tabela** é suficiente pro horizonte (nº de nós + nuvem + tabelas
   quentes tipo LogsAcao por anos)? Vale trocar por particionamento por bits (dono trivial do Id)?
6. **Backfill retroativo**: as entidades que hoje "somem" pelo furo (Caixa, ContaReceber, MovimentoEstoque)
   precisam de reconciliação histórica ao entrar no dicionário, ou só passam a replicar dali pra frente?
7. **Onboarding de nó novo**: registro/validação do código de nó numa fonte central no 1º handshake
   (rejeitar código já usado por nó ativo), à la o incidente do ProviderInstanceId reaproveitado no ZulexSac.

## PRÓXIMO PASSO recomendado
FASE 1 (corretude de conflito no applicator) é barata, não mexe em schema pesado e **para a corrupção
que já acontece hoje** (LWW mentiroso, AtualizadoEm recarimbado, 23505 engolido, ponteiro que avança em
erro). Candidata a subir ANTES do resto do redesenho. FASE 0 (eixos/nó/tenant) e demais fases vêm depois.
