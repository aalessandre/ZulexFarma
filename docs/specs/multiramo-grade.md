# Spec — Multi-ramo (por filial) + Grade de variações configurável

**Status:** v0.2 — 2026-07-02 — **Passo 1 (Ramo) e Passo 2a+2b IMPLEMENTADOS e na main**; falta 2c (PDV). Balança e Offline ficam pra depois (specs próprias). Ver §Estado de implementação no fim.
**Escopo:** Passo 1 (Ramo por filial) + Passo 2 (Grade configurável).

## Contexto / objetivo
O ErpPharma deixa de ser só farmácia e passa a atender **vários ramos** (vestuário, hortifruti, mercearia, …). A diferença entre ramos é **quais funcionalidades aparecem** e **como o produto é modelado**. Duas fundações:
1. **Ramo por filial** — cada filial tem um ramo que liga/desliga bundles de features. (Cliente pode ter farmácia numa filial e loja de roupas em outra.)
2. **Grade de variações configurável** — produto com eixos dinâmicos (Tamanho×Cor, Voltagem, Sabor…), cada combinação = 1 SKU.

**Decisões travadas:**
- Ramo é **por filial** (não global).
- Grade é **configurável** (eixos dinâmicos), não fixo em Tamanho×Cor.
- Atributos de variação são **globais e reusáveis** (Tamanho/Cor cadastrados 1x).
- Estoque/preço da variação reusa **`ProdutoDados` + `ProdutoVariacaoId` nullable** (sem tabela nova de estoque).
- **1 código de barras por variação**.

---

## Passo 1 — Ramo por filial

### 1.1 Modelo
- `Filial.Ramo` — novo enum `RamoFilial`: `Generico`, `Farmacia`, `Vestuario`, `Hortifruti`, `Mercearia` (extensível). Default dos existentes = `Farmacia` (seed/migration).
- **Mapa Ramo → Features** (código, `RamoFeatureMap`): cada ramo mapeia pra um conjunto de feature-keys:
  - `Farmacia`: `sngpc`, `farmacia-popular`, `receita`, `substancias`, `pbm`
  - `Vestuario`: `grade`
  - `Hortifruti` / `Mercearia`: `pesavel`
  - (features comuns — caixa, compras, financeiro, fiscal, clientes — não dependem de ramo)
- Override individual de feature por filial: **fora do MVP** (só o preset do ramo por enquanto).

### 1.2 Backend
- Migration: adiciona `Ramo` em `Filiais` (default `Farmacia` pros existentes).
- No **login / `/me`**: incluir `ramo` da filial + `features: string[]` (derivadas do mapa) na resposta. Opcional: claim JWT `features` pra gatear endpoints sensíveis (ex.: controllers SNGPC só quando feature `sngpc` ativa) — mesma mecânica do `[Permissao]`.
- `FilialService`/cadastro de filial: campo Ramo editável (tela Filiais, que já exige senha do dia).

### 1.3 Frontend
- `AuthService`: guardar `features`; helper `temFeature(key)` (espelha `temPermissao`).
- **Gate de tiles**: filtrar os blocos/tiles do dashboard (`dashboard.component.ts`) e do índice de busca (`erp-shell.component.ts`) por feature — ex.: tile SNGPC só aparece com feature `sngpc`. (Conecta com a limpeza de tiles já feita.)
- **Gate de telas**: guard nas rotas de features de ramo (SNGPC, Farmácia Popular, Grade…) → redireciona se a filial não tem a feature.
- **Gate de campos**: no cadastro de produto, seção "Grade" só aparece com feature `grade`; campos SNGPC (classe terapêutica) só com feature `sngpc`; etc.

### 1.4 Checklist Passo 1
- [ ] Enum `RamoFilial` + `RamoFeatureMap` (Domain).
- [ ] Migration `Filiais.Ramo` (default Farmacia).
- [ ] Login/`/me` retorna `ramo` + `features`.
- [ ] `FilialService`/tela Filiais: editar Ramo.
- [ ] Frontend: `AuthService.temFeature`, gate de tiles, guard de telas, gate de campos.

---

## Passo 2 — Grade de variações configurável

