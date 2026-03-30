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
3. SaveChangesAsync gera Codigo visivel e seta FilialOrigemId
4. Apos save, insere na SyncFila (I/U/D + JSON com Id global)

### PUSH (PC -> Railway, a cada 30s)
1. SELECT SyncFila WHERE Enviado = false LIMIT 100
2. POST /api/sync/enviar (lote de operacoes)
3. Railway guarda na sua SyncFila (para outras filiais)
4. PC marca Enviado = true

### PULL (Railway -> PC, a cada 30s)
1. GET /api/sync/receber?filialId=X&ultimoId=Y
2. Railway retorna operacoes de OUTRAS filiais (FilialOrigemId != X)
3. PC aplica: I=insert (por Id), U=update (por Id), D=delete (por Id)
4. INSERT preserva o Id original (é globalmente unico)

### Limpeza (automatica)
- DELETE SyncFila WHERE Enviado = true AND EnviadoEm < (hoje - X dias)
- Configuravel: sync.limpeza.dias (default 7, min 5, max 15)

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
