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
- **Id** = tecnico, PK, auto-increment local. Pode variar entre PCs.
- **Codigo** = visivel, unico global. Identifica o registro em todas as filiais.
- O sync usa Codigo para encontrar registros ao aplicar operacoes remotas.

## Fluxo

### Cadastro
1. Usuario salva registro
2. SaveChangesAsync gera Codigo e seta FilialOrigemId
3. Apos save, insere na SyncFila (I/U/D + JSON)

### PUSH (PC -> Railway, a cada 30s)
1. SELECT SyncFila WHERE Enviado = false LIMIT 100
2. POST /api/sync/enviar (lote de operacoes)
3. Railway guarda na sua SyncFila (para outras filiais)
4. PC marca Enviado = true

### PULL (Railway -> PC, a cada 30s)
1. GET /api/sync/receber?filialId=X&ultimoId=Y
2. Railway retorna operacoes de OUTRAS filiais (FilialOrigemId != X)
3. PC aplica: I=insert (por Codigo), U=update (por Codigo), D=delete (por Codigo)
4. INSERT usa Id=0 (EF gera novo Id local), mantem Codigo original

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

## Colisao de IDs
- Cada PC gera seus proprios IDs (auto-increment local)
- IDs podem ser diferentes entre PCs para o mesmo registro
- O Codigo eh o identificador universal
- Ao aplicar INSERT remoto: Id=0 (EF gera local), Codigo preservado
- Ao aplicar UPDATE remoto: busca por Codigo, atualiza mantendo Id local
- Ao aplicar DELETE remoto: busca por Codigo, remove

## Autenticacao
- Background service autentica no Railway como usuario SISTEMA
- Senha rotativa diaria: SHA256(YYYYMMDD + SistemaKey)[0..8]
- JWT cacheado com renovacao 5min antes do vencimento