### 2.1 Modelo (EAV enxuto)
- `AtributoVariacao : BaseEntity` — eixo global reusável. Campos: `Nome` (Tamanho, Cor, Voltagem), `Ordem`.
- `ValorAtributo : BaseEntity` — valores de um atributo. `AtributoVariacaoId`, `Valor` (P, M, Preto, 110V), `Ordem`.
- `Produto.ControlaGrade` (bool) — quando `true`, o produto é o **"modelo"** e é vendido pelas variações (não vende o modelo "solto").
- `ProdutoAtributo : BaseEntity` — quais eixos o produto usa. `ProdutoId`, `AtributoVariacaoId`, `Ordem`. (Camiseta → Tamanho, Cor.)
- `ProdutoVariacao : BaseEntity` — o **SKU real**. `ProdutoId`, `CodigoBarras` (próprio, único), `Ativo`. (Opcional: `PrecoProprio` decimal? nullable — herda do modelo se null.)
- `ProdutoVariacaoValor : BaseEntity` — combinação. `ProdutoVariacaoId`, `AtributoVariacaoId`, `ValorAtributoId`. N linhas por variação (Camiseta-M-Preto = Tamanho:M + Cor:Preto).

Todas herdam `BaseEntity` → **entram no sync** automaticamente (FilialOrigemId, SyncGuid, ID-range). Atributos/valores são cadastro central compartilhado.

### 2.2 Estoque/preço por SKU
- `ProdutoDados` ganha `ProdutoVariacaoId` **nullable**:
  - Produto **sem grade** → 1 `ProdutoDados` por (Produto, Filial), `ProdutoVariacaoId = null` (comportamento atual, intacto).
  - Produto **com grade** → 1 `ProdutoDados` por (ProdutoVariacao, Filial). O estoque/preço/promo/custo vivem no SKU, reusando toda a máquina existente.
- Índice único ajustado: `(ProdutoId, FilialId, ProdutoVariacaoId)`.
- Preço: variação usa o `ValorVenda` do seu `ProdutoDados`; por padrão, ao gerar a grade, herda o preço do modelo (editável célula a célula).

### 2.3 Cadastros / telas
- **Atributos de variação** (nova tela, cadastro simples): CRUD de `AtributoVariacao` + `ValorAtributo` (ex.: Tamanho → P/M/G/GG; Cor → lista com nome + hex opcional).
- **Produto → aba/serção Grade** (só com feature `grade`):
  - Liga `ControlaGrade`.
  - Escolhe os eixos (`ProdutoAtributo`) e os valores participantes por eixo.
  - Gera a **matriz** de combinações → cada célula vira uma `ProdutoVariacao` (com barras + estoque + preço). UI: matriz pra 2 eixos; lista/expansível pra 3+.
- Barras: por variação; se o fornecedor já manda EAN por SKU, importa; senão, gera.

### 2.4 PDV / venda / fiscal
- No caixa, a busca por **código de barras da variação** resolve o SKU direto (produto + atributos). Estoque baixa no SKU (`ProdutoDados` da variação).
- Item de venda referencia a variação (novo `VendaItem.ProdutoVariacaoId` nullable). Descrição do item = "Camiseta Polo P Preto".
- **Fiscal herda do modelo**: NCM/CEST/CFOP/impostos vêm do `ProdutoFiscal` do modelo (variação não muda tributação). `VendaItemFiscal` continua igual.

### 2.5 Checklist Passo 2
- [ ] Entidades `AtributoVariacao`, `ValorAtributo`, `ProdutoAtributo`, `ProdutoVariacao`, `ProdutoVariacaoValor` + `Produto.ControlaGrade`.
- [ ] `ProdutoDados.ProdutoVariacaoId` nullable + índice único.
- [ ] `VendaItem.ProdutoVariacaoId` nullable.
- [ ] Migration.
- [ ] Tela cadastro de Atributos/Valores.
- [ ] Produto: seção Grade (gerar matriz → variações + estoque/preço).
- [ ] PDV: busca por barras da variação, baixa de estoque no SKU.
- [ ] Venda/relatórios: exibir descrição com atributos.

---

## Impactos / riscos
- **Explosão de SKUs**: grade P/M/G/GG × 8 cores = 32 variações por modelo → UX da matriz precisa ser boa; sync cresce (aceitável).
- **Telas de produto existentes**: a seção Grade é aditiva (feature-gated); produto sem grade não muda.
- **Migração de dados**: nenhum produto atual tem grade → zero backfill; `ProdutoVariacaoId` nasce null em tudo.
- **Estoque por filial**: continua por filial (não colide no sync entre lojas).

## Ordem de execução
1. **Passo 1 (Ramo)** — pequeno, destrava a visibilidade por ramo. ✅ FEITO.
2. **Passo 2 (Grade)** — 2a (modelo + Atributos) ✅ · 2b (grade no produto) ✅ · 2c (PDV) ⏳.
3. (depois) Balança / Pesável, e Offline/servidor local — specs próprias.

---

## Estado de implementação (as-built)

