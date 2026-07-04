# Spec — Hortifruti / Mercado: produto pesável + balança

**Status:** v0.2 — 2026-07-04 — **Fase 1 IMPLEMENTADA e na main**. Fases 2 (export PLU) e 3 (balança no caixa/hardware) pendentes. Ver §Estado de implementação no fim.
**Escopo:** vender produtos **por peso** (kg) no ramo Hortifruti (que passa a atender mercado/açougue/hortifruti em geral, via feature `pesavel`). Dois esquemas de pesagem: (1) balança nos fundos → etiqueta lida no caixa; (2) balança no caixa → peso em tempo real.
**Relacionado:** [[multiramo-grade]] (Passo 1 criou `RamoFilial.Hortifruti` + feature `pesavel`; já anotou que `VendaItem.Quantidade` é **int** e precisa virar decimal aqui).

---

## Contexto / objetivo

O ramo `Hortifruti` já existe (feature `pesavel`), mas não há nada de pesagem. Objetivo: vender produtos **por kg**, cobrindo os dois fluxos usados no varejo:

1. **Balança nos fundos** — o operador pesa, a balança **imprime uma etiqueta** com código de barras (peso/preço embutido). No caixa, esse código é **bipado** e o item entra com o peso/valor já resolvido.
2. **Balança no caixa** (estilo supermercado) — o produto é posto na balança ligada ao PDV; o operador **digita/busca** o produto e o sistema **lê o peso em tempo real** e calcula o valor.

O ramo continua chamado **Hortifruti** internamente, mas atende **mercado/açougue/mercearia** — a feature `pesavel` é o que liga o comportamento (não o nome do ramo). Exibição pode ser relabelada pra "Mercado/Hortifruti".

---

## Decisões (travadas)

- **D1 — `VendaItem.Quantidade` vira `decimal`.** É a mudança estrutural; sem ela não há venda por peso. Cascateia por item de venda/pré-venda, totais, baixa de estoque e NFC-e. (Estoque já é `numeric(10,3)`.)
- **D2 — Produto pesável** com campos novos: `Pesavel` (bool), `Unidade` (`UN`/`KG`), `CodigoBalanca` (PLU — código interno usado no código de barras da balança). Preço por kg reusa `ProdutoDados.ValorVenda`.
- **D3 — Parse do código de barras da balança é comum aos 2 esquemas** e configurável (balanças variam). Config define prefixo, tamanho do código, tamanho do valor e se o valor embutido é **peso** ou **preço**.
- **D4 — Esquema 2 (hardware) fica pra Fase 3**, com a abordagem (agente local vs WebSerial) a decidir lá. Fases 1 e 2 não dependem de hardware.
- **D5 — Fiscal herda do cadastro** (unidade KG, NCM/CFOP de hortifruti/açougue). Sem lógica fiscal nova além de suportar quantidade decimal na NFC-e.

---

## Modelo de dados

### Produto (novos campos)
- `Pesavel: bool` — produto vendido por peso.
- `Unidade: string` (`"UN"` | `"KG"`; default `"UN"`) — unidade de venda/tributação.
- `CodigoBalanca: int?` — PLU. Único por filial (ou global) entre os pesáveis; é o "código interno" que entra no EAN da balança e no PDV.
- (Preço por kg = `ProdutoDados.ValorVenda`, já existe.)

### VendaItem
- `Quantidade`: `int` → **`decimal`** (`numeric(10,3)`). Migration + ajuste de todos os cálculos que assumem int.

### Configuração do código de barras da balança (global/por filial)
Chaves de config (ou uma entidade `BalancaConfig`):
- `balanca.barcode.prefixo` (ex.: `2`)
- `balanca.barcode.tam_codigo` (ex.: `5` ou `6`)
- `balanca.barcode.tam_valor` (ex.: `5`)
- `balanca.barcode.tipo_valor` (`peso` | `preco`)
- `balanca.barcode.casas_peso` (ex.: peso em gramas → 3 casas)

**Exemplo (tipo=preço, EAN-13):** `2 IIIII PPPPP C` → `I`=PLU (5), `P`=preço total em centavos (5), `C`=verificador. No PDV: prefixo `2` → resolve produto por PLU `IIIII` → preço do item = `PPPPP/100`; quantidade (peso) = preço ÷ preço/kg.
**Exemplo (tipo=peso):** `P` = peso em gramas → quantidade = `PPPPP/1000` kg; preço do item = peso × preço/kg.

---

## Fluxos

### Esquema 1 — Balança nos fundos (etiqueta) — Fase 2
1. **Export da tabela PLU** pra balança: o ERP gera um arquivo com `CodigoBalanca`, descrição, preço/kg, (validade, tara…) no formato da balança (Toledo/Filizola têm layouts próprios). Primeira versão: export genérico (TXT/CSV) documentado; refinar por marca depois.
2. A balança imprime a etiqueta com o EAN de balança (peso/preço embutido).
3. **No caixa (Fase 1):** bipar o EAN → PDV parseia (config D3) → resolve produto por PLU → item entra com quantidade (peso) e valor. Baixa de estoque em kg.

