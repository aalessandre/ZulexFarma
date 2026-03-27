# Modulo: Gerenciar Produtos

## Tela unificada com abas coloridas
Rota: `/erp/gerenciar-produtos`
Tile no dashboard: GP (Cadastros)

### Abas
| Aba | Cor | API | Entidade | Status |
|-----|-----|-----|----------|--------|
| Produtos | Azul (#4a90d9) | — | — | Placeholder |
| Grupo Principal | Coral (#e8845f) | `/api/grupos-principais` | GrupoPrincipal | Implementado |
| Grupo | Amarelo (#f0c75e) | `/api/grupos-produtos` | GrupoProduto | Implementado |
| Sub Grupo | Verde (#7bc67e) | `/api/sub-grupos` | SubGrupo | Implementado |
| Secao | Teal (#5bb8c9) | `/api/secoes` | Secao | Implementado |
| Familia | Lilas (#b088c9) | — | — | Placeholder |

### Campos compartilhados (ClassificacaoProdutoBase)
| Campo | Tipo | Padrao |
|-------|------|--------|
| Nome | varchar(200) | — |
| ComissaoPercentual | numeric(5,2) | 0 |
| DescontoMinimo | numeric(5,2) | 0 |
| DescontoMaximo | numeric(5,2) | 0 |
| DescontoMaximoComSenha | numeric(5,2) | 0 |
| ProjecaoLucro | numeric(5,2) | 30 |
| MarkupPadrao | numeric(5,2) | 50 |
| Priorizar | varchar(20) | null (MARKUP ou PROJECAO) |
| ControlarLotesVencimento | bool | false |
| InformarPrescritorVenda | bool | false |
| ImprimirEtiqueta | bool | false |
| PermitirDescontoPrazo | bool | false |
| PermitirPromocao | bool | false |
| PermitirDescontosProgressivos | bool | false |

### Arquitetura
- `ClassificacaoProdutoBase` — classe base abstrata
- `ClassificacaoProdutoService<T>` — service generico
- 4 controllers concretos herdam de `ClassificacaoProdutoControllerBase<T>`
- Frontend: uma unica tela que muda a API baseado na aba ativa

## Permissao
- Codigo: `gerenciar-produtos`
