# Spec — Configurador de visibilidade por ramo (tiles / telas / seções / campos)

**Status:** v0.2 — 2026-07-05 — **PROPOSTA (não implementado)**. Decisão de arquitetura fechada; implementação faseada pendente.
**Escopo:** definir, de forma **global** (uma config vale pra todos os clientes), o que aparece em cada **ramo** (`RamoFilial`): tiles do dashboard/busca, telas (rotas), e **seções/campos** dentro das telas — com um **configurador SH-only** no próprio ErpPharma.
**Relacionado:** [[multiramo-grade]] (Passo 1 criou `RamoFilial` + `RamoFeatures` + `temFeature`/`featureGuard`).

> **Decisão (2026-07-05):** foi avaliada uma **segunda camada por cliente** (override local por instalação, ex.: uma farmácia que não usa Farmácia Popular esconde `PREÇO FP BOLSA FAMÍLIA`). **Adiada** — neste primeiro momento fica **só por ramo** (global). O modelo de duas camadas (`visível = ramo permite E cliente não escondeu`) fica registrado como evolução futura, mas **fora do escopo agora**.

---

## Contexto / problema

O Passo 1 do multi-ramo já gateia **tiles** e **telas** por feature-key (`RamoFeatures.Para(ramo)` → `features[]` → `AuthService.temFeature(key)` no front e `featureGuard` nas rotas). Funciona, mas:

1. **Campos/seções não são gateados.** Ex.: no cadastro de produto, uma filial de **vestuário** ainda vê seções de farmácia (Substâncias, SNGPC/classe terapêutica, Farmácia Popular, parte do Fiscal). Precisam sumir por ramo.
2. **O mapa ramo→features é código** (`RamoFeatures.Para`). Mudar exige recompilar. A ideia é ter um **configurador** onde a SH marca visualmente o que aparece por ramo.
3. A config precisa ser **global** — a mesma pra todos os clientes da software house — **não** configurável por cliente.

## Objetivo

- Estender o gating de visibilidade para **seções e campos** (além de tiles/telas), reusando a máquina de feature-keys já existente.
- Ter um **configurador visual** (SH-only) pra definir "o que aparece em cada ramo", sem recompilar.
- Manter a definição **global e versionada** (fonte única de verdade, viaja no build), não uma tabela divergente por instalação.

---

## Decisões de arquitetura (travadas)

### D1 — Onde: **ErpPharma**, dev tile **SH-only** (não no ZulexAdmin)
- As telas/seções/campos/tiles moram no ErpPharma — o configurador precisa enxergar esse catálogo.
- O login **SH/SISTEMA** já existe no ErpPharma; já há padrão de "seção dev só pra SH".
- Evita acoplar dois produtos e criar dependência de rede em runtime (ErpPharma buscando config no Admin).

### D2 — Global = **manifesto versionado**, não tabela por cliente
- Cada instalação tem seu próprio banco. Se o configurador gravasse numa tabela editável, cada cliente **divergiria** → deixaria de ser global.
- A definição vive num **manifesto versionado** que viaja no build (`ramo-visibilidade.json` no repo **ou** tabela **semeada e tratada como read-only de produto**).
- O **dev tile (SH)** é o **editor** desse manifesto: edita → gera/exporta o JSON → **commita** → deploy leva pra todos. O banco, no máximo, é **cache/preview local** pra testar antes de commitar.

### D3 — Reusar **feature-keys** (não inventar sistema novo)
- Já existe `temFeature(key)` gateando tiles/telas. Campos/seções usam o **mesmo** mecanismo (tag no elemento + key no set do ramo).
- O manifesto passa a ser exatamente **"ramo → feature-keys"** (hoje isso é o `RamoFeatures.Para` em código; vira dado versionado gerado pelo configurador).

