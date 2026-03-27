# Modulo: Fabricantes

## Entidade
**Tabela**: `Fabricantes` (herda BaseEntity)

| Campo | Tipo | Obrigatorio |
|-------|------|-------------|
| Nome | varchar(200) | Sim |

## Endpoints
- `GET /api/fabricantes` — lista
- `POST /api/fabricantes` — criar
- `PUT /api/fabricantes/{id}` — atualizar
- `DELETE /api/fabricantes/{id}` — excluir
- `GET /api/fabricantes/{id}/log` — historico

## Observacoes
- Entidade mais simples do sistema — apenas Nome
- Sem enderecos, contatos ou abas

## Permissao
- Codigo: `fabricantes`
