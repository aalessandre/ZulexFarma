# GUIA — adicionar entidade/tela/fluxo novo ao esquema de sincronismo

> **Leia isto ANTES de codar** qualquer entidade, tela ou fluxo que grave dados. O ErpPharma tem
> replicação multi-master própria (estrela edge→hub, LWW por linha, outbox). Se você criar algo sem
> classificar, **o boot cai** (fail-closed) — este guia é o caminho pra acertar de primeira.
>
> Fonte da verdade do MECANISMO: `sync.md`. Fonte da verdade das DECISÕES e do "não fazer":
> `plano-correcao-replicacao-2026-07-17.md`. Retrospectiva/erros: `synAteAqui.md`. Este guia é o
> checklist prático; em conflito, o plano vence.

## Modelo mental em 6 linhas

- Cada nó tem seu banco. **Hub** (nuvem, `No:Codigo=0`) consolida; **Edge** (PC de loja, `No:Codigo≥1`)
  replica; **StandaloneCloud** (loja só nuvem, `≥1`) NÃO replica. Modo vem de `No:Modo` (obrigatório).
- Toda escrita numa entidade replicável gera uma op na **SyncFila** (outbox), **no mesmo commit** do dado
  (`AppDbContext.SaveChangesAsync`).
- O Edge dá PUSH das suas ops e PULL das dos outros; o Hub é reativo e numera as ops (`SeqEntrega`).
- Conflito = **LWW por linha** (`AtualizadoEm`; empate → escritor maior).
- **Faixa de Id por nó** (`offset = No:Codigo × 1e9`, `DatabaseSeeder.cs:14`) — PK nunca colide e é
  preservada ao replicar. Não invente Id; o seeder reposiciona as sequences no boot.
- Um **registry único** (`SyncRegistry.cs`) classifica TUDO e é validado no boot (fail-closed).

---

## O CHECKLIST

### 1. Toda `BaseEntity` nova → classifique o ESCOPO em `SyncRegistry.cs` (senão o boot cai)

Escolha **exatamente um**:

| Escopo | O que é | Onde declarar | Exige |
|---|---|---|---|
| **Global** | Igual em todos os nós (cadastros de referência: Produto, Pessoa, Convenio…) | `SyncRegistry.Globais` (`:72`) | estar no dicionário do applicator |
| **PorFilial** | Pertence a UMA filial (Venda, Caixa, movimento de estoque…) | `PorFilialDireta` (tem `FilialId`) ou `PorFilialDerivada` (+ entrada em `AppDbContext.DerivacaoFilialDono`) | `FilialId` ou mapa de derivação |
| **Infra** | NUNCA replica (cursor, credencial, controle local) | `InfraTipos` (`:32`) / `TabelasInfra` (`:39`) | — |

- Se replica (Global/PorFilial), a tabela **precisa estar no dicionário** `SyncApplicator.ResolverTipo` —
  senão a op chega no destino como `TipoDesconhecido`. O boot valida isso (`SyncRegistry.cs:161`).
- **O boot te avisa:** classe sem escopo, em >1 escopo, PorFilial sem `FilialId`/derivação → o boot
  lança com a **lista nominal** do que corrigir (prefixo `SyncRegistry INVALIDO (fail-closed...)`,
  `ValidarModelo` `:139`).

### 2. Coleção-filha POCO (não-`BaseEntity`) de agregado GLOBAL → decida UNIÃO vs SUBSTITUIÇÃO

Filho POCO viaja no JSON do pai e é reconciliado por **delete-missing** (a coleção do pai vencedor
substitui a inteira). Você tem que declarar a semântica (senão o boot cai, `ColetarPocoNaoClassificado`
`:193`):

- **Substituição** → declare o tipo em `ColecoesPocoSubstituicaoAceitas` (`:103`). Correto quando o pai
  é **editado em bloco numa tela** (o `AtualizarAsync` faz `RemoveRange`+re-add). O form inteiro mais
  novo vence. É o caso da maioria (Promoção, Hierarquia, Adquirente, Campanha, Convênio).
- **União** → promova o filho a `BaseEntity` (ganha `Id`/`SyncGuid`/etc.; replica sozinho, nada se
  perde). Correto quando **dois nós adicionam vínculos DIFERENTES ao mesmo pai de forma independente**,
  em fluxos separados. É o caso dos 5 filhos de **Cliente** (fase 6).

> Critério de bolso: **editado como bloco = substituição; acrescido em pedaços por nós diferentes =
> união.** Ver `spec-conflito-poco-bc.md` para o racional completo. Se for filho de agregado
> **PorFilial single-writer** (ex.: `VendaItem`), fica POCO e **fora** dessa invariante.