### D4 — Granularidade: **seções primeiro**, campo a campo só onde necessário
- O form de produto já é dividido em accordions/seções (Identificação, Classificação, Estoque, Preços, Fiscal, Substâncias, SNGPC…). Gatear **seção inteira** por ramo cobre ~90% do caso e é muito mais barato.
- Campo-a-campo só onde a seção é mista (ex.: um campo de farmácia dentro de uma seção comum).

---

## Modelo de dados / manifesto

### Feature-keys (catálogo)
Keys já existentes: `sngpc`, `farmacia-popular`, `receita`, `substancias`, `grade`, `pesavel`.
O manifesto define, por ramo, o **conjunto de keys ativas**. Elementos de UI (tiles, rotas, seções, campos) declaram a key que os gateia.

### Manifesto `ramo-visibilidade` (formato proposto)
```jsonc
{
  "versao": 1,
  "ramos": {
    "Farmacia":  { "features": ["sngpc","farmacia-popular","receita","substancias"] },
    "Vestuario": { "features": ["grade"] },
    "Hortifruti":{ "features": ["pesavel"] },
    "Mercearia": { "features": ["pesavel"] },
    "Generico":  { "features": [] }
  },
  // Catálogo de elementos gateáveis (pra o configurador saber o que existe e
  // pra validar). Cada elemento aponta a key que o mostra.
  "elementos": {
    "tiles":   [ { "id": "sngpc",        "label": "SNGPC",              "feature": "sngpc" } ],
    "telas":   [ { "rota": "prescritores","label": "Prescritores",      "feature": "receita" } ],
    "secoes":  [ { "tela": "produtos",   "id": "substancias",           "label": "Substâncias",           "feature": "substancias" },
                 { "tela": "produtos",   "id": "sngpc",                 "label": "SNGPC / Classe terap.", "feature": "sngpc" },
                 { "tela": "produtos",   "id": "farmacia-popular",      "label": "Farmácia Popular",      "feature": "farmacia-popular" } ],
    "campos":  [ /* {tela, secao, id, label, feature} — só onde a seção é mista */ ]
  }
}
```
Observações:
- **`features` por ramo** é o que o login já devolve (`LoginResponseDto.Features`). Passa a ser **derivado do manifesto** em vez do `switch` hardcoded.
- **`elementos`** é o catálogo que o configurador manipula (checkboxes: "esta seção aparece nos ramos X, Y") e que valida keys órfãs.
- Onde mora o manifesto no runtime: opção A) arquivo `json` embarcado + lido no startup; opção B) tabela `RamoVisibilidade` semeada do json (read-only na prática). Decidir na implementação (ver §Questões abertas).

---

## Fluxo de aplicação (como o gating chega na tela)

1. **Backend login/`/me`** devolve `Ramo` + `Features` (já faz; passa a derivar do manifesto).
2. **Front `AuthService`** guarda `features`; helpers:
   - `temFeature(key)` — já existe (tiles/telas).
   - Extensão: usar o mesmo `temFeature` para **seções** (`@if (auth.temFeature('substancias'))` no accordion) e **campos**.
3. **`featureGuard`** nas rotas — já existe.
4. **Seções do produto** — passam a ser condicionadas por `temFeature(...)` (hoje aparecem sempre).

> Nota: manter o comportamento **lenient** atual (`temFeature` retorna `true` se a sessão não tem `features`) pra não quebrar sessões antigas.

---

## Configurador (dev tile SH-only)

- **Acesso:** tile na seção Dev do dashboard, visível **só com login SISTEMA/SH** (mesmo padrão dos outros itens SH).
- **Tela:** matriz **Elemento × Ramo** (linhas = tiles/telas/seções/campos do catálogo; colunas = ramos; célula = checkbox "aparece").
  - Internamente edita o mapa **ramo → features** + as tags de feature nos elementos.
- **Persistência/saída:** grava no cache local (preview) e permite **exportar o manifesto** (JSON) pra commitar no repo. É o passo que torna a mudança **global** (via deploy).
- **Preview:** poder "ver como ramo X" sem trocar de login, pra validar antes de exportar.

