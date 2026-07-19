# SPEC — conflito de coleção POCO concorrente (b + c)

> **Origem:** achado ALTO da auditoria cross-cutting (17/07/2026). Design produzido por um workflow
> multi-agente (levantamento de fatos → 3 designs independentes → painel de juízes → síntese), todo
> aterrado em `arquivo:linha` reais.
>
> **✅ NÚCLEO IMPLEMENTADO em 18/07/2026** (commits `e7bdaba` fase 6 + `85398be` fase 6b): as 5 folhas
> de Cliente promovidas a BaseEntity + invariante de boot + fix FURO 1 (Cascade→Restrict) + higiene.
> Revisão adversarial do b+c pegou e curou: **A1** (o `RemoveRange`+re-add do `ClienteService`
> duplicava sob edição concorrente → `ClienteService.ReconciliarFilhos` diff-preserve por chave
> natural) e **M2** (D de pai referenciado abandonado → `PrecisaRetry`; RESTRICT do PG é 23001 e o
> `EhFkViolation` só pegava 23503). Suíte 81/81. **✅ Join-tables classificadas (18/07):** todas as 19 da whitelist
> ficam substituição — a checagem do código (os 6 pais editam em bloco) refutou a hipótese de união
> do design (ver seção "DECISÃO DO DONO" abaixo). **Pendente (não bloqueia):** smoke-test do fluxo de
> edição de cliente no app (o diff é lógica de negócio). Ver seção 5c do plano.

## O problema (recap)

Filhos POCO (não-`BaseEntity`) de agregados viajam no JSON do pai e são reconciliados por
**delete-missing** (coleção presente no JSON = autoritativa; filho ausente morre). Sob concorrência
multi-master — dois nós adicionam filhos DIFERENTES à MESMA coleção do MESMO agregado (nó A add
`ClienteConvenio` cv2, nó B add cv3 no mesmo Cliente) — o LWW elege UM agregado vencedor e o
delete-missing apaga o filho do perdedor **em silêncio** (viola o invariante do dono: nada silencioso).

## Decisão de design (síntese dos 3 designs + painel)

- **Eixo UNIÃO vs SUBSTITUIÇÃO** (do design pragmático). Nem toda coleção deve ser promovida: coleção
  de **valor coerente** (ex.: `PromocaoFaixa` = faixas de desconto) ficaria PIOR com união (mesclaria
  faixas contraditórias) — ali LWW-substituição é o certo. Só as coleções **união-por-natureza**
  (vínculos operacionais acrescidos independentemente em várias lojas) viram `BaseEntity`. Ancorado em
  precedente real: `CampanhaFidelidadeItem : BaseEntity` já convive com `CampanhaFidelidadeFilial`/
  `Pagamento` POCO sob a mesma campanha Global.
- **(b) = INVARIANTE FAIL-CLOSED DE BOOT, não auditoria runtime.** Filho POCO **não tem** escritor
  (`NoOrigemId`/`AtualizadoPorNoId` são de `BaseEntity`); no ponto do delete-missing
  (`SyncApplicator.cs:361-363`) só há `(paiId, Id)`. Sem escritor não há como distinguir remoção
  legítima de perda-por-conflito → qualquer auditoria runtime **ou audita tudo (ruído proibido) ou é
  código-morto** (os dois designs que propuseram audit foram derrubados pelo painel — o discriminador
  por escritor-do-pai confunde concorrente com hand-off sequencial cross-node; o por faixa-de-Id é
  desmentido pelo próprio guard `ColisaoIdentidade`). A realização honesta e sem-ruído de "nada
  silencioso" é **estrutural, no boot**: toda coleção POCO de agregado Global ou é promovida (c) ou é
  uma entrada nominal e revisável numa whitelist. *(Reinterpreta o (b) literal do dono — "quarentena
  runtime" — para "invariante de boot"; fiel à restrição REAL "visível + sem ruído", que a quarentena
  runtime comprovadamente viola. **Requer OK do dono.**)*

## FURO 1 (verificado no código — governa o escopo do (c))

`CarregarFilhosCascataAsync` só gera D+lápide de um filho no delete do pai se a FK for `Cascade` **E**
tiver nav inversa **E** o filho for `BaseEntity` (`AppDbContext.cs:2293-2297`). Várias join-tables têm
uma **2ª FK `Cascade`** pra outro agregado com `WithMany()` VAZIO (sem nav inversa):
`ClienteConvenio→Convenio` (`:1664`), `ClienteBloqueio→TipoPagamento` (`:1685`), e as de Promoção.
**Gate resolvido (17/07):** `ConvenioService`/`TipoPagamentoService` fazem hard-delete (fallback
soft no catch de FK) e a FK é Cascade → o hard-delete **cascateia** os vínculos → FURO 1 é **LIVE**.
Promover `ClienteConvenio`/`ClienteBloqueio` sem fix = regressão (cascade sem lápide → ressurreição).
**Fix (recomendado):** 2ª FK `Cascade → Restrict` (`AppDbContext.cs:1664` e `:1685`). Alinha com o
invariante do repo ("delete que esbarra em FK vira erro de negócio, nunca 500") e compõe com o
try-hard-delete-catch-soft-delete existente (Convênio com clientes vinculados passa a soft-delete).

