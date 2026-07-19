# Índice de Contexto — ZulexPharma / ErpPharma

> Guia operacional de IA: **`CLAUDE.md` na raiz do repo**. Antes de codar entidade/tela/fluxo que grave
> dados, leia o guia de sincronismo (abaixo) — o sistema é fail-closed e entidade mal classificada
> derruba o boot.

## Sincronismo / replicação (`INFRAESTRUTURA/`)
- [GUIA-sync-nova-entidade.md](INFRAESTRUTURA/GUIA-sync-nova-entidade.md) — **checklist pra adicionar entidade/tela nova. Comece aqui.**
- [plano-correcao-replicacao-2026-07-17.md](INFRAESTRUTURA/plano-correcao-replicacao-2026-07-17.md) — plano de 6 fases, decisões travadas, pendências (§5c). **Fonte da verdade.**
- [sync.md](INFRAESTRUTURA/sync.md) — mecanismo: tabelas, push/pull, faixa de Id, autenticação.
- [spec-conflito-poco-bc.md](INFRAESTRUTURA/spec-conflito-poco-bc.md) — design união vs substituição (b+c).
- [runbook-implantacao.md](INFRAESTRUTURA/runbook-implantacao.md) — montar nuvem + PCs locais do zero (topologia, env vars, bootstrap).
- [runbook-bootstrap-no.md](INFRAESTRUTURA/runbook-bootstrap-no.md) — anexar nó novo a um hub existente.
- [synAteAqui.md](INFRAESTRUTURA/synAteAqui.md) — retrospectiva/erros do subsistema.
- [orientacao-replicacao-codex-2026-07-17.md](INFRAESTRUTURA/orientacao-replicacao-codex-2026-07-17.md) — auditoria externa (Codex).

## Outros subsistemas (`INFRAESTRUTURA/`)
- [autenticacao.md](INFRAESTRUTURA/autenticacao.md), [atualizacao.md](INFRAESTRUTURA/atualizacao.md), [deploy.md](INFRAESTRUTURA/deploy.md)

## Visão geral do projeto
- [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md) — stack/módulos/status. ⚠️ Parcialmente desatualizado (não reflete o sincronismo nem os módulos atuais).
