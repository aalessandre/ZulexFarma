# Classificação de Entidades para Replicação Multi-Master (fonte da verdade)

> Base do redesenho da replicação (ver `handoff-2026-07-13.md` e a memória do assistente).
> Decisões travadas com o dono em **2026-07-14**. As ambíguas foram resolvidas com os
> palpites do assistente + confirmação do dono (marcadas ✅). **A confirmar** = ainda pode mudar.

## Os dois eixos (nunca mais confundir)

| Eixo | Coluna | De onde vem | Pra que serve |
|---|---|---|---|
| **Origem / Nó** | `FilialOrigemId` (já existe em TODA BaseEntity) | config **local** do servidor (`Nó:Codigo`, ex-`Filial:Codigo`) | faixa de Id, prefixo do Código, "não puxar o que eu mandei", auditoria de ONDE foi feito |
| **Dono (idFilial)** | `FilialId` operacional (28 entidades hoje) | **usuário logado** | de qual filial o dado É → escopo da replicação |

Regra de replicação:
- **GLOBAL** → replica pra TODOS os nós (todos os locais + nuvem).
- **POR-FILIAL** → replica só pro nó da filial dona (`FilialId`) + nuvem.
- **INFRA** → não replica (fica no nó).

---

## GLOBAL (~55) — cadastro compartilhado, replica pra todos

**Pessoas/Cadastros:** Pessoa, PessoaContato, PessoaEndereco, Cliente, Fornecedor, Colaborador ✅,
Prescritor, Municipio, RamoVisibilidade
**Usuários/Acesso:** Usuario ✅ (tem FilialId "home", mas é cadastro global; acesso por filial é o
`UsuarioFilialGrupo`), GrupoUsuario, GrupoPermissao, UsuarioFilialGrupo
**Produtos — definição:** Produto, ProdutoBarras, ProdutoMs, ProdutoSubstancia, ProdutoFamilia,
ProdutoLocal, Substancia, Fabricante, ClassificacaoProdutoBase, GrupoPrincipal, GrupoProduto,
SubGrupo, Secao, AtributoVariacao, ValorAtributo, ProdutoAtributo, **ProdutoVariacao (def do SKU)**,
ProdutoVariacaoValor
**Convênios/Descontos/Comissão:** Convenio, ConvenioBloqueio, ConvenioDesconto, ClienteConvenio,
ClienteDesconto, ClienteBloqueio, ClienteAutorizacao, ClienteUsoContinuo, HierarquiaDesconto(+filhas),
HierarquiaComissao(+filhas), ComissaoFaixaDesconto, ColaboradorComissaoAgrupador
**Promoções:** Promocao ✅ (global, escopada por `PromocaoFilial`), PromocaoProduto, PromocaoFaixa,
PromocaoConvenio, PromocaoPagamento, PromocaoFilial
**Fidelidade:** CampanhaFidelidade, CampanhaFidelidadeItem, CampanhaFidelidadeFilial,
CampanhaFidelidadePagamento, PremioFidelidade, **Voucher ✅** (emitido numa filial, mas replica pra
todas — resgatável em qualquer filial)
**Fiscal — referência:** Ncm, NcmFederal, NcmIcmsUf, NcmStUf, IcmsUf, IbptTax, NaturezaOperacao,
NaturezaOperacaoRegra
**Financeiro — cadastro:** PlanoConta, TipoPagamento, Adquirente, AdquirenteBandeira,
**AdquirenteTarifa ✅** (global — taxa é do contrato da adquirente)
**Sistema:** Filial

---

## POR-FILIAL (~45) — dado operacional, replica só pra filial dona + nuvem

**Produtos — dados:** ProdutoDados, ProdutoFiscal, ProdutoFornecedor, ProdutoLote,
MovimentoLote ⟨+FilialId⟩, MovimentoEstoque, AtualizacaoPreco, AtualizacaoPrecoItem ⟨+FilialId⟩
**Vendas/Caixa:** Venda, VendaItem (POCO), VendaItemDesconto (POCO), VendaPagamento (POCO),
VendaFiscal ⟨+FilialId⟩, VendaItemFiscal ⟨+FilialId⟩, VendaReceita, VendaReceitaItem ⟨+FilialId⟩,
VendaFarmaciaPopular ⟨+FilialId⟩, VendaFarmaciaPopularItem ⟨+FilialId⟩, Caixa,
CaixaMovimento ⟨+FilialId⟩, CaixaFechamentoDeclarado ⟨+FilialId⟩
**Compras:** Compra, CompraProduto ⟨+FilialId⟩, CompraProdutoLote ⟨+FilialId⟩, CompraFiscal ⟨+FilialId⟩
**Financeiro — movimento:** ContaPagar, ContaReceber, MovimentoContaBancaria ⟨+FilialId⟩
**Entregas:** EntregaPerfil, EntregaFaixa ⟨+FilialId⟩, EntregaAgenda, Entrega, EntregaEvento ⟨+FilialId⟩
**SNGPC:** SngpcMapa, InventarioSngpc, InventarioSngpcItem ⟨+FilialId⟩
**SelfCheckout:** SelfCheckoutTerminal, SelfCheckoutConfiguracao,
SelfCheckoutChamadoAtendente ⟨+FilialId⟩, SelfCheckoutConciliacaoEstoque ⟨+FilialId⟩
**Config/Log (resolvidos por filial):**
- **Configuracao ✅ POR-FILIAL** (correção do dono — cada filial tem a sua). Precisa de `FilialId`
  → chave passa a ser `(FilialId, Chave)`. **Nota:** a config de NÓ/deployment (`Nó:Codigo`, URL do
  sync, intervalo) NÃO fica nesta tabela — fica no `appsettings` local (é propriedade do servidor).