---

## Faseamento

1. **Fase 1 — Gating de seções/campos (código, ganho rápido).** Condicionar por `temFeature` os elementos do cadastro de produto não-inerentes ao ramo. Alvos concretos já levantados:
   - Seção **Substâncias** → feature `substancias`.
   - Seção/campos **SNGPC** (classe terapêutica) → `sngpc`.
   - **Farmácia Popular**: campos `PREÇO FP`, **`PREÇO FP BOLSA FAMÍLIA`**, `PARTICIPA FARMÁCIA POPULAR` → `farmacia-popular`.
   - **Prescritores/receita** → `receita`.
   Usa as keys que já existem. Entrega a dor imediata (ramos não-farmácia param de ver campo de farmácia) e **mapeia o catálogo** de seções/campos/keys.
2. **Fase 2 — Manifesto versionado.** Migrar `RamoFeatures.Para` (switch) → leitura do manifesto (`ramo-visibilidade.json`/tabela semeada). Login passa a derivar dele. Sem mudança visível pro usuário.
3. **Fase 3 — Configurador SH.** Dev tile com a matriz Elemento × Ramo, preview e exportação do manifesto.
4. **Fase 4 — Campos (opcional).** Gating campo-a-campo onde a seção é mista.

---

## Impactos / riscos

- **Sessões antigas:** `temFeature` lenient evita telas quebrarem; manter.
- **Divergência entre clientes:** mitigada por D2 (manifesto versionado é a fonte; banco é cache). Override **por cliente** foi avaliado e **adiado** (2026-07-05) — hoje é **só por ramo** (global). Quando/se voltar, entra como camada 2 (`ramo AND NOT override_local`).
- **Catálogo desatualizado:** se um dev adiciona uma seção nova e não a registra no catálogo, ela não aparece no configurador. Mitigar com convenção/checklist ou um lint que cruza `temFeature('x')` usados no código × keys do manifesto.
- **Não é multi-tenant de UI por cliente** — de propósito. É por **ramo**, global.

## Questões abertas
- Manifesto **arquivo embarcado** vs **tabela semeada**? (arquivo = mais simples e claramente versionado; tabela = editável em runtime pra preview, mas exige disciplina de export).
- O configurador exporta JSON pra commit **manual**, ou grava direto num arquivo do repo em ambiente dev? (provável: exporta/baixa, dev commita).
- Precisamos de **feature-keys novas** além das atuais pra cobrir as seções do produto? (levantar na Fase 1).
- Ramos futuros (papelaria, pet, etc.) — o manifesto já suporta; só adicionar ao enum `RamoFilial` + entrada no manifesto.

---

## Ordem de execução
Fase 1 (seções do produto) → Fase 2 (manifesto) → Fase 3 (configurador SH) → Fase 4 (campos). Cada fase é entregável e reversível; a Fase 1 já resolve o problema atual do vestuário.

---

## Estado de implementação (as-built)

### Fase 1 — Gating no cadastro de produto ✅ parcial (2026-07-05)
- `ProdutosComponent.temFeature(key)` exposto pro template.
- Gated por `temFeature` no form de produto:
  - Campos **Farmácia Popular** (PREÇO FP, PREÇO FP BOLSA FAMÍLIA, PARTICIPA FARMÁCIA POPULAR) → `farmacia-popular`.
  - Campo **CLASSE TERAPÊUTICA (SNGPC)** → `sngpc`.
  - Accordion **Substâncias** → `substancias`.
- Efeito: ramos não-farmácia (Hortifruti/Mercado/Vestuário) param de ver esses campos. `temFeature` é lenient (sessão sem features vê tudo).
- **Pendente da Fase 1:** demais cadastros/telas; avaliar key nova pra "Registros MS" (medicamento) — hoje sem key própria, continua visível. Catálogo formal (id/label/tela) ainda não materializado (virá com o configurador).

### Fases 2/3/4 — pendentes.