### Esquema 2 — Balança no caixa (peso em tempo real) — Fase 3
1. Operador seleciona o produto pesável (digita código/busca).
2. PDV **lê o peso** da balança conectada → calcula valor = peso × preço/kg → item entra.
3. **Ponte com a balança:** a decidir na Fase 3 —
   - **Agente local:** serviço em localhost lê a serial/USB e expõe o peso via HTTP/WebSocket (robusto, qualquer navegador; encaixa na estratégia de servidor local).
   - **WebSerial API:** Chrome/Edge lê a serial direto no navegador (sem instalar, mas limitado a esses navegadores e setup por máquina).
   - Protocolos comuns: Toledo, Filizola (serial, string de peso).

---

## Faseamento

1. **Fase 1 — Núcleo (software puro).**
   - `VendaItem.Quantidade` → decimal (migration + cálculos + NFC-e qCom/qTrib decimal + unidade KG).
   - Produto: `Pesavel`, `Unidade`, `CodigoBalanca` + tela (gated `pesavel`).
   - PDV/pré-venda: parse do EAN de balança (config D3) → resolve PLU + peso/preço → item com quantidade decimal. Exibir unidade (KG) e preço/kg na grade.
   - Já permite vender por peso bipando a etiqueta da balança dos fundos.
2. **Fase 2 — Esquema 1 (export PLU).** Exportar a tabela de produtos pesáveis pra balança (formato genérico → por marca).
3. **Fase 3 — Esquema 2 (hardware).** Ponte com a balança no caixa (agente local vs WebSerial — decidir aqui) + leitura de peso em tempo real no PDV.

---

## Impactos / riscos

- **`Quantidade` int → decimal** é invasivo: revisar toda soma/multiplicação de item, conferência de compras, relatórios, e a **NFC-e** (precisão de qCom/qTrib; unidade). Migration de coluna com dados existentes (int→numeric é seguro).
- **Arredondamento:** valor = peso × preço/kg arredondado a 2 casas; cuidar de centavos na NFC-e (mesma lógica do MapearItem que já arredonda).
- **PLU único:** garantir unicidade do `CodigoBalanca` entre pesáveis (índice).
- **Balança nos fundos** normalmente já tem software próprio pra imprimir; o export do ERP é pra manter a tabela sincronizada — não reinventar a impressão.
- **Produtos por unidade vs por peso:** nem todo item de hortifruti é por kg (ex.: 1 coco). O flag `Pesavel` + `Unidade` cobre; itens `UN` seguem o fluxo normal (quantidade decimal permite frações se precisar, mas default inteiro).
- **Não-pesável não muda:** farmácia/vestuário seguem com quantidade (agora decimal, mas usada como inteiro).

## Questões abertas
- `CodigoBalanca` **global** ou **por filial**? (PLU costuma ser por loja/balança).
- Config do barcode: **global** ou **por filial**? (lojas podem ter balanças diferentes).
- Export PLU: começar com **formato genérico** (TXT/CSV) e mapear Toledo/Filizola depois? Quais marcas o público-alvo usa?
- Fase 3: **agente local vs WebSerial** — decidir com base no parque de máquinas/navegador dos clientes.
- Precisão da NFC-e: qCom com 3 ou 4 casas? (a SEFAZ aceita até 4; peso em kg com 3 casas é o usual).

---

## Ordem de execução
Fase 1 (núcleo: quantidade decimal + produto pesável + parse do EAN) → Fase 2 (export PLU) → Fase 3 (balança no caixa). A Fase 1 já entrega venda por peso via etiqueta, sem hardware.

---

## Estado de implementação (as-built)

### Fase 1 — Núcleo ✅ (2026-07-04)
- **`VendaItem.Quantidade` int → decimal** (`numeric(10,3)`) + DTOs de venda. SNGPC/controlados seguem inteiros (cast, pois controlado é sempre UN). Fiscal preserva o peso.
- **NFC-e**: `qCom`/`qTrib` com peso decimal (`D4`) e `uCom`/`uTrib` = `Produto.Unidade` (antes era `UN` fixo + `Quantidade.0000`, que quebrava com peso). Migration `AddProdutoPesavelEQuantidadeDecimal`.
- **Produto**: `Pesavel` + `Unidade` (UN/KG) + `CodigoBalanca` (PLU, índice único parcial). Tela de produto com os campos gated pela feature `pesavel`.
- **Config do EAN de balança** (chaves `balanca.barcode.prefixo|tam_codigo|tam_valor|tipo_valor`, seed default: prefixo `2`, 6+5, `peso` em gramas).
- **PDV (caixa + pré-venda)**: `ProdutosController.Buscar` parseia o EAN de balança → resolve o produto pesável pelo PLU → devolve item com quantidade (peso) + preço/kg. Item entra com quantidade decimal, unidade KG, linha própria (não agrupa). Grid permite quantidade decimal só pra itens KG.
- **Falta na Fase 1 (melhorias):** exibir a coluna "Un." e o preço/kg mais explícito na grade do PDV; entrada manual de peso (hoje o peso vem do código de barras). Config do barcode ainda não tem tela (edita via seed/banco).

### Fase 2 — Export PLU ⏳ / Fase 3 — Balança no caixa ⏳
Pendentes.
