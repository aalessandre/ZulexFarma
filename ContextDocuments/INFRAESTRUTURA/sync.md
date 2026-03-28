# Infraestrutura: Sincronizacao

## Arquitetura
- **Railway (nuvem)** = servidor central (receptor passivo)
- **PC local** = cada farmacia roda backend + PostgreSQL local
- Sync bidirecional: PC local ↔ Railway ↔ outros PCs
- Autenticacao no central via usuario SISTEMA (senha rotativa diaria)

## SyncService (engine generico)
- `ObterAlteracoes(tabela, versaoDesde, filialId)` — puxa registros alterados (PULL central→local)
- `ObterAlteracoesLocais(tabela, versaoDesde, filialOrigemId)` — puxa apenas registros desta filial (PUSH local→central)
- `AplicarAlteracoes(tabela, registrosJson)` — aplica com last-write-wins
- `ObterStatus(filialId)` — status por tabela
- `AtualizarControle(filialId, tabela, ...)` — atualiza SyncControle
- `ResetarSequence(tabela)` — reseta sequence PostgreSQL apos PULL (evita conflito de PK)

## Tabelas sincronizaveis
Filiais, Pessoas, PessoasContato, PessoasEndereco, Colaboradores, Usuarios, UsuariosGrupos, UsuariosGruposPermissao, UsuarioFilialGrupos, Fornecedores, Fabricantes, GruposPrincipais, GruposProdutos, SubGrupos, Secoes, Substancias

## API endpoints
- `GET /api/sync/tabelas` — lista tabelas
- `GET /api/sync/receber?tabela=X&versaoDesde=Y&filialId=Z` — pull
- `POST /api/sync/enviar` — push
- `GET /api/sync/status/{filialId}` — status
- `GET /api/sync/servico` — status do background service (rodando, ultima execucao, erros)
- `POST /api/sync/executar/{filialId}` — forcar sync

## SyncBackgroundService (completo e funcional)
- Roda periodicamente no PC local
- Config: `Sync:Habilitado`, `Sync:IntervaloSegundos`, `Sync:FilialLocalId`, `Sync:UrlCentral`
- Desabilitado por padrao (central Railway nao precisa)
- Ciclo: autentica como SISTEMA → para cada tabela: PUSH local→central, PULL central→local
- Cache de JWT com renovacao automatica (margem 5min)
- Profiles por PC: `appsettings.pc1.json`, `appsettings.pc2.json`
- Variavel de ambiente: `SYNC_PROFILE=pc1` (ou pc2) antes de `dotnet run`
- Startup: `$env:SYNC_PROFILE = "pc1"` (PowerShell) → `dotnet run`

## VersaoSync (mecanismo de versionamento)
- Usa timestamp em milissegundos (`DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()`)
- Monotonicamente crescente (lock + Math.Max para garantir unicidade)
- Atribuido tanto em Added quanto em Modified no SaveChangesAsync
- PULL local usa `SuspenderAutoSync=true` para preservar VersaoSync do central (evita echo loop)
- Central (Railway) NAO usa SuspenderAutoSync — incrementa VersaoSync normalmente ao receber PUSH

## FilialOrigemId
- Identifica de qual farmacia/servidor o registro foi criado
- Fonte primaria: `Sync:FilialLocalId` da config (identifica o servidor)
- Fallback: `filialId` do JWT do usuario
- PUSH envia apenas registros onde `FilialOrigemId == FilialLocalId`
- PULL filtra registros onde `FilialOrigemId != filialId` solicitante (evita echo)

## Reset de sequences
- Apos PULL, reseta `pg_get_serial_sequence` para `MAX(Id)+1`
- Evita conflito de PK quando registros sincronizados tem IDs explicitos

## Colisao de IDs entre filiais
- Dois PCs podem gerar o mesmo Id (sequences independentes)
- O sync detecta: mesmo Id, FilialOrigemId diferente = registros DIFERENTES
- Quando isso ocorre, o registro remoto recebe um novo Id local (Id=0, EF gera)
- Nao eh erro — eh comportamento esperado em bancos distribuidos
- O registro original mantem seu Id, o remoto ganha Id novo
- Log: "registro Id conflitante, inserido com novo Id"

## Validacao de campos unicos no sync
- Antes de inserir registro remoto, verifica campos marcados como Unico no DD
- Se CPF/CNPJ/Login ja existe localmente, rejeita e loga como conflito
- Usa reflection para ler os campos do DD dinamicamente

## Resolucao de conflitos
- Last-write-wins baseado em `AtualizadoEm`

## Tela de gerenciamento
- Rota: `/erp/sync`
- Grid: Tabela, Status, Ultima Sync, Versoes, Pendentes
- Botao forcar sync
