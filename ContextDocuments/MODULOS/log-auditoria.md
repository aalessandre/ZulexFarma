# Modulo: Log de Auditoria

## Entidade
**Tabela**: `LogsAcao` (herda BaseEntity)

| Campo | Tipo | Descricao |
|-------|------|-----------|
| RealizadoEm | DateTime | Timestamp UTC |
| UsuarioId | FK -> Usuario | Quem fez a acao |
| Tela | varchar(100) | Nome da tela |
| Acao | varchar(50) | CRIACAO, ALTERACAO, EXCLUSAO, DESATIVACAO, LIBERACAO POR SENHA |
| Entidade | varchar | Tipo do registro |
| RegistroId | varchar | ID do registro |
| ValoresAnteriores | JSON | Snapshot antes |
| ValoresNovos | JSON | Snapshot depois |
| LiberacaoPorSenha | bool | Se precisou de senha |
| UsuarioLiberouId | FK -> Usuario | Quem liberou |

## Endpoints
- `GET /api/logs` — lista paginada com filtros

## Filtros disponiveis
- Data Inicio/Fim (ajuste timezone BR)
- Tela (case-insensitive)
- Acao (contains para pegar liberacoes)
- Usuario (nome ou login)
- Liberacao por senha (sim/nao)

## Frontend
- Grid padronizado (colunas configuraveis, resize, sort)
- Linhas expansiveis mostrando Valores Anteriores/Novos
- Campos alterados destacados em **laranja**
- Badges coloridos por tipo de acao
- Paginacao

## Permissao
- Codigo: `log-geral`