## 1. Lista fechada

### (c) PROMOVER a `BaseEntity` — as 5 folhas de `Cliente` (`Cliente.cs:38-42`)
`ClienteAutorizacao`, `ClienteDesconto`, `ClienteUsoContinuo` (2ª FK Produto = `Restrict`, sem FURO 1),
`ClienteConvenio` e `ClienteBloqueio` (exigem o fix FURO 1). São o cenário-âncora do ALTO,
operacionais-por-cliente, acrescidos em várias lojas. **Todas folhas** (sem netos).

### (b) FICAM POCO + entrada nominal na whitelist (SUBSTITUIÇÃO aceita, revisável)
Demais filhos POCO de agregado Global: `Convenio` (`ConvenioDesconto`, `ConvenioBloqueio`), `Promocao`
(`PromocaoFaixa`, `PromocaoFilial`, `PromocaoPagamento`, `PromocaoConvenio`, `PromocaoProduto`),
`HierarquiaDesconto`/`HierarquiaComissao` (`Item`+neto `Secao`, `Colaborador`, `Cliente`, `Convenio`),
`Adquirente` (`AdquirenteBandeira`+neto `AdquirenteTarifa`), `CampanhaFidelidade` (`Filial`,
`Pagamento`). Filhos de **`Venda`** (PorFilial, single-writer) ficam POCO e **fora** da invariante.

### ✅ DECISÃO DO DONO (18/07/2026) — join-tables de vínculo puro: SUBSTITUIÇÃO
`HierarquiaDescontoColaborador/Cliente/Convenio`, `HierarquiaComissaoColaborador`, `PromocaoConvenio/
Filial/Pagamento/Produto`, `AdquirenteBandeira`, `CampanhaFidelidadeFilial/Pagamento` — o design supôs
que fossem **candidatas fortes a UNIÃO** ("duas lojas vinculam itens DIFERENTES → ambos sobrevivem",
regra "na dúvida promova"). **A checagem do código REFUTOU a suposição:** os 6 pais editam a coleção em
BLOCO (`RemoveRange`+re-add no `AtualizarAsync` — `ConvenioService:218,233`, `PromocaoService:186-190`,
`HierarquiaDescontoService:112-116`, `HierarquiaComissaoService:104-106`, `AdquirenteService:80-81`,
`CampanhaFidelidadeService:131-136`). NÃO existe fluxo de "adicionar 1 vínculo"; edita-se o form
inteiro. Sob esse padrão, união NÃO é o seguro: mesclaria dois forms concorrentes num estado que nenhum
editor autorou (pior que LWW limpo). Union nunca perde LINHA, mas aqui perderia a INTENÇÃO. Por isso
**todas ficam SUBSTITUIÇÃO** — nada migra de (b) para (c). A invariante de boot segue forçando a decisão
explícita de qualquer coleção POCO nova.

## 2. (c) Migração (por folha promovida)

Cada `Cliente*` já tem `long Id` identity. Herdar `BaseEntity` adiciona **7 colunas** (`Codigo`,
`CriadoEm`, `AtualizadoEm`, `Ativo`, `NoOrigemId`, `AtualizadoPorNoId`, `SyncGuid`) + os **2 índices**
que a convenção (`AppDbContext.cs:1863-1886`) materializa (`SyncGuid` UNIQUE default `gen_random_uuid()`
+ `(Codigo,NoOrigemId)` UNIQUE filtrado). **5 tabelas → 35 colunas + 10 índices, tudo `AddColumn`/
`CreateIndex` mecânico.** Sequences: ZERO código (`ConfigurarSequences`/`CriarSequencesCodigo` varrem
por reflexão). **SyncGuid: greenfield-only** — `gen_random_uuid()` basta (um só nó); **NÃO** fazer
backfill determinístico (`uuidv5` colidiria em Ids reciclados — footgun provado pelo `ColisaoIdentidade`).
Migração TEM que entrar **antes do 2º nó real**.

**Registro em código (senão o boot cai fail-closed):** (1) `class ClienteConvenio : BaseEntity` (remover
`Id` local); (2) += os 5 tipos em `SyncRegistry.Globais`; (3) += no dicionário `_tiposPorTabela` do
applicator; (4) `GetOrdemTabela` filho `3` (> pai `Clientes`=`2`); (5) `DbSet` de cada; (6) cascade+nav
inversa do dono já OK → D+lápide de graça no delete do Cliente.

**O que sai do JSON do pai / reconciliação ignora:** AUTOMÁTICO — os 3 pontos bifurcam por
`!IsAssignableFrom(BaseEntity)` (`SyncApplicator.cs:181-182`, `:319-320`; `AppDbContext.cs:2609-2610`).
**Higiene (a), barata:** relaxar `RemoverColecoesNaoCarregadas` (`:2609`) pra tirar coleção BaseEntity
do JSON do pai (hoje viaja embutida, inerte mas incha o payload) + apagar comentário morto
"ExtrairFilhosPoco" (`SyncApplicator.cs:191-195`).

