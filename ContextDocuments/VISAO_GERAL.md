# ZulexPharma ERP — Visao Geral

## O que e
ERP web para farmacias, com foco em alta disponibilidade (funcionar offline), multi-filial e replicacao de dados.

## Stack
- **Backend**: .NET 9, C#, PostgreSQL, EF Core, JWT, BCrypt
- **Frontend**: Angular 19, Signals, Standalone Components, SCSS
- **Deploy**: Railway (backend + frontend + PostgreSQL)
- **Repositorio**: https://github.com/aalessandre/ZulexFarma

## Estrutura do Repositorio
```
ErpPharma/
├── backend/
│   ├── ZulexPharma.API/          ← Controllers, Filters, Program.cs
│   ├── ZulexPharma.Application/  ← DTOs, Interfaces
│   ├── ZulexPharma.Domain/       ← Entidades, Enums
│   └── ZulexPharma.Infrastructure/ ← Services, Data (DbContext, Migrations)
├── frontend/
│   └── src/app/
│       ├── core/                 ← Services (auth, tab, modal, settings), Directives
│       ├── modules/              ← Telas do ERP (filiais, colaboradores, etc)
│       └── pages/                ← Login, paginas publicas
└── ContextDocuments/             ← Esta documentacao
```

## Decisoes Arquiteturais
- **Banco unico por cliente** com `FilialId` nas tabelas por filial
- **Sync via API REST** entre servidor local e nuvem (nao replicacao nativa PG)
- **BaseEntity** com `VersaoSync` e `FilialOrigemId` em todas as tabelas
- **Permissoes** enforcadas via atributo `[Permissao]` + JWT claims
- **Liberacao por senha** de supervisor quando usuario sem permissao
- **Log de auditoria** completo com `UsuarioLiberouId`

## Branches
- `main` — producao (Railway deploya automaticamente)
- `dev-pc1` — desenvolvimento PC1
- `dev-pc2` — desenvolvimento PC2

## Banco de Dados
- **Local**: PostgreSQL localhost:5432, database `zulexpharma_db`
- **Nuvem**: Railway PostgreSQL (connection string nas variaveis do Railway)
- **Migrations**: EF Core, aplicadas via `dotnet ef database update`

## Usuarios padrao
- **admin** / admin123 — Administrador
- **SISTEMA** — senha rotativa diaria (SHA256 do dia + chave)
