# Modulo: Substancias

## Entidade
**Tabela**: `Substancias` (herda BaseEntity)

| Campo | Tipo | Obrigatorio |
|-------|------|-------------|
| Nome | varchar(200) | Sim |
| Dcb | varchar(50) | Nao |
| Cas | varchar(50) | Nao |
| ControleEspecialSngpc | bool | Nao (padrao: false) |
| ClasseTerapeutica | varchar | Nao |

## Observacoes
- Criado pelo PC2
- Entidade de substancias farmaceuticas com codigo DCB (Denominacao Comum Brasileira), codigo CAS e controle SNGPC
- Tile no dashboard: SB (Cadastros)

## Permissao
- Codigo: `substancias`
