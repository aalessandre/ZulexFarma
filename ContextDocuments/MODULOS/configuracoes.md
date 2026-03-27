# Modulo: Configuracoes

## Entidade
**Tabela**: `Configuracoes` (NAO herda BaseEntity)

| Campo | Tipo |
|-------|------|
| Id | bigint |
| Chave | varchar(100), unico |
| Valor | varchar(500) |
| Descricao | varchar(200) |

## Chaves configuradas
| Chave | Padrao | Descricao |
|-------|--------|-----------|
| sessao.maxima.minutos | 480 | Tempo maximo de sessao (0=sem limite) |
| sessao.inatividade.minutos | 10 | Tempo de inatividade (0=sem limite) |
| sistema.nome | ZulexPharma | Nome exibido no topo |

## Endpoints
- `GET /api/configuracoes` — lista todas
- `PUT /api/configuracoes` — salvar (upsert por chave)
- `GET /api/configuracoes/sessao` — tempos de sessao (anonimo)

## Frontend
- Tela com secoes: Sistema, Controle de Sessao
- Tooltips informativos nos campos de tempo
