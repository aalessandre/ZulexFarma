# Spec — Cadastro de Produto (multi-filial)

**Status:** v0.1 — 2026-07-04 — regra de propagação **em ajuste** (create já propaga; edit sendo alinhado).
**Escopo:** como o cadastro de produto se comporta num ambiente **multi-filial**: o que é global, o que é por filial, e a regra de **propagação entre filiais**.

---

## Princípio

Um **produto é o mesmo em todas as filiais** — muda só o **estoque**. Portanto:

> **Tudo é empurrado para todas as filiais ativas, EXCETO o estoque.**

Ao cadastrar **ou editar** um produto, os dados comerciais (preço, markup, custo, promoção, descontos, comissão), os **dados fiscais** (NCM/CST/CFOP/alíquotas) e os vínculos de **fornecedor** passam a valer em **todas as filiais**. O **estoque** (atual/mínimo/máximo/demanda/curva/depósito) é **por filial** e nunca é copiado — cada loja controla o seu.

---

## Modelo: o que é global vs por filial

- **Global (uma linha `Produto`):** Nome, Código de barras, Qtde embalagem, Fração, Lista, Classe terapêutica, Fabricante, Grupos, NCM (do produto), flags (ControlaGrade, Pesável/Unidade/CodigoBalança), Preço FP, Barras adicionais, Registros MS, Substâncias.
- **Por filial (`ProdutoDados`):**
  - **Propagado (exceto estoque):** preços (ValorVenda, Pmc, PrecoFabrica, Markup, ProjeçãoLucro, CustoMédio, ÚltimaCompra*), promoção, descontos, comissão/incentivo, local/seção/família, nome etiqueta/mensagem, e as flags de bloqueio/config.
  - **NÃO propagado (por filial):** `EstoqueAtual`, `EstoqueMinimo`, `EstoqueMaximo`, `Demanda`, `CurvaAbc`, `EstoqueDeposito`.
- **Por filial (`ProdutoFiscal`):** NCM/CST/CFOP/CEST/origem/alíquotas — **propagado** (fiscal é igual entre filiais do mesmo emitente na prática do ERP).
- **Por filial (`ProdutoFornecedor`):** vínculos com fornecedor — **propagado** (cria nas filiais que não têm).

> Estoque decimal: `EstoqueAtual` é `numeric(10,3)` (suporta peso/kg — ver [[hortifruti-pesavel-balanca]]).

---

## Regra de propagação

### Filial de origem
A propagação parte da **filial que está sendo editada** (a `FilialOrigem` — a filial selecionada no formulário). Os valores dela são copiados para **todas as outras filiais ativas**, exceto os campos de estoque.

### Ao CRIAR (`CriarAsync`)
- Garante uma linha `ProdutoDados` e `ProdutoFiscal` para **cada filial ativa**.
- Copia os dados-exceto-estoque + fiscal + fornecedores da filial de origem para as demais. **(Já implementado — `CopiarDadosParaOutrasFiliais`.)**

### Ao EDITAR (`AtualizarAsync`)
- Mesma propagação: os dados-exceto-estoque + fiscal da filial editada são empurrados para as demais. **(Ajuste desta spec — antes só o preço propagava, e ainda gated pela config `produto.preco.regra`.)**
- **Estoque nunca é sobrescrito** nas outras filiais (mantém o saldo de cada uma).

### Config `produto.preco.regra` (legado)
Existia pra escolher se o **preço** propagava no edit (`perguntar` | `todas` | `atual`, default `atual`). Com a regra "produto uniforme", o preço passa a **sempre propagar** junto com o resto (exceto estoque). A config fica **obsoleta pro caso geral** (mantida só se um dia quisermos preço regional por filial — hoje **não** é o comportamento).

---

## Fluxo (frontend)

- O formulário de produto edita **uma filial por vez** (seletor de FILIAL no topo). Ao salvar, manda `FilialOrigem = filialSelecionada`.
- O backend propaga essa filial pras demais (exceto estoque). Não há mais o prompt "aplicar preço em quais filiais?" — propaga sempre.
- **PDV/caixa** usa o preço/estoque da **filial do operador** (do login). Por isso, produto sem preço numa filial aparece R$ 0,00 lá — mas com a propagação, ao cadastrar/editar em qualquer filial, todas passam a ter o preço. (Estoque continua por filial.)

---

## Impactos / riscos

- **Sobrescrita de preço regional:** como o edit passa a propagar o preço pra todas, qualquer diferença de preço por filial é sobrescrita ao editar. É o comportamento desejado ("produto uniforme"). Se no futuro precisar de preço por filial, reintroduzir via `produto.preco.regra`/override.
- **Estoque preservado:** garantir que a cópia nunca toque nos campos de estoque (o `CopiarDadosSemEstoque` já respeita).
- **Filiais novas:** um produto criado antes de uma filial nova existir não tem linha lá. Ao editar depois, a propagação cria/atualiza. (Ideal futuro: ao criar filial, semear ProdutoDados/Fiscal dos produtos existentes.)
- **Performance:** propagação é O(nº de filiais) por save — irrelevante no volume atual.

## Questões abertas
- Preço **regional por filial** algum dia? (hoje: não; uniforme.)
- Ao **criar uma filial nova**, semear os produtos existentes nela? (fora do escopo agora.)
- Propagar também **barras adicionais**/registros? (são globais no produto, então já valem pra todas — sem ação.)

---

## Estado de implementação
- **CREATE**: propaga dados-exceto-estoque + fiscal + fornecedores. ✅ (pré-existente)
- **EDIT**: propagação de tudo-exceto-estoque — **em implementação nesta spec** (antes: só preço, gated por `produto.preco.regra`).