**Prova de convergência:** cada filho vira op I própria (faixa-por-nó + SyncGuid) → A add cv2, B add cv3
→ ambos replicam → `{cv2,cv3}` nos dois nós (LWW do pai não alcança mais o filho). Delete via cascade do
Cliente → op D + lápide próprios (conserta de brinde o delete-por-cascade hoje silencioso em POCO).
Edição do mesmo filho → LWW por `(AtualizadoEm, AtualizadoPorNoId)`.

## 3. (b) Mecanismo
Nada muda no apply; **sem auditoria runtime**. Delete-missing das coleções POCO continua idêntico (é a
realização correta do LWW-substituição). "Nada silencioso" vem da INVARIANTE (§4). *Rede opcional
CONFIÁVEL (defesa em profundidade, deferível):* pra filho de **PorFilial** (`Venda`), delete-missing
disparado por op cujo `NoOrigemId` ≠ nó-dono da filial (derivação CONFIÁVEL, não guess de POCO) →
quarentena `DeleteMissingPorFilialForaDoDono` + prossegue. Sem ruído.

## 4. Invariante de boot (`SyncRegistry.ValidarModelo`)
> Toda coleção de filho POCO alcançável de um agregado **Global** (recursivo pelos filhos POCO, mesmo
> predicado de `SyncApplicator.cs:319-320`) deve ter o tipo do filho na whitelist
> `ColecoesPocoSubstituicaoAceitas`. Senão, boot NÃO sobe, com a lista nominal.

Varredura: para cada `Type` em `Globais`, iterar `GetNavigations().Where(IsCollection && !BaseEntity)`,
recursivo pelos POCO (pega netos `Secao`/`Tarifa`); cada `TargetEntityType.ClrType` fora da whitelist →
`problemas.Add`. Parte do agregado e só segue nav-de-coleção do dono → a join-table de 2 FKs é alcançada
só pela FK dona (sem a ambiguidade "qual é o pai"). Whitelist inicial = as coleções (b) do §1 + netos.
Efeito: os 5 `Cliente*` somem da varredura (viraram BaseEntity); coleção POCO NOVA sob pai Global no
futuro **quebra o boot** com nome. `Venda` (PorFilial) não é varrida.

## 5. Testes (Postgres real, vermelho→verde)
- **T-INV-1/2:** invariante morde (fake Global+POCO fora da whitelist → lança; remover um real da
  whitelist → boot cai) e não falso-positiva (`Venda` PorFilial não lança).
- **T-C-UNIAO (o ALTO):** 2 nós add `ClienteConvenio` diferentes + U do Cliente que perde LWW → ambos os
  nós com `{cv2,cv3}`, nada apagado.
- **T-C-DELETE:** deletar Cliente (cascade) → cada `ClienteConvenio` gera D+lápide → some em B; I velho
  de cv2 barrado pela lápide.
- **T-C-LWW-FILHO:** editar o mesmo filho nos 2 nós → maior `(AtualizadoEm, escritor)` vence.
- **T-FURO1:** hard-delete do 2º pai (Convenio) referenciado por `ClienteConvenio` promovido → com
  fix Restrict: erro de negócio (400), filho sobrevive. Sem fix: expõe ressurreição (vermelho).
- **T-B-SUBSTITUICAO:** 2 nós editam a mesma `Promocao` (faixas divergentes) → LWW elege uma,
  delete-missing roda, `SyncQuarentena` **vazia** (prova sem-ruído).
- **T-REGRESSAO:** `CampanhaFidelidadeItem` (já BaseEntity) segue união; `Filial`/`Pagamento` seguem POCO.

## 6. Ordem, riscos, tamanho
**Ordem (maior valor/menor risco primeiro):** (1) gate de dados [FEITO: FURO 1 live, greenfield ok];
(2) **invariante + whitelist** — zero migração, deployável já, torna "nada silencioso" estrutural; (3)
higiene (a); (4) promover as 3 folhas SEM FURO 1; (5) promover as 2 COM FURO 1 (após o fix Restrict);
(6) opcional: rede PorFilial-dono; (7) ✅ resolvido: join-tables classificadas como SUBSTITUIÇÃO (nenhuma união).

**Riscos:** índice `SyncGuid` UNIQUE global tem que entrar antes do 2º nó (greenfield ok); NÃO fazer
backfill determinístico; FK-order → `PrecisaRetry` teto alto (só reduz incidência); a invariante só
cobre Global (se um PorFilial virar multi-writer no futuro, a rede §3 ou a premissa fiscal cobrem).

**Tamanho do núcleo que fecha o ALTO:** 1 migration (5 tabelas: 35 col + 10 idx) + fix Restrict em 2
relações + ~15 linhas de registro + 1 bloco na `ValidarModelo` + 1 whitelist (~19 entradas). Sem tabela
nova, sem SQL cru, sem promover ~18 tabelas de baixa concorrência, sem lógica nova no caminho quente.
