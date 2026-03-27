# Padroes de Codigo

## Como criar uma nova tela (modulo completo)

### Backend
1. **Entidade**: `backend/ZulexPharma.Domain/Entities/NovaEntidade.cs` (herda BaseEntity)
2. **AppDbContext**: adicionar DbSet + configuracao em OnModelCreating
3. **DTOs**: `backend/ZulexPharma.Application/DTOs/NomeModulo/NomeDto.cs` (List, Form, Detalhe)
4. **Interface**: `backend/ZulexPharma.Application/Interfaces/INomeService.cs`
5. **Service**: `backend/ZulexPharma.Infrastructure/Services/NomeService.cs`
   - Usar ILogAcaoService para auditoria
   - Usar padrao hard delete com fallback soft delete
   - Validar campos obrigatorios
6. **Controller**: `backend/ZulexPharma.API/Controllers/NomesController.cs`
   - `[Authorize]`, `[ApiController]`, `[Route("api/[controller]")]`
   - `[Permissao("codigo-tela", "c/i/a/e")]` em cada method
7. **DI**: registrar no `Program.cs`
8. **Migration**: `dotnet ef migrations add AddNome`
9. **Sync**: adicionar em `SyncService._tabelasSyncaveis`
10. **Permissoes**: adicionar em `telas-sistema.ts`

### Frontend
1. **Componente**: `frontend/src/app/modules/nome/`
   - `.ts` — signals, CRUD, permissoes, tokenLiberacao, sessionStorage
   - `.html` — sidebar tiles, grid, form, toolbar, modais
   - `.scss` — copiar de filiais.component.scss (estilo clean)
2. **Columns**: definicao de colunas inline ou em arquivo separado
   - **Sempre incluir coluna ID** como primeira coluna: `{ campo: 'id', label: 'ID', largura: 60, minLargura: 50, padrao: true }`
3. **Rota**: adicionar em `erp-shell.routes.ts`
4. **Dashboard**: adicionar tile com sigla
5. **Imports**: CommonModule, FormsModule, EnterTabDirective

### Padroes obrigatorios para telas CRUD

#### Botao Procurar (sidebar)
- O metodo `fechar()` DEVE chamar `this.carregar()` apos `this.modo.set('lista')`
- Garante que a grid recarrega dados frescos do backend ao voltar para a lista

#### Restauracao de abas (sessionStorage)
- Usar `restaurarAba(registro, ativar)` como metodo separado — NUNCA chamar `editar()` em loop
- `restaurarEstado()` itera os IDs salvos e chama `restaurarAba()` para cada um
- `restaurarAba()` recebe o registro DIRETO como parametro (nao depende do signal selecionado)
- Para modulos com HTTP async (colaboradores, fornecedores): faz GET individual por aba
- Para modulos sync (filiais, fabricantes, grupos, substancias): cria aba direto do dado local
- Skip de `verificarPermissao()` na restauracao (abas ja foram autorizadas)
- Sempre checar duplicata antes de adicionar: `if (this.abasEdicao().find(...)) return`

#### Icones
- **Sempre usar SVG inline** — nunca Unicode (×, &#9432;, &#9650;, etc.)
- **Nunca usar icon fonts** (Material Icons, Font Awesome) nas telas do ERP
- SVGs com `stroke="currentColor"` ou `fill="currentColor"` para herdar cor do contexto

### Convencoes de nomes
- Entidades: PascalCase singular (Filial, Colaborador)
- Tabelas: PascalCase plural (Filiais, Colaboradores)
- DbSets: PascalCase plural
- Controllers: PascalCase plural + Controller
- Rotas API: kebab-case plural (/api/colaboradores)
- Componentes Angular: kebab-case (colaboradores.component.ts)
- Permissoes: kebab-case (gerenciar-produtos)
- Signals: camelCase
- TELA (log): nome do modulo ("Filiais", "Colaboradores")
- ENTIDADE (log): nome da entidade ("Filial", "Colaborador")