### 3. Fluxo novo com VÁRIOS `SaveChanges` → envolva numa TRANSAÇÃO única

Se a sua função efetiva algo em mais de um `SaveChanges` (ex.: cabeçalho + itens + financeiro + estoque),
**abra `_db.Database.BeginTransactionAsync()` e commite no fim.** Sem isso, um crash no meio deixa
estado parcial **e o outbox replica a parcialidade fielmente** (a SyncFila commita junto de cada save).
Precedente pronto: `SelfCheckoutVendaService.cs:79`. Dívida conhecida do mesmo tipo (ainda aberta):
`VendaService.FinalizarAsync:289` e `CompraService.FinalizarAsync:772` (ver §5c do plano). Sub-serviços
que só fazem `SaveChanges` (sem abrir tx própria) alistam na transação ambiente automaticamente.

### 4. Tabela de LEDGER (fato imutável) → append-only

`MovimentosEstoque`/`MovimentosLote` só aceitam **I** no protocolo (`TabelasLedger` `:125`); `U`/`D`
remoto vai pra quarentena `LedgerImutavel`. Correção de estoque = **movimento de AJUSTE novo**, nunca
editar movimento. Se criar outra tabela de fato imutável, o mesmo vale.

### 5. Campo novo numa entidade que já replica → em geral, nada a fazer

O outbox serializa a entidade inteira, então um campo escalar novo replica sozinho. **Pense em
privacidade:** se o campo carrega conteúdo sensível, avalie se ele deveria viajar. Um campo novo no
backend também precisa do espelho no modelo/DTO do frontend (`core/models`) pra tela consumir.

### 6. O que o boot invariante PEGA vs o que você ainda tem que PENSAR

O fail-closed é forte, mas não pensa por você:

| ✅ O boot PEGA (estrutural) | ⚠️ O boot NÃO pega (você decide) |
|---|---|
| `BaseEntity` sem escopo / em >1 escopo | Escopo **errado** (marcar coisa por-filial como Global compila e sobe) |
| Replicável fora do dicionário do applicator | Atomicidade de transação (fluxo multi-save — §3) |
| PorFilial sem `FilialId`/derivação | União vs substituição **correta** (só que está declarada) |
| POCO sem FK fora de `PocosInfra` | Privacidade de um campo novo |
| Coleção POCO de agregado Global não classificada | Ledger sendo editado localmente por um service |

### 7. Prove antes de dar por pronto

Regra do projeto: **"compilou, sobe" não vale — teste vermelho→verde ou não aconteceu.**
- Teste de integração no harness (`PostgresFixture`, Postgres real): aplica op, afirma convergência.
- Tradução EF duvidosa → provar OFFLINE com `ToQueryString()` (harness sem banco).
- Pós-deploy: `GET /api/sync/checksum?tabela=X` (hub × nó) — count+hash iguais = íntegro.

---

## NÃO fazer (herdado do plano §5 — os que já custaram caro)

- NÃO reciclar `No:Codigo`, PK ou sequence (colisão de faixa).
- NÃO LWW em ledger, venda finalizada ou documento fiscal.
- NÃO delete-missing de coleção sem a chave presente no JSON (contrato da fase 3).
- NÃO purgar lápide/fence por idade.
- NÃO cursor por `Id`/tempo/`MAX(Id)` (o "GAP do cursor" — resolvido com `SeqEntrega`; não regredir).
- NÃO numerar `SeqEntrega` dentro da transação de negócio.
- NÃO confiar em `filialId`/origem vindos do cliente (o escopo vem do cadastro `SyncNoFiliais`).
- NÃO ligar 2º nó real antes do Gate 5 (piloto).

## Onde estão os docs profundos

- **`plano-correcao-replicacao-2026-07-17.md`** — plano de 6 fases, decisões travadas, pendências (§5c), o que não fazer. **Fonte da verdade.**
- **`sync.md`** — descrição do mecanismo (tabelas, fluxo push/pull, faixa de Id, autenticação).
- **`spec-conflito-poco-bc.md`** — design união vs substituição (o b+c).
- **`runbook-implantacao.md`** — montar nuvem + PCs locais do zero (topologia, env vars, bootstrap).
- **`runbook-bootstrap-no.md`** — anexar um nó novo a um hub existente.
- **`synAteAqui.md`** / **`orientacao-replicacao-codex-2026-07-17.md`** — retrospectiva e auditoria externa.