### Passo 1 — Ramo por filial ✅ (2026-07-01)
- `Filial.Ramo` (enum `RamoFilial`: Generico/Farmacia/Vestuario/Hortifruti/Mercearia) + `RamoFeatures.Para(ramo)` (mapa ramo→features). Migration `AddFilialRamo` (default Farmacia).
- Login (`LoginResponseDto`) devolve `Ramo` + `Features`; SISTEMA vê todas, admin de filial vê só as do ramo.
- Frontend: `AuthService.temFeature(key)`; tiles do dashboard e da busca (`erp-shell`) filtrados por `feature`; `featureGuard` nas rotas `sngpc`/`substancias`/`prescritores`; campo Ramo na tela Filiais.
- Feature keys: `sngpc`, `farmacia-popular`, `receita`, `substancias`, `grade`, `pesavel`.

### Passo 2a — Modelo + Atributos ✅ (2026-07-01)
- Entidades: `AtributoVariacao`, `ValorAtributo`, `ProdutoAtributo`, `ProdutoVariacao`, `ProdutoVariacaoValor` + `Produto.ControlaGrade` + `ProdutoDados.ProdutoVariacaoId` (nullable) + `VendaItem.ProdutoVariacaoId` (nullable). Migration `AddGradeVariacoes` (5 tabelas + colunas).
- API `IAtributoVariacaoService` + `AtributosVariacaoController` (`/api/atributos-variacao`, CRUD com sincronia dos valores).
- **Seed** de Tamanho (PP..46) e Cor (10 cores c/ hex) no `DatabaseSeeder` (guard `AnyAsync`; ⚠️ multi-instância: futuro ideal é semear só na central).
- Frontend: tela `atributos-variacao` (grid + modal com lista inline de valores), rota + tiles gated `grade`.

### Passo 2b — Grade no produto ✅ (2026-07-02)
- `IProdutoGradeService`/`ProdutoGradeController`: `GET/PUT /api/produtos/{id}/grade`. Salvar: liga `ControlaGrade`, sincroniza eixos (`ProdutoAtributo`), gera/sincroniza `ProdutoVariacao` (+`ProdutoVariacaoValor`, combinação imutável por SKU) e grava estoque/preço por SKU em `ProdutoDados` da **filial atual** (`FilialContexto.FilialIdAtual`; exige filial > 0). Variação usada em venda → desativa (não apaga).
- Frontend: editor `/erp/produto-grade/:id` (gated `grade`) — marca eixos + valores → **Gerar matriz** (produto cartesiano, mescla com existentes por chave de combinação) → edita barras/estoque/preço por célula. Botão **"Grade"** na toolbar do produto (`podeGrade()` = `temFeature('grade')`, só editando).

### Correções pós-teste (2026-07-03)
- **Índice único de `ProdutoDados` corrigido**: era `(ProdutoId, FilialId)` — bloqueava >1 SKU por produto/filial (salvar grade dava **500**). Agora `(ProdutoId, FilialId, ProdutoVariacaoId)` com **NULLS NOT DISTINCT** (PG 15+), garantindo 1 linha "sem variação" por produto/filial e N linhas por SKU. Migration `AjustaIndiceProdutoDadosVariacao`.
- **Editor de grade — UX**: chips de valor modernizados (checkbox nativo escondido + indicador de check estilizado); botão **"Gerar barras"** por célula + **"Gerar barras das vazias"** (EAN-13 interno, prefixo `2` + 7 díg. do produto + 4 díg. da linha + verificador).
- **Grade virou MODAL sobre o produto** (antes era tela/aba separada — perdia o contexto e obrigava a repesquisar). `ProdutoGradeComponent` agora é modal com `@Input produtoId/nome` + `@Output fechar/salvou`; aberto de dentro do cadastro de produto (`abrirGrade()` → `modalGrade` signal). Rota `produto-grade/:id` removida.
- **Estoque do produto = soma das variações**: `ProdutoDetalheDto.ControlaGrade` + `MapDetalhe` filtra as linhas-base de `ProdutoDados` (`ProdutoVariacaoId == null`, 1 por filial) e, para produto com grade, sobrescreve `EstoqueAtual` com a **soma dos SKUs por filial**. No form, o campo Estoque Atual fica **read-only** (tag "grade") — edita-se pela Grade. Ao salvar a grade, o modal emite o total e o produto atualiza o campo na hora (sem repesquisar). Corrige de quebra o bug latente de `Dados` trazer linhas de variação misturadas.

### Passo 2c — PDV ⏳ (pendente)
Falta: no caixa, resolver o `CodigoBarras` da variação → SKU, referenciar `VendaItem.ProdutoVariacaoId`, e baixar estoque no `ProdutoDados` da variação. `VendaItem.Quantidade` é **int** (revisar pra decimal quando for a fase Pesável/Balança).
