# Infraestrutura: Sincronizacao v2 (Fila de Operacoes)

## Arquitetura
- **Railway (nuvem)** = servidor central (hub de operacoes)
- **PC local** = cada farmacia roda backend + PostgreSQL local
- Cada operacao CRUD gera um registro na fila (SyncFila)
- PUSH: PC envia operacoes pendentes para Railway
- PULL: PC busca operacoes de outras filiais do Railway
- DELETE replica naturalmente (eh uma operacao na fila)

## Tabelas do Sync

### SyncFila (em cada PC e no Railway)
Fila de operacoes. Cada INSERT/UPDATE/DELETE gera um registro automaticamente.
- Id, Tabela, Operacao (I/U/D), RegistroId, RegistroCodigo
- DadosJson (snapshot JSON do registro, null em Delete)
- FilialOrigemId, CriadoEm, Enviado, EnviadoEm, Erro

### SequenciaLocal (so no PC, nao replica)
Controla o proximo sequencial do Codigo visivel por tabela.
- Id, Tabela, Ultimo

## Campo Codigo (identificador visivel)
- Formato: "FilialCodigo.Sequencial" (ex: "1.1", "2.115950")
- Unico global, amigavel para o usuario
- Gerado automaticamente no SaveChangesAsync para cada INSERT
- Tabelas sem sync (Configuracoes, SyncFila, etc) nao geram Codigo

## Identificadores
- **Id** = PK bigint, globalmente unico por faixa de filial
  - Filial 1: IDs 1.000.000.001 a 1.999.999.999
  - Filial 2: IDs 2.000.000.001 a 2.999.999.999
  - Formula: FilialCodigo * 1.000.000.000 + sequencial
  - Com 1000 inserts/dia por tabela, cada filial dura 2.739 anos
  - Configurado automaticamente no startup (ALTER TABLE ... RESTART WITH)
- **Codigo** = visivel, formato "FilialCodigo.Sequencial", apenas para exibicao humana
  - NAO usado internamente pelo sync nem por FKs
- Como os IDs sao globais, FKs funcionam direto entre filiais (sem remapeamento)

## Fluxo

### Cadastro
1. Usuario salva registro
2. PostgreSQL gera Id na faixa da filial (identity column)
3. SaveChangesAsync gera Codigo visivel e seta NoOrigemId (o NO/servidor de origem) + FilialDonoId
   (a filial DONA do dado, null = GLOBAL) — sao dois EIXOS diferentes, nao confundir
4. Apos save (mesma transacao — outbox atomico), insere na SyncFila (I/U/D + JSON com Id global)
   ja' com `OpUid` = Guid novo (identidade global e imutavel da op)

