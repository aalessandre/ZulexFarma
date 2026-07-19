# CLAUDE.md — ErpPharma / ZulexPharma

ERP de farmácia multi-filial com **replicação multi-master própria** (não é replicação nativa do
Postgres). Backend .NET 9 + EF Core 9 + PostgreSQL (`backend/`: API/Application/Domain/Infrastructure);
frontend Angular 21 (`frontend/`). Deploy Railway via Docker; env vars no painel (não há
`appsettings.Production.json` — `appsettings.json` só tem placeholders).

## ⚠️ ANTES de codar qualquer entidade / tela / fluxo que grave dados

**Leia `ContextDocuments/INFRAESTRUTURA/GUIA-sync-nova-entidade.md`.** O sistema de sincronismo é
fail-closed: uma entidade nova mal classificada **derruba o boot inteiro** (não só o sync). O guia é o
checklist pra acertar de primeira. A fonte da verdade das decisões é
`ContextDocuments/INFRAESTRUTURA/plano-correcao-replicacao-2026-07-17.md`.

## Invariantes não-negociáveis (resumo — detalhe no guia)

- **Registry único + fail-closed:** toda `BaseEntity` é classificada (Global | PorFilial | Infra) em
  `SyncRegistry.cs`, validada no boot (`ValidarModelo`). Coleção-filha POCO de agregado Global tem que
  ser promovida a `BaseEntity` (união) OU declarada na whitelist (substituição). Sem isso, o boot cai
  com a lista nominal.
- **Outbox atômico:** escrita replicável enfileira na `SyncFila` **no mesmo commit** do dado. Fluxo com
  vários `SaveChanges` PRECISA de transação única, senão o crash no meio replica estado parcial.
- **Ledger append-only:** `MovimentosEstoque`/`MovimentosLote` só aceitam `I`; correção = movimento novo.
- **Faixa de Id por nó** (`No:Codigo × 1e9`): PK preservada cross-node, nunca reciclar código/PK/sequence.
- **Conflito = LWW por linha** (`AtualizadoEm`; empate → escritor maior). NÃO LWW em ledger/venda/fiscal.
- **`No:Modo` é obrigatório** (Hub/Edge/StandaloneCloud) — ausente/contraditório = boot falha alto
  (`NoDeployment.Resolver`). Migrations rodam no STARTUP (fail-fast) — boot lento = migration rodando.

## Processo

- **"Compilou, sobe" não vale — teste vermelho→verde ou não aconteceu.** Testes de integração no
  harness `PostgresFixture` (Postgres real). Tradução EF duvidosa → provar offline com `ToQueryString()`.
- Mudança crítica (replicação, dinheiro, corrida de estado) → revisão adversarial antes de concluir.
- Comentários de código em PT sem acentos (estilo do repo). Docs/commits podem usar acentos.
- Commit local por preocupação; **deploy/push só quando o dono pedir.**

## Índice de documentação

`ContextDocuments/INFRAESTRUTURA/`:
- `GUIA-sync-nova-entidade.md` — **checklist pra adicionar entidade/tela nova** (comece aqui).
- `plano-correcao-replicacao-2026-07-17.md` — plano de 6 fases, decisões travadas, pendências (§5c). **Fonte da verdade.**
- `sync.md` — mecanismo (tabelas, push/pull, faixa de Id, autenticação).
- `spec-conflito-poco-bc.md` — design união vs substituição.
- `runbook-implantacao.md` — montar nuvem + PCs locais do zero.
- `runbook-bootstrap-no.md` — anexar nó novo a um hub existente.
- `synAteAqui.md` / `orientacao-replicacao-codex-2026-07-17.md` — retrospectiva + auditoria externa.
- `autenticacao.md`, `atualizacao.md`, `deploy.md` — outros subsistemas.
- `PROJECT_CONTEXT.md` (raiz do ContextDocuments) — visão geral do projeto (⚠️ parcialmente desatualizado).