- **LogAcao ✅ / LogErro ✅ POR-FILIAL** (auditoria pertence à filial onde ocorreu; alternativa:
  consolidar só na nuvem, sem redistribuir).

### Híbridos (FilialId nullable: nulo = global, preenchido = da filial) ✅
- **ContaBancaria** — conta da matriz (nula/global) vs conta da filial.
- **Feriado** — nacional (nulo/global) + override por filial (já modela os dois).

---

## INFRA (~12) — não replica (fica no nó)

SyncFila, SequenciaLocal, SequenciaCentral (alocação central de sequência fiscal),
AbcFarmaBase (referência externa de preços), DicionarioRevisao, DicionarioTabela,
DicionarioRelacionamento, CertificadoDigital (sensível/local), SefazNota (caixa de entrada,
por-filial mas não redistribui), **GestorTributarioJob ✅ / GestorTributarioUsoMensal ✅** (local/infra).

---

## LISTA DE TRABALHO 1 — por-filial SEM `FilialId` (17) → decidir estratégia

Todas são **filhas de um agregado que já tem `FilialId`**. Dois caminhos:
- **(a) Desnormalizar `FilialId`** (+FK Filial +índice) → roteamento direto + consulta isolada rápida.
  Recomendado para as consultadas sozinhas.
- **(b) Replicar junto do pai** (agregado atômico) → menos colunas; o filho nunca viaja sozinho.

| Entidade | Pai | Recomendação |
|---|---|---|
| MovimentoLote | ProdutoLote | (a) desnormalizar |
| MovimentoContaBancaria | ContaBancaria | (a) desnormalizar |
| CaixaMovimento | Caixa | (a) desnormalizar |
| CaixaFechamentoDeclarado | Caixa | (a) desnormalizar |
| AtualizacaoPrecoItem | AtualizacaoPreco | (b) junto do pai |
| VendaFiscal / VendaItemFiscal | Venda / VendaItem | (b) junto da Venda |
| VendaReceitaItem | VendaReceita | (b) |
| VendaFarmaciaPopular(+Item) | Venda | (b) |
| CompraProduto / CompraProdutoLote / CompraFiscal | Compra | (b) junto da Compra |
| EntregaFaixa / EntregaEvento | EntregaPerfil / Entrega | (b) |
| InventarioSngpcItem | InventarioSngpc | (b) |
| SelfCheckoutChamadoAtendente / SelfCheckoutConciliacaoEstoque | Terminal / VendaItem | (b) |

## LISTA DE TRABALHO 2 — POCOs da venda (sem BaseEntity)
`VendaItem`, `VendaItemDesconto`, `VendaPagamento` não são `BaseEntity` e não replicam sozinhos.
**Decisão (palpite):** tratar **Venda como agregado atômico** (item/pagamento só viajam dentro da
venda) — em vez de promovê-los a BaseEntity+FilialId.

## LISTA DE TRABALHO 3 — furo do sync ATUAL (bug, independe do redesenho)
O dicionário `ResolverTipo` (`SyncApplicator.cs`, ~61 tabelas) está **incompleto**: NÃO replicam hoje,
mas deveriam — **Caixa, ContaReceber (!), MovimentoEstoque, MovimentoLote, ProdutoLote, VendaItem,
VendaPagamento, SngpcMapa, Inventario\*, SelfCheckout\***, além de cadastros globais **Municipio,
Adquirente\*, NaturezaOperacao\*, Variação\* (ProdutoVariacao/AtributoVariacao/…), HierarquiaComissao,
IbptTax**. Curiosidade: **ContaPagar replica, ContaReceber não.** Reconciliar o dicionário com esta
classificação antes de definir o escopo.

---

## Próximos passos
1. Adicionar `FilialId` (+FK +índice) nas por-filial que faltam (lista 1, opção a/b por entidade).
2. Separar/renomear o eixo Nó (`FilialOrigemId` → conceito "nó"); mover config de nó pro `appsettings`.
3. Reconciliar o dicionário do sync (lista 3).
4. Codigo: `nextval` nativo + prefixo de nó.
5. Conflito: LWW real por `AtualizadoEm` (global); por-filial já é livre de conflito cruzado.
6. Escopo do PULL: global→todos; por-filial→só a filial dona + nuvem.
7. Multi-tenant: `TenantId` + roteamento de connection string + provisionamento.