### PUSH (PC -> Railway, a cada 30s)
1. SELECT SyncFila WHERE Enviado = false LIMIT 100
2. POST /api/sync/enviar (lote de operacoes, cada uma com seu OpUid)
3. Railway APLICA no banco consolidado e RE-ENFILEIRA na sua SyncFila (para as outras filiais),
   gravando o MESMO OpUid — e' a chave de IDEMPOTENCIA: se o PUSH chegou mas a resposta se perdeu,
   o reenvio traz o mesmo OpUid e a central reconhece e NAO duplica a redistribuicao (indice parcial
   unico em OpUid). Op de no ANTIGO (sem OpUid) => sem dedup, comportamento antigo (nada e' descartado).
4. PC marca Enviado = true

### PULL (Railway -> PC, a cada 30s)
1. GET /api/sync/receber?filialId=X&filiais=...&ultimoId=Y   (filialId = codigo do NO, nome legado)
2. Railway retorna ops de OUTROS nos (anti-eco: NoOrigemId != X) e no ESCOPO do no
   (FilialDonoId null = GLOBAL, ou entre as filiais que o no atende)
3. PC aplica: I=insert (por Id), U=update (por Id, com LWW), D=delete (+ lapide anti-ressurreicao)
4. INSERT preserva o Id original (é globalmente unico — faixa por no)
5. Falha na aplicacao NAO some: vai pra SyncQuarentena (dead-letter) e e' drenada por retry.
   O ponteiro avanca sempre — ATENCAO ao "GAP do cursor" abaixo, que e' outro problema.

### Limpeza (MANUAL, e so' faz efeito no NO-FOLHA)
- NAO existe job automatico. O unico caminho e' o botao "Limpar antigos" do painel ->
  `POST /api/sync/limpar` -> `DELETE SyncFila WHERE Enviado = true AND EnviadoEm < (hoje - X dias)`.
- X = parametro explicito `?dias=` > config `sync.limpeza.dias` (slider da tela de Configuracoes,
  faixa 5-15) > 7. Piso de 1 dia (valor negativo inverteria o corte e apagaria tudo).
  Ate' 07/2026 essa config era MORTA (ninguem lia; o botao usava 7 fixo) — hoje o backend le de verdade.

### Retencao da fila CENTRAL (limitacao conhecida — a fila cresce sem teto)
- Na central o `/limpar` apaga ZERO: as linhas de REDISTRIBUICAO nascem `Enviado=false` e ficam assim
  PRA SEMPRE (a central nao faz PUSH; `Enviado=true` so' e' setado no no que empurra). Logo o filtro
  `Enviado = true` nunca casa e a SyncFila da Railway cresce indefinidamente. **Monitorar o tamanho.**
- E' DECISAO CONSCIENTE, nao esquecimento: a compactacao por "apaga Id <= MIN(cursor dos nos)" foi
  implementada e REVERTIDA em 07/2026 (revisao adversarial) porque a premissa e' falsa — ver o comentario
  em `SyncApplicator.cs`. Dois motivos independentes: (1) no que nunca puxou nao tem cursor e fica
  INVISIVEL pro MIN (a 1a pull pos-deploy compactava contra um no so'); (2) o cursor NAO PROVA consumo,
  porque `SyncFila.Id` sai no INSERT mas so' fica visivel no COMMIT (ver "GAP do cursor" abaixo).
- Pre-requisitos pra reativar: fechar o gap do cursor + registro EXPLICITO dos nos esperados (fail-closed)
  + marca-d'agua de compactacao exposta no /status + o /receber detectar "no pediu abaixo da marca" e
  responder GAP em vez de um lote parcial silencioso.

### RESSURREICAO por delete LOCAL — CORRIGIDO em 07/2026 (escopo: so' BaseEntity)
- O outbox agora crava a lapide TAMBEM no delete LOCAL (upsert ATOMICO `ON CONFLICT` no
  `AppDbContext.SaveChangesAsync`, na mesma transacao do dado, com o MESMO instante que vai no `CriadoEm` da
  op "D"). Foi ON CONFLICT e nao query-then-add porque uma corrida (23505) dentro da tx do outbox reverteria
  **o delete do usuario**. O `WHERE ... < EXCLUDED` preserva a morte mais nova (mesmo LWW do original).
- A faxina (`PurgarTombstonesAsync`) SAIU do `Receber()` e virou `PurgarLapidesSePreciso` (1x/hora) no laco
  do SyncBackgroundService: antes ela dependia do pull por ACIDENTE (a lapide so' nascia aplicando "D"
  remoto). Com a lapide nascendo no delete local, num no de loja unica o pull volta sempre vazio e a faxina
  nunca rodaria. LIMITACAO: no com `Sync:Habilitado=false` nao roda o laco (mas ali a SyncFila tambem enche
  sem ninguem esvaziar — pre-existente).
- **NAO cobre filho POCO** (VendaItem, ClienteConvenio, AdquirenteTarifa...): eles nao geram op nem lapide,
  viajam no JSON do pai. Ver RECONCILIACAO DE FILHOS abaixo.

### RECONCILIACAO DE FILHOS (pendente — orfao/duplicacao em agregado)
- `UpsertFilhosPocoAsync` e' append/update-only: filho POCO REMOVIDO no pai continua VIVO no destino.
- Pior que "orfao": os services fazem `RemoveRange` de TODOS os filhos e re-adicionam do DTO, e os novos
  ganham Ids NOVOS da faixa daquele no. Entao uma edicao ROTINEIRA de cliente ja' DUPLICA o conjunto no par
  (o destino fica com os Ids velhos + os novos), e se o par depois editar o mesmo registro, o JSON dele leva
  os orfaos de volta = ressurreicao efetiva do filho que o outro tinha apagado.
- Tentativa de 07/2026 (delete-missing) foi REVERTIDA: apagar da replica o filho que "nao veio no JSON"
  assume JSON COMPLETO, mas varios caminhos salvam o pai SEM Include dos filhos (FinalizarAsync nao carrega
  Itens.Descontos; CancelarAsync sem Include; fallback excluir->desativar) -> apagaria dado LEGITIMO.
  Cura certa: garantir o grafo completo na serializacao (forcar Load das colecoes POCO no outbox) OU o
  outbox NULLAR colecao nao-IsLoaded e o applicator reconciliar SO' as colecoes com chave PRESENTE no JSON.

### Filho BaseEntity apagado por CASCATA do banco — CORRIGIDO em 07/2026 (no OUTBOX, num lugar so')
- ERA: se o service fazia `Remove(pai)` sem carregar um filho BaseEntity com FK `Cascade`, o Postgres
  cascateava mas o filho nunca virava entry no ChangeTracker -> **nao gerava "D" nem lapide** -> ficava VIVO
  nos outros nos (divergencia) e um "I" atrasado dele levava 23503 la'.
- ESCALA REAL (mapeamento do repo): **24 call-sites** com o bug contra 6 corretos. Piores: `PerdaService`
  (6 filhos), `FilialService` (3 + cadeia), e `CampanhaFidelidadeItem` (SETE pais diferentes cascateiam pra
  ele; so' o dono real carrega). `ProdutoService` nao tem exclusao hoje — no dia que tiver, ~8 de uma vez.
  ATENCAO: NAO existe politica global de Restrict neste projeto (isso e' o ZulexSac) — todo
  `OnDelete(Cascade)` das Configurations vale de verdade.
- CURA (`AppDbContext.CarregarFilhosCascataAsync`, chamado no inicio do outbox): antes de coletar as ops,
  CARREGA os filhos que o banco apagaria por cascata (so' os que REPLICAM: BaseEntity + fora de
  `_tabelasSemSync`), e chama `ChangeTracker.CascadeChanges()` — a cascata do EF alcanca o que esta'
  RASTREADO, entao cada filho carregado vira Deleted e o laco do outbox gera "D" + lapide pra ele de graca.
  Ate' 5 niveis (neto: Venda->Entrega->EntregaEvento). Sem delete no save, sai de graca.
  POR QUE no outbox e nao nos 24 services: o 25o nasceria bugado.
- VERIFICADO EM RUNTIME (harness com rollback, nao so' build): Ncm + 3 NcmFederal, ChangeTracker limpo
  (0 filhos rastreados), `Remove(pai)` sem Include -> 4 ops (`NcmFederais D` x3 + `Ncms D` x1) e 4 lapides.
  Antes do fix: 1 op e 1 lapide.
- Junto: `AplicarCabecalhoAsync` passou a classificar 23503 como **PrecisaRetry** (teto 240 = a drenagem
  resolve quando o pai chegar) em vez de deixar subir como "Erro" (teto 5, que aposentava a op por um
  problema de ORDEM). O `UpsertFilhosPocoAsync` ja' fazia isso; o cabecalho estava de fora.

### GerarCodigo: fallback da sequence era ILUSAO — CORRIGIDO em 07/2026
- O comentario dizia que o fallback "se auto-cura se a sequence faltar". NAO se curava: no Postgres um
  statement que falha ABORTA a transacao inteira, entao o 42P01 do `nextval` envenenava a tx e o proprio
  `CriarSequenceCodigoAsync` do `catch` morria com 25P02 — derrubando a operacao (incluindo uma VENDA, se a
  finalizacao tivesse aberto a tx). Provado na pratica: um harness num banco sem as sequences falhou com
  25P02 exatamente assim.
- Cura: `SAVEPOINT` antes do nextval, **so' quando ha' transacao ambiente** (sem tx cada statement e' sua
  propria transacao e nada e' envenenado -> zero round-trip a mais no caminho quente comum).
- Lembrete: as sequences nascem no BOOT (`DatabaseSeeder.CriarSequencesCodigo` varre information_schema por
  TODA tabela com coluna `Codigo`), entao pos-boot o fallback e' caminho morto — mas agora, se disparar,
  funciona de verdade.
- A lapide (SyncTombstone) so' e' gravada ao APLICAR um delete REMOTO: `RegistrarTombstoneAsync` tem UM
  unico chamador, `SyncApplicator.AplicarOperacaoAsync` no ramo `operacao == "D"`. O OUTBOX (AppDbContext)
  NAO grava lapide quando o usuario apaga um registro LOCALMENTE — ele so' gera a op "D" pra fila.
- Consequencia: o no que apagou fica SEM lapide do proprio delete, entao um "I" remoto ATRASADO do mesmo
  registro passa pelo teste `BuscarTombstoneAsync` (nao acha nada) e RE-INSERE o registro.
  Trace: B apaga X as 10:00 (sem lapide local). A tinha gerado "I" de X as 09:59, ainda em transito.
  B puxa as 10:01 -> X nao existe -> sem lapide -> INSERT -> **X ressuscita em B**. O "D" do B chega em A,
  que apaga X e grava lapide (caminho remoto). Resultado: X morto em A, VIVO em B -> divergencia PERMANENTE.
- Cura: o outbox deve gravar a lapide TAMBEM no delete local (Tabela, RegistroId, DeletadoEm=Agora(),
  NoOrigemId=self), com upsert (a chave (Tabela,RegistroId) e' unica — cuidado pra nao quebrar o delete do
  usuario com 23505). Ai' o LWW da lapide (DeletadoEm >= incomingTs => Stale) passa a proteger os dois lados.
- Achado em 07/2026 pela revisao adversarial da varredura (a varredura AMPLIAVA esta janela ao re-servir ops
  velhas, mas o bug independe dela).

### GAP do cursor (BUG PRE-EXISTENTE, ativo — nao corrigido)
- O PULL usa `Id > ultimoId`. Mas o `SyncFila.Id` e' atribuido no **INSERT** e so' fica **visivel no
  COMMIT**. Com duas transacoes concorrentes (Id 101 e 102) em que a 102 commita primeiro, um pull nesse
  instante serve a 102, o no crava o ponteiro em 102, e a 101 — ao commitar depois — **nunca e' entregue**.
  Perda SILENCIOSA de operacao sob escrita concorrente (varios terminais gravando junto).
- O ponteiro-avanca-sempre + quarentena cobrem falha de APLICACAO, nao esse gap de VISIBILIDADE.
- O PUSH e' IMUNE (usa flag `!Enviado`, nao cursor): linha que commita tarde e' pega no ciclo seguinte.
  O buraco e' so' no PULL (`/receber`) — o conserto e' localizado.
- **CUIDADO — a prescricao "use pg_snapshot_xmin" que estava aqui era INCOMPLETA e faria o proximo repetir
  o bug.** O horizonte de estabilidade so' fecha o gap **SE O CURSOR VIRAR O XID**. Mantendo o cursor em
  `Id`, o gap CONTINUA: as duas ordens sao INDEPENDENTES (o xid nasce na 1a escrita da tx; o `Id` sai no
  insert do outbox, DEPOIS — e aqui a distancia e' estrutural: o save do negocio e o insert do outbox tem
  serializacao JSON e upsert de lapide no meio). Contraexemplo: tx S (xid 500) faz outbox DEPOIS da tx R
  (xid 501) => Id_R=101 < Id_S=102; com R ainda rodando, h=501, a S passa (500<501), cursor vai pra 102, e
  a 101 fica pra tras quando R commitar.
- As duas curas que FUNCIONAM:
  * (A) **cursor = xid**: coluna `TxId xid8 DEFAULT pg_current_xact_id()` (xid8 = 64 bits, imune a
    wraparound; o `xmin` de sistema e' 32 bits e NAO serve) + servir `TxId >= cursor AND TxId < h`.
    CUSTO: transacao longa — ate' alheia ao sync (relatorio, autovacuum) — PARALISA a fila inteira.
  * (B) **PREFERIDA — publicador + `SeqEntrega`**: numerar por `nextval` SO' as linhas ja' COMMITADAS (o
    publicador nao enxerga as em voo) sob advisory lock; cursor passa a ser `SeqEntrega`. Linha que commita
    tarde so' pega numero MAIOR na rodada seguinte -> **tx longa nao trava nada** (superior ao xid8). Buraco
    na numeracao e' inofensivo (nextval nao e' transacional; cursor e' `>`). Obstaculo: a central nao roda
    background loop -> numerar de forma OPORTUNISTA no `/receber` (e/ou fim do `/enviar`) sob
    `pg_try_advisory_xact_lock`: quem pega o lock numera, quem nao pega serve o que ja' esta' numerado.
- Enquanto nao houver uma das duas, a retencao NAO pode voltar.
- **Retrato completo do subsistema (objetivos, infra, erros cometidos, pendencias): `synAteAqui.md`.**
- TENTATIVA REJEITADA (07/2026) — "VARREDURA por tempo": re-puxar periodicamente a janela recente por
  CriadoEm (ignorando o cursor) e reaplicar (idempotente). Foi implementada e REVERTIDA; a revisao
  adversarial derrubou com 5 criticos. Ficam registrados pra ninguem tentar de novo sem resolver:
  1. **CriadoEm e' o relogio da ORIGEM** (o /enviar copia op.CriadoEm), nao a hora de chegada na central.
     Op de no que ficou offline JA' NASCE fora da janela. Pior: a cobertura e' INVERSAMENTE correlacionada
     com o risco — lote grande de PUSH (backlog pos-queda) demora mais pra commitar => maior chance de ser
     pulado => e sao justo as ops mais VELHAS => as menos cobertas. Protege o caso benigno, falha no maligno.
     (Cura seria coluna nova `RecebidoEmCentral` carimbada no /enviar + janelar por ela.)
  2. **CriadoEm nao tem indice** -> full scan da SyncFila central (que cresce sem teto) a cada varredura,
     por no. Painel verde enquanto o banco afoga.
  3. **Nao-Aplicado descartado em silencio**: a op do gap e' a UNICA que o PULL nunca entregara' (o ponteiro
     passou por cima); se ela volta Conflito/PrecisaRetry e nao vai pra quarentena, morre quando a janela
     expira = perda permanente com o painel dizendo "0 recuperadas = saudavel".
  4. **RESSURREICAO**: re-servir op velha reintroduz "I" de registro apagado LOCALMENTE (que nao tem lapide
     — ver bug acima) => modo de falha NOVO. Isso MATA a justificativa da varredura ("estritamente aditiva").
  5. Mesmo consertando tudo, continua probabilistica: PC de farmacia que desliga no fim do expediente dentro
     da janela perde a op de qualquer jeito; saturacao + Take(limite) truncam a cobertura em silencio.
  CONCLUSAO: band-aid com muitos furos. O caminho certo e' a cura de RAIZ (horizonte de estabilidade), que
  IMPEDE o gap em vez de tentar recuperar depois. Custo aceito: transacao longa aberta trava o sync (falha
  RUIDOSA e visivel, e transacao longa e' bug por si so') — muito melhor que perda silenciosa.

## API Endpoints
- POST /api/sync/enviar — PC envia lote de operacoes
- GET /api/sync/receber — PC puxa operacoes pendentes
- GET /api/sync/status — pendentes, erros, ultimo sync
- GET /api/sync/fila — listagem paginada com filtros (para tela)
- POST /api/sync/forcar-envio — forca envio no proximo ciclo
- POST /api/sync/limpar — limpa registros antigos

## Configuracoes
```json
{
  "Filial": { "Codigo": 1 },
  "Sync": {
    "Habilitado": false,
    "IntervaloSegundos": 30,
    "UrlCentral": "",
    "LoteTamanho": 100,
    "LimpezaDias": 7
  }
}
```

Editaveis na tela Configuracoes do sistema:
- sync.intervalo.segundos (min 7, max 300)
- sync.lote.tamanho (min 50, max 150)
- sync.limpeza.dias (min 5, max 15)

## Tabelas que NÃO entram na fila
- Configuracoes (local por filial)
- DicionarioTabelas, DicionarioRevisoes (ferramenta dev)
- SyncFila (controle interno)
- SequenciaLocal (controle interno)

## IDs globais por faixa de filial
- Cada filial tem uma faixa exclusiva de IDs (1 bilhao por filial)
- IDs NUNCA colidem entre filiais
- FKs (PessoaId, ColaboradorId, etc) sao validas em qualquer PC
- Ao aplicar INSERT remoto: usa o Id original (é unico global)
- Ao aplicar UPDATE remoto: busca por Id, atualiza
- Ao aplicar DELETE remoto: busca por Id, remove
- Nao precisa de remapeamento de FKs

## Autenticacao
- Background service autentica no Railway como usuario SISTEMA
- Senha rotativa diaria: SHA256(YYYYMMDD + SistemaKey)[0..8]
- JWT cacheado com renovacao 5min antes do vencimento
