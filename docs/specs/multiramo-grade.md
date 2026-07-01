# Spec — Multi-ramo (por filial) + Grade de variações configurável

**Status:** v0.1 — 2026-07-01 — desenho aprovado, pré-codificação
**Escopo:** Passo 1 (Ramo por filial) + Passo 2 (Grade configurável). Balança e Offline ficam pra depois (specs próprias).

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
1. **Passo 1 (Ramo)** — pequeno, destrava a visibilidade por ramo. Codar primeiro.
2. **Passo 2 (Grade)** — modelo + telas + PDV. Depende só do gate `grade` do Passo 1.
3. (depois) Balança / Pesável, e Offline/servidor local — specs próprias.
