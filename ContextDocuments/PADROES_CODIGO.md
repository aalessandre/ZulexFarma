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
3. **Rota**: adicionar em `erp-shell.routes.ts`
4. **Dashboard**: adicionar tile com sigla
5. **Imports**: CommonModule, FormsModule, EnterTabDirective

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
