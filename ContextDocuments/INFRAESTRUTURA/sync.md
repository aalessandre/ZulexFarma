# Infraestrutura: Sincronizacao

## SyncService (engine generico)
- `ObterAlteracoes(tabela, versaoDesde, filialId)` — puxa registros alterados
- `AplicarAlteracoes(tabela, registrosJson)` — aplica com last-write-wins
- `ObterStatus(filialId)` — status por tabela
- `AtualizarControle(filialId, tabela, ...)` — atualiza SyncControle

## Tabelas sincronizaveis
Filiais, Pessoas, PessoasContato, PessoasEndereco, Colaboradores, Usuarios, UsuariosGrupos, UsuariosGruposPermissao, UsuarioFilialGrupos, Fornecedores, Fabricantes, GruposPrincipais, GruposProdutos, SubGrupos, Secoes

## API endpoints
- `GET /api/sync/tabelas` — lista tabelas
- `GET /api/sync/receber?tabela=X&versaoDesde=Y&filialId=Z` — pull
- `POST /api/sync/enviar` — push
- `GET /api/sync/status/{filialId}` — status
- `POST /api/sync/executar/{filialId}` — forcar sync

## Background Service
- `SyncBackgroundService` — roda periodicamente
- Config: `Sync:Habilitado`, `Sync:IntervaloSegundos`, `Sync:FilialLocalId`, `Sync:UrlCentral`
- Desabilitado por padrao

## Resolucao de conflitos
- Last-write-wins baseado em `AtualizadoEm`

## Tela de gerenciamento
- Rota: `/erp/sync`
- Grid: Tabela, Status, Ultima Sync, Versoes, Pendentes
- Botao forcar sync
