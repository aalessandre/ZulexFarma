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

### GAP do cursor (BUG PRE-EXISTENTE, ativo — nao corrigido)
- O PULL usa `Id > ultimoId`. Mas o `SyncFila.Id` e' atribuido no **INSERT** e so' fica **visivel no
  COMMIT**. Com duas transacoes concorrentes (Id 101 e 102) em que a 102 commita primeiro, um pull nesse
  instante serve a 102, o no crava o ponteiro em 102, e a 101 — ao commitar depois — **nunca e' entregue**.
  Perda SILENCIOSA de operacao sob escrita concorrente (varios terminais gravando junto).
- O ponteiro-avanca-sempre + quarentena cobrem falha de APLICACAO, nao esse gap de VISIBILIDADE.
- Cura: servir so' abaixo de um horizonte de estabilidade (`pg_snapshot_xmin(pg_current_snapshot())`)
  ou provar entrega por no (ack). Enquanto nao houver, a retencao NAO pode voltar.

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
