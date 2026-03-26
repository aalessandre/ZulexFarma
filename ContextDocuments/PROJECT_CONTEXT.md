# ZulexPharma ERP — Contexto do Projeto

## Stack Definida
- **Backend:** ASP.NET Core Web API (.NET 9) + C#
- **ORM:** Entity Framework Core 9.0.4
- **Banco:** PostgreSQL 16 — `zulexpharma_db` — `localhost:5432` — user: `postgres`
- **Frontend:** Angular 21 (template MaterialPro base, mas telas principais do zero)
- **CSS:** Tailwind + SCSS customizado por componente
- **Ícones:** SVGs inline (subset Tabler Icons)
- **Autenticação:** JWT (8h) — BCrypt para senhas
- **Logs:** Serilog (arquivo diário + console) + tabela `LogsErro` no banco

## Estrutura de Pastas
```
ErpPharma/
├── backend/
│   ├── ZulexPharma.API          → Controllers, Middleware, Program.cs
│   ├── ZulexPharma.Application  → DTOs, Interfaces
│   ├── ZulexPharma.Domain       → Entities, Enums
│   └── ZulexPharma.Infrastructure → EF Core, Repos, Services (AuthService)
│
├── frontend/                    → Angular 21 (base template MaterialPro)
│   └── src/app/
│       ├── core/                → AuthService, Guards, Interceptors, Models
│       ├── modules/dashboard/   → Tela de tiles (do zero, estilo InovaFarma)
│       ├── pages/authentication/side-login/ → Tela de login (do zero)
│       └── environments/        → environment.ts (apiUrl: localhost:5000/api)
│
├── ContextDocuments/            → Este arquivo + MEMORY.md
└── frontend-template/           → Template original (referência)
```

## Entidades do Banco (migration: InitialCreate)
- `Filiais` — multi-filiais, IsMatriz
- `GruposUsuario` — grupos de acesso
- `GruposPermissao` — permissões por bloco/tela (RBAC)
- `Usuarios` — login, senhaHash (BCrypt), grupo, filial
- `LogsErro` — erros com usuário, tela, função, stacktrace
- `LogsAcao` — auditoria: antes/depois, usuário que liberou

## Seed inicial (criado automaticamente ao subir API)
- Filial: `001 — ZulexPharma Farmácia LTDA`
- Grupo: `Administrador`
- Usuário: `admin` / `admin123`

## Rotas principais
- Backend: `http://localhost:5000/api/auth/login`
- Frontend: `http://localhost:4200`
- Login: `/authentication/login`
- Dashboard: `/dashboard` (protegido por authGuard)

## Decisões de UI
- Template MaterialPro NÃO é usado para as telas principais
- Telas principais (login, dashboard) são do zero com SCSS customizado
- Layout inspirado no InovaFarma: sem sidebar, full-screen tiles
- Tiles: 4 blocos coloridos — Movimento(cyan), Cadastros(laranja), Relatórios(roxo), Manutenção(amarelo)
- Topbar fixa com: logo, nome filial, pesquisa, avatar, ícones ação, logout

## Status dos Módulos
- [x] Estrutura backend (Clean Architecture)
- [x] EF Core + PostgreSQL + migrations
- [x] JWT + BCrypt
- [x] Serilog (arquivo + console)
- [x] ErrorHandlingMiddleware
- [x] AuthController (POST /api/auth/login)
- [x] Tela de Login (estilo InovaFarma)
- [x] Dashboard de tiles (estilo InovaFarma)
- [x] AuthService Angular + Guard + Interceptor
- [ ] CRUD Usuários + Grupos
- [ ] CRUD Filiais
- [ ] Configurações Gerais
- [ ] Tela de sub-módulo (layout ERP com sidebar esquerda + toolbar inferior)

## Permissões Claude Code
- Write e Edit: liberados sem confirmação (settings.json global)
