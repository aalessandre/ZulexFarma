# Plano de Correção da Replicação — execução (2026-07-17)

**Destinatário:** Claude Opus (executor)
**Autor:** Claude Fable 5, após análise cruzada de: `synAteAqui.md` (retrato honesto de 16/07), `orientacao-replicacao-codex-2026-07-17.md` (auditoria independente do Codex/ChatGPT) e **verificação direta do código** na branch `dev-pc1` (código de sync idêntico ao `origin/main`, HEAD `a96336f`).
**Regra herdada do synAteAqui:** fato verificado > memória. Toda referência `arquivo:linha` deste plano foi conferida no código em 17/07/2026 — mas **re-verifique antes de editar**, porque linhas driftam.

---

## 0. Veredicto e estratégia

O Codex acertou no diagnóstico: **verifiquei no código e praticamente todos os P0 dele são reais** (tabela na §2). O synAteAqui também está correto no que aponta (§6.1 gap do cursor, §6.2 filhos POCO) — os dois documentos convergem, o Codex apenas cobre mais superfície (segurança, domínio, bootstrap).

Onde eu **divirjo do Codex**: o remédio dele é um protocolo enterprise completo (HLC, WriterEpoch, SubscriptionGeneration, NodeDelivery materializado por nó, mTLS, sagas). Para um ERP com **um banco por cliente, meia dúzia de nós por tenant, zero produção multi-nó hoje e um dev + IA como time**, implementar tudo isso antes de ligar o segundo nó é o jeito mais provável de nunca ligar o segundo nó. A base existente (outbox atômico, OpUid, quarentena, lápides, faixas de Id) está **verificada e boa** — o próprio Codex manda preservá-la.

**Estratégia travada: evoluir o mecanismo existente em 6 fases com gates de teste, não reescrever.** Cada peça do Codex foi classificada em: *fazer agora* (fases 0-5), *adiada com gatilho explícito* (§8) ou *rejeitada*. O critério de corte foi: **o que é indispensável para 2-3 nós convergirem sem perda silenciosa e sem furo de segurança** — nada além.

Regras inegociáveis (do dono):

1. **Falha ruidosa > perda silenciosa.** O painel precisa refletir o real. Qualquer descarte/decisão importante aparece.
2. **Nenhum segundo nó real liga antes do Gate 5.**
3. **"Compilou" não prova nada neste subsistema** (placar: 4 desenhos derrubados por revisão). Todo fix estrutural nasce com teste que falhava antes.

---

## 1. Decisões TRAVADAS (não re-decidir, não re-litigar)

| Tema | Decisão | Fonte |
|---|---|---|
| Mecanismo | Caseiro (outbox+fila+apply), gerenciável pelo painel. PG nativo descartado | dono |
| Topologia | Estrela: edges → hub (nó 0, Railway). Sem peer-to-peer | dono |
| Tenancy | 1 banco por cliente. TenantId fora de escopo | dono |
| Escrita | Multi-master: nuvem é gravável (venda móvel) | dono |
| Conflito | **LWW por linha** por `AtualizadoEm`, desempate pelo **escritor real** (não pelo criador) | dono 07/14 + P0.5 |
| PK | `long` global por faixa (`noCodigo × 1e9`), preservada cross-nó. FKs valem sem remapeamento. **Não migrar pra GUID** | dono + Codex P0.10 |
| `SyncGuid` | Vira guard `NOT NULL UNIQUE`; par `Id ↔ SyncGuid` divergente = colisão, rejeitar (nunca `SetValues`) | Codex P0.10 |
| Fiscal | Numeração pinada ao nó dono; `SequenciasCentrais` NÃO replica (já está em `_tabelasSemSync`, `AppDbContext.cs:2082-2083` ✅) | dono |
| Escopo | GLOBAL → todos; POR-FILIAL → dono + hub; INFRA → nunca. Fonte: `classificacao-replicacao.md` (decisões 2026-07-14) | dono |
| `Codigo` | Sequencial puro por nó (`nextval`); unicidade por índice composto `(NoOrigemId, Codigo)`; exibição com prefixo `N-` no front. Códigos legados não migram | dono 07/14 |
| Cursor | **Publicador + `SeqEntrega`** (opção B do synAteAqui §6.1). NÃO usar `pg_snapshot_xmin` com cursor em `Id`, NÃO varredura por tempo, NÃO `MIN(cursor)` sem registro de nós | synAteAqui + Codex |
| Produto | Propagação "tudo exceto estoque" via cópia no service (`CopiarDadosParaOutrasFiliais`) permanece; redesenho template/override é backlog | spec cadastro-produto |

## 1b. Decisões ASSUMIDAS neste plano (defaults meus — dono pode vetar antes da fase correspondente)

| # | Default assumido | Fase |
|---|---|---|
| A1 | **CONFIRMADO pelo dono (17/07):** endpoint `GET /api/auth/senha-sistema` é **removido**. O suporte obtém a senha do dia pelo **ZulexAdmin** (`GET /api/produtos/senha-dia` — root-only + auditado, já existe). Algoritmo idêntico nos dois projetos (verificado: ambos `SHA256(UtcNow yyyyMMdd + SistemaKey)[..8].ToLower()`); requisito: a `SistemaKey` NOVA (rotacionada) precisa ser configurada igual no ErpPharma E no env do ZulexAdmin (não confiar no fallback do ZulexAdmin pro JWT secret dele — não bate) | 0 |
| A2 | Relógio continua **hora de parede de Brasília** (sem DST desde 2019). UTC-em-tudo é migração invasiva que não cura skew. Contrapartida: NTP obrigatório nos nós + guard de timestamp absurdo (op > 5min no futuro → quarentena) | 3 |
| A3 | Escopo do PULL passa a vir do **cadastro do nó no hub** (`?filiais=` da query é ignorado) | 1 |
| A4 | `Configuracoes` **para de replicar já** (entra em `_tabelasSemSync`); modelagem por-filial `(FilialId, Chave)` fica pro backlog. Estado do sync sai dela pra tabela própria | 2 |
| A5 | SLA de nó offline: **30 dias** (configurável). Acima disso, rebootstrap — por ação explícita no painel, nunca automático | 5 |
| A6 | Bootstrap de nó novo = **janela de manutenção** (dump/restore + watermark). Bootstrap online é backlog | 5 |
| A7 | Lápides deixam de ser purgadas (são 4 campos — custo ~nada; purga por idade é o que permite ressurreição pós-restore) | 3 |
| A8 | Auth de nó = **chave por nó + JWT com policy própria**. mTLS é backlog | 1 |

---

## 2. Estado verificado do código (17/07, branch `dev-pc1`)

Confirmado por inspeção direta — este é o baseline sobre o qual o plano opera:

| Achado | Evidência | Fase que cura |
|---|---|---|
| `SyncController` inteiro só com `[Authorize]`, sem policy — qualquer JWT humano usa `/enviar`, `/receber`, `/fila` (que expõe `DadosJson` integral), `/limpar`, `/resetar-recebimento` | `SyncController.cs:13`, `:261,291-297` | 1 |
| `GET /api/auth/senha-sistema?key=` **anônimo** devolve a senha diária do SISTEMA (que dá token admin); `key` comparada com `SistemaApiKey` versionada no git | `AuthController.cs:79-94` | 0 |
| Segredos reais versionados: senha do banco, JWT secret, `SistemaKey`, `SistemaApiKey` | `appsettings.json:3,6,32-33` | 0 |
| `"No": {"Codigo": 0}` versionado → fail-fast do `Program.cs:269-283` nunca dispara; loja sem env sobe silenciosamente como hub | `appsettings.json:38-40` | 0 |
| Gap do cursor: PULL serve `Id > ultimoId`; Id nasce no INSERT, visibilidade no COMMIT → op que commita tarde é perdida pra sempre | `SyncController.cs:169`; `SyncBackgroundService.cs:271` | 2 |
| Push desonesto: hub retorna 200 com `errosDb>0`; edge marca lote INTEIRO `Enviado=true` em qualquer 2xx; hub re-enfileira até ops quarentenadas | `SyncController.cs:73,84,115-138,145`; `SyncBackgroundService.cs:195-198` | 2 |
| PULL não carrega `OpUid` (idempotência só no push) | `SyncController.cs:173-177` | 2 |
| Cursor do pull vive em `Configuracoes` (`sync.ultimo.id.recebido`) e **`Configuracoes` replica como GLOBAL** (não está em `_tabelasSemSync` `AppDbContext.cs:2077-2084` e está no dicionário do applicator `SyncApplicator.cs:639`) — cursor de um nó pode vazar pra outro | verificado 17/07 | 2 |
| LWW não-atômico: SELECT + compara em memória + `SetValues`, sem lock/CAS; desempate usa `entidade.NoOrigemId` (criador da linha), não o escritor da op | `SyncApplicator.cs:275-287` | 3 |
| U/D não converge: D grava lápide, U mais novo sobre linha ausente vira `PrecisaRetry` eterno; noutro nó o D vira `Stale` — um nó vivo, outro morto | `SyncApplicator.cs:46-59,105-120` | 3 |
| Descarte por LWW (`Stale`) é 100% silencioso | `SyncApplicator.cs:67-69,252-254,395` | 2 |
| Filhos POCO: applicator é append/update-only (sem delete); services fazem `RemoveRange`+re-add com Ids novos → duplicação em edição rotineira. 8 services afetados | `SyncApplicator.cs:183-213`; `ClienteService.cs:181-197` e afins | 3 |
| Classificação de escopo fail-open: não classificado → `null` → GLOBAL; cadeia de pai não resolvida → Warning + GLOBAL | `AppDbContext.cs:2140-2163` | 4 |
| No hub, `NoOrigemId` da op vem do claim `filialId` do JWT (mistura os eixos; quebra anti-eco pra edições feitas na nuvem) | `AppDbContext.cs:2281` | 1 |
| `SyncGuid` tem índice NÃO-unique; merge por `SyncGuid` usa `FirstOrDefault` | `AppDbContext.cs:1864-1869` | 3 |
| Sem registro de nós, sem detecção de gêmeo, sem entrega por nó | — | 1 |
| Quarentena agrupa por `(Tabela,RegistroId,Operacao)` — versões distintas se sobrescrevem | `SyncApplicator.cs:319-320` | 3 |
| Lápides purgadas por idade fixa 90d, sem ACK | `SyncApplicator.cs:219,254-258` | 3 |
| Seeds inseguros multi-nó: `ProdutosFiscal` com `FilialId=1` hardcoded em SQL cru; 27 `IcmsUf` criados com sync LIGADO (cada edge publica os seus → colisão por chave natural) | `DatabaseSeeder.cs:37-44,297,351-369` | 4 |
| `GerarCodigo` concatena sem separador (`{no}{seq}` → nó 1+seq 11 == nó 11+seq 1 == "111") | `AppDbContext.cs:2426` | 4 |
| `MovimentoEstoque` sem idempotência, sem unique, aceita UPDATE via LWW (é ledger conceitual) | `MovimentoEstoque.cs:14-38`; `AppDbContext.cs:1030` | 4 |
| `VendaService.FinalizarAsync` = múltiplos `SaveChangesAsync` **sem transação** (contas a receber `:484`, estoque `:563`, entrega `:578`, caixa `:611/616`, SNGPC `:627`) — crash no meio deixa venda parcial, e o sync replica a parcialidade fielmente | `VendaService.cs:289-630` | 4 |
| **Zero** projeto de testes backend | — | 0 |
| Comentário morto "TUDO replica" contradizendo o design | `AppDbContext.cs:2074` | doc |

O que está **bom e não se mexe** (verificado): outbox no mesmo commit (`AppDbContext.cs:2316-2392`), `AplicandoSync` short-circuit, `OpUid` com índice parcial único no push, cascata do banco → D+lápide (`CarregarFilhosCascataAsync`, testado em runtime), lápide no delete local, SAVEPOINT no fallback do `GerarCodigo`, TLS validado + backoff no transporte, faixas de Id com RESTART não-regressivo, `SequenciasCentrais` fora do sync.

---

## 3. As fases

Ordem obrigatória: 0 → 1 → 2 → 3 → 4 → 5. Não paralelizar fases; dentro da fase, um commit por preocupação. Cada fase termina com o gate verde + revisão adversarial (subagente com o prompt "tente provar que isso perde/duplica/vaza dado").

### FASE 0 — Segredos, identidade de deployment e fundação de testes

> ✅ **EXECUTADA em 17/07/2026** (Fable, branch `dev-pc1`): commits `85fa79d` (segurança), `8fa5a24`
> (No:Modo), `265b577` (suíte de testes — **7 vermelhos provando os bugs + 12 verdes**), `72ee0af`
> (limpeza documental). **Gate 0 BATIDO** com uma ressalva: os VALORES dos segredos ainda são os
> antigos (a rotação exige ação do dono na Railway — checklist no fim do plano). Ajustes de rota
> em relação ao texto original: o teste "EntidadeNaoClassificada" virou dois casos concretos
> (`Configuracao_NaoDeveEntrarNaSyncFila` + `MovimentoEstoque_EhLedger`) porque o furo do
> `ContasReceber` citado nos docs JÁ estava fechado pela fase 3a antiga; e o teste de segurança é
> estrutural (reflection sobre as policies), não de servidor vivo.

*Invariante-alvo: nenhum segredo no git; nenhum nó sobe com identidade errada em silêncio; os bugs conhecidos têm teste vermelho que os prova.*

1. **Rotacionar e externalizar segredos.** Tirar do `appsettings.json`: connection string (senha), `Jwt:SecretKey`, `SistemaKey`, `SistemaApiKey` → env vars (`ConnectionStrings__DefaultConnection`, etc.). Gerar valores NOVOS (os atuais estão comprometidos pelo histórico do git — trocar também na Railway e no PG local). O `appsettings.json` fica com placeholders vazios + `appsettings.Development.json` **no .gitignore** para o dev local.
2. **Remover `GET /api/auth/senha-sistema`** (`AuthController.cs:79-94`) e a config `SistemaApiKey` (só existe para ele) — decisão A1 confirmada pelo dono. NÃO criar ferramenta nova: o suporte usa o gerador de senha do dia do **ZulexAdmin** (`ProdutosController.SenhaDia`, root-only + auditado; algoritmo já idêntico ao `SenhaDiaService` do ErpPharma). Ao rotacionar a `SistemaKey` (item 1), configurar o MESMO valor novo no env do ZulexAdmin (`SistemaKey`), senão as senhas divergem (o fallback do ZulexAdmin usa o JWT secret dele).
3. **Modo de deployment explícito.** Nova config obrigatória `No:Modo` = `Hub | Edge | StandaloneCloud` (sem default versionado; remover `"No"` do `appsettings.json`). Fail-fast REAL no `Program.cs`: `Modo` ausente/inválido → `Log.Fatal` + throw antes de qualquer hosted service. Regras: `Hub` força `No:Codigo=0`; `Edge` exige `No:Codigo>=1` E `Sync:UrlCentral`; `StandaloneCloud` **não gera outbox** (curar P1.1: hoje `Sync:Habilitado=false` só desliga o transporte, `AppDbContext` segue enchendo `SyncFila` pra sempre) e não roda loop. O log do early-return do loop (`SyncBackgroundService.cs:70-74`) passa a dizer a verdade por modo.
4. **Projeto `backend/ZulexPharma.Tests`** (xunit). Fixture de **Postgres real** (EF InMemory proibido — não reproduz sequences/locks/isolamento): connection string em `ERPPHARMA_TEST_PG`, cria database descartável por run + roda migrations. Escrever os testes **vermelhos** que provam os bugs atuais (eles viram os testes de aceite das fases seguintes):
   - `Gap_CursorId_PerdeCommitTardio` (duas tx, a de Id menor commita depois; consumidor perde);
   - `Lww_DoisPushesConcorrentes_UltimoFisicoVence` (corrida do SetValues);
   - `UpdateDelete_OrdensDiferentes_NaoConvergem` (permutações U/D);
   - `EditarCliente_DuplicaFilhosNoDestino`;
   - `EntidadeNaoClassificada_VazaComoGlobal`;
   - `JwtHumano_AcessaEnviarReceber` (segurança).

**Gate 0:** segredos fora do repo e rotacionados; boot falha sem `No:Modo`; suíte roda e os 6 testes acima estão vermelhos *pelo motivo certo*.

### FASE 1 — Identidade e autorização de nó

> ✅ **EXECUTADA em 17/07/2026** (Fable, `dev-pc1`): commits `b431d53` (código), `c461acd` (testes),
> `34fa917` (fase 1b — correções da revisão adversarial). **Gate 1 BATIDO**: suíte com 33 verdes +
> 6 vermelhos (os bugs das fases 2-4). A revisão adversarial (obrigatória pelo protocolo) encontrou
> **1 CRÍTICO real**: a origem da op tinha um SEGUNDO site com fallback pro criador (`noOrigemOp`) —
> o commit inicial declarava P0.3 curado e só tinha curado metade; hub editando registro de loja
> nunca chegaria à loja dona. Mais 3 altos (token de nó vazava pra API inteira → `SyncNodeGate`;
> corrida no anti-gêmeo → CAS; cadastro sem filiais → 422 ruidoso) e 2 médios (Status não cortava o
> data plane; não-2xx não contava como falha). Tudo curado e testado em `34fa917`. **Lição repetida:
> a revisão adversarial pegou de novo o que o autor declarou pronto.**
>
> **ROLLOUT da fase 1 (deployar hub+lojas JUNTOS):** (1) subir hub; (2) cadastrar cada nó:
> `POST /api/sync/nos {noCodigo, nome, filiais:[...]}` (admin; a resposta traz a CHAVE — aparece 1x);
> **preencher as filiais é obrigatório** (cadastro vazio + edge declarando filiais = 422 proposital);
> (3) configurar `Sync:NoChave` no edge (env/appsettings.Development.json); (4) subir edge. Até lá o
> transporte fica parado com status CONFIG/AUTH visível — backlog acumula na fila, nada se perde.
> O painel `/erp/sync` agora exige usuário ADMIN (não-admin toma 403).

*Invariante-alvo: só nó cadastrado e autenticado alcança o data plane; a origem de cada op é derivada pelo servidor; nó gêmeo não sobe.*

1. **Tabela `SyncNos`** (INFRA, não replica; hub-only na prática): `NoCodigo int UNIQUE`, `InstanciaUid uuid`, `Modo`, `Status (Provisionando|Bootstrapping|Ativo|Suspenso|RebootstrapNecessario|Desativado)`, `ChaveHash`, `UltimoAckSeq bigint`, `UltimoPushEm`, `UltimoPullEm`, `VersaoApp`, `CriadoEm`. + **`SyncNoFiliais`** (`NoCodigo`, `FilialId`) = escopo autorizado do pull. CRUD no painel (admin).
2. **Credencial por nó.** Ao cadastrar o nó no hub, gerar chave aleatória (exibida 1x; hash guardado). Edge configura `Sync:NoChave` via env. Novo endpoint `POST /api/sync/handshake` (anônimo): valida `(NoCodigo, InstanciaUid, chave)` contra `SyncNos` → emite JWT curto com claims `syncNode=true, noCodigo`. **Anti-gêmeo:** primeiro handshake grava `InstanciaUid`; handshake com `InstanciaUid` diferente para o mesmo `NoCodigo` → 409 `NoGemeoDetectado`, o edge loga fatal e NÃO liga o loop (falha ruidosa). Re-instalação legítima = botão "Resetar instância" no painel.
3. **Policies.** `SyncNode` (claim `syncNode`) em `/enviar` e `/receber`. `Admin` (role real de usuário) em `/fila`, `/quarentena`, `/reprocessar`, `/limpar`, `/resetar-recebimento`, `/status`, CRUD de nós. `/fila` **para de expor `DadosJson`** na listagem (só num `GET /fila/{id}/payload` admin, auditado). O transporte **aposenta o login SISTEMA** (o usuário virtual pode continuar existindo pro suporte humano, mas sem servir de credencial de máquina).
4. **Origem correta no hub:** `AppDbContext.cs:2281` vira `var noOrigem = _noCodigo;` — SEMPRE o nó do servidor, nunca claim de filial (cura P0.3: edição feita na nuvem passa a chegar ao nó criador do registro, porque o anti-eco compara nó, não filial).
5. **Escopo server-side:** `/receber` ignora `?filiais=` e deriva o escopo de `SyncNoFiliais` do nó autenticado (decisão A3). Edge continua configurando `No:Filiais` só como documentação local/validação.
6. Limites de request no data plane: teto servidor de lote (qtde e bytes) no `/enviar` e `/receber`.

**Gate 1 (testes):** JWT humano → 403 em `/enviar`/`/receber`; nó A não recebe escopo de B mesmo pedindo; gêmeo (mesma `NoCodigo`, `InstanciaUid` diferente) → 409 e loop não sobe; hub gera op com `NoOrigemId=0` mesmo com JWT de filial no contexto.

### FASE 2 — Entrega sem gap + push honesto + estado visível

> ✅ **EXECUTADA em 17/07/2026** (Fable, `dev-pc1`): commits `b58b395` (código), `be50be0` (testes),
> `238fd58` (fase 2b — revisão adversarial). **Gate 2 BATIDO**: suíte 41 verdes + 4 vermelhos
> (LWW/U-D/filhos/ledger — fases 3-4). **O GAP DO CURSOR ESTÁ FECHADO** — o teste que reproduzia a
> perda do commit tardio agora prova a entrega. A revisão adversarial pegou de novo 1 CRÍTICO: a
> geração do hub NÃO detectava restore (o restore restaura o mesmo uuid) — cura robusta: o edge
> trata REGRESSÃO DA MARCA (`seqMaxNumerado < cursor`) como fatal REBOOTSTRAP. Mais: 426 pra edge
> de protocolo antigo (senão congelava em silêncio), LWW no upsert da quarentena (op velha
> sobrescrevia a nova — divergência), ampliar filial de nó ativo → RebootstrapNecessario,
> resetar-recebimento limpa a geração vista, índice parcial pro publicador, `NaoProcessada` não
> marca Enviado, `POST /quarentena/descartar`. **Deferido pra fase 3** (anotar): teto de tentativas
> do hub é por EXECUÇÃO de drenagem, não por tempo — PrecisaRetry pode "prender" em ~40min de
> pushes com nó de origem offline (cura: backoff temporal na quarentena, junto do rework por OpUid).
> **Rollout:** hub+lojas JUNTOS (o hub rejeita pull antigo com 426); cursor novo começa em 0 →
> primeiro pull re-aplica tudo idempotente (volume atual pequeno).

*Invariante-alvo: toda op commitada e elegível é eventualmente numerada com `SeqEntrega` única e imutável; o cursor só anda sobre números já atribuídos; nenhum descarte é silencioso; falha de transporte nunca marca enviado.*

1. **`SeqEntrega bigint NULL` na `SyncFila`** + sequence `seq_sync_entrega` + índice `(SeqEntrega) WHERE SeqEntrega IS NOT NULL`. **Publicador oportunista no hub** (a central não roda background loop — não criar um só pra isso): no início do `/receber` e no fim do `/enviar`, sob `pg_try_advisory_xact_lock(chave_fixa)` — quem pega o lock numera (`UPDATE SyncFila SET SeqEntrega = nextval(...) WHERE SeqEntrega IS NULL AND <elegível>`); quem não pega serve só o que já está numerado. Linha que commita tarde pega número MAIOR na rodada seguinte — transação longa não trava nada. Buracos de `nextval` são inofensivos com cursor `>`.
   - **Elegível para numerar = op aplicada com sucesso no hub (ou resolvida).** Op que caiu em quarentena NÃO é numerada (não redistribui conflito — cura P1.4); quando o retry da quarentena aplicar, a op fica elegível.
2. **Cursor = `SeqEntrega`.** `/receber`: `WHERE SeqEntrega > :cursor AND SeqEntrega IS NOT NULL AND <anti-eco por nó> AND <escopo>` ORDER BY `SeqEntrega` LIMIT teto. A resposta inclui **`seqMaxNumerado`** (MAX global numerado) para o cursor avançar mesmo com lote vazio/todo filtrado. A resposta inclui **`OpUid` de cada op** (cura P1.8; painel do receptor passa a deduplicar e auditar).
3. **Geração do hub:** valor `sync.hub.geracao` (uuid criado uma vez, guardado na tabela nova de estado — item 4). Toda resposta do `/receber` o inclui; o edge persiste junto do cursor; mudou (restore/promoção do hub) → edge PARA com status `RebootstrapNecessario` visível no painel, não puxa lote parcial. Barato agora, impagável depois.
4. **Estado local do sync sai de `Configuracoes`.** Nova entidade `SyncEstadoLocal` (INFRA, `_tabelasSemSync`): cursor, geração do hub vista, marcas de última execução. **`Configuracoes` entra em `_tabelasSemSync` JÁ** (decisão A4) — remover também do dicionário do applicator (`SyncApplicator.cs:639`). Migração: copiar `sync.ultimo.id.recebido` pro novo lugar. ATENÇÃO: a troca de cursor Id→SeqEntrega exige **corte coordenado** (ver §5, migração).
5. **Push honesto (cura P0.14/P1.2):** `/enviar` responde **por op**: `{opUid, resultado: Aplicado|Duplicado|Stale|Quarentena|Rejeitado}`. Edge marca `Enviado=true` só nas ops presentes na resposta (todas essas estão duráveis no hub — inclusive `Quarentena`, que é problema do hub resolver e aparece no painel dele); resposta HTTP não-2xx ou corpo inválido → **nada** é marcado, backoff conta falha de verdade (hoje `SyncBackgroundService.cs:97-104` engole).
6. **`Stale` visível (objetivo 7):** descarte por LWW grava registro auditável — reusar `SyncQuarentena` com `Motivo=Stale`, `Resolvido=true` (sem retry), payload da op perdedora. Painel ganha filtro "Descartados (LWW)". Vale nos dois lados (hub `/enviar` e edge apply).
7. **ACK:** o request do `/receber` leva `ack=<cursor durável atual>`; hub grava em `SyncNos.UltimoAckSeq` + `UltimoPullEm`. (Retenção só na fase 5.) Push atualiza `UltimoPushEm`. O `/status` do painel passa a ler estado **persistido por nó**, não variável estática (cura P1.14).
8. **Cursor do edge avança atomicamente:** persistir cursor na mesma transação do processamento do lote (aplicações + quarentenas + cursor num commit). Re-pull após crash re-aplica idempotente (por Id/OpUid).

**Gate 2 (testes):** `Gap_CursorId_PerdeCommitTardio` verde com o desenho novo (tx longa concorrente não perde op — ela é numerada depois e entregue); resposta perdida no push → reenvio → `Duplicado`, efeito único; op quarentenada no hub não aparece em pull de terceiro até resolver; `Stale` aparece na quarentena-auditoria; crash do edge no meio do lote → re-pull sem perda nem efeito duplo; mudança de geração do hub → edge para com estado visível.

### FASE 3 — Convergência: LWW atômico, U/D, filhos POCO

> ✅ **EXECUTADA em 17/07/2026** (Fable, `dev-pc1`): commits `7fbad80` (código), `3cfce13` (testes),
> `19a45ae` (fase 3b — revisão adversarial). **Gate 3 BATIDO**: suíte 57 verdes + 1 vermelho
> (MovimentoEstoque ledger — fase 4). Entregue: `AtualizadoPorNoId` (escritor real) + comparador
> único (ts, escritor) valendo pra linha×linha×lápide; atomicidade por op (tx + advisory lock por
> registro — applies concorrentes serializam, cabeçalho+filhos num commit); U/D convergente (U mais
> novo que a lápide recria; U órfão = upsert); contrato de coleção nos DOIS lados (outbox omite
> não-carregada, "I" inclui tudo, force-load no touch-do-pai; applicator: chave presente =
> delete-missing recursivo com savepoint); `SyncGuid` UNIQUE + `ColisaoIdentidade` (linha pré-sync
> adota o guid); `RelogioSuspeito` (+5min, teto alto); lápides SEM purga (A7); touch-do-pai pra
> edição só-de-filho (todos os pais com nav de coleção; kiosk coberto). A revisão adversarial pegou
> DE NOVO 2 críticos declarados prontos (delete-missing commitando sem os inserts; tracker stale do
> lote furando o LWW) — 3ª fase seguida em que o método salva. **Pendências anotadas** (fase 4/5):
> teto da quarentena por TEMPO (não execuções) no hub; guard de identidade pros filhos POCO;
> migration da fase 3 recria ~96 índices únicos — em produção futura, pré-criar fora de pico.

*Invariante-alvo: dadas as mesmas ops em qualquer ordem, todos os nós terminam no mesmo estado (linhas E lápides); editar um agregado N vezes não duplica filho.*

1. **Escritor real na linha:** coluna `AtualizadoPorNoId int NULL` na `BaseEntity` (uma migration, ~130 tabelas, nullable, sem backfill). Outbox carimba `_noCodigo` em toda escrita local; applicator carimba `op.NoOrigemId` ao aplicar remoto. `NoOrigemId` da entidade volta a ser só "criador" (histórico).
2. **Comparador ÚNICO de versão** (uma função, usada em TODOS os caminhos): versão = `(AtualizadoEm ?? CriadoEm, escritor)` onde escritor do incoming = `op.NoOrigemId` e do estado atual = `AtualizadoPorNoId ?? NoOrigemId ?? 0`; para lápide, versão = `(DeletadoEm, NoOrigemId da lápide)`. Empate de timestamp → maior escritor vence. Guard A2: op com timestamp > agora+5min → quarentena `RelogioSuspeito`.
3. **Atomicidade do apply:** cada op roda em transação com `SELECT ... FOR UPDATE` na linha alvo (SQL cru por Id; e na lápide correspondente — mesma tx). Dois applies concorrentes serializam; decisão linha×lápide é atômica (cura P0.5 e metade do P0.6).
4. **U/D convergente (cura P0.6):** com o comparador único —
   - `D` mais novo que linha → apaga + grava lápide com a versão do D;
   - `D` mais velho que linha → `Stale` (visível);
   - `D` sobre linha ausente → grava/atualiza lápide (NUNCA `PrecisaRetry`);
   - `U`/`I` mais novo que lápide → **recria a linha** (o JSON carrega o estado completo) + remove/rebaixa a lápide;
   - `U` mais velho que lápide → `Stale` (visível);
   - `U` sobre linha ausente SEM lápide → tratar como `I` (upsert) — o retry eterno morre.
5. **Lápides param de ser purgadas** (decisão A7): remover `PurgarTombstonesAsync`/`PurgarLapidesSePreciso` do caminho por idade; lápide só some em decommission explícito. (São 4 campos; anti-ressurreição vale mais que os bytes.)
6. **Filhos POCO — contrato de coleção (cura P0.7 / synAteAqui §6.2):**
   - **Contrato:** no JSON do pai, **chave de coleção AUSENTE = "não carregada, preserve"; chave PRESENTE (mesmo `[]`) = autoritativa, reconcilie**.
   - **Outbox:** ao serializar o pai, incluir só coleções POCO com `IsLoaded=true` (as não carregadas são omitidas do payload — implementar sem sujar o ChangeTracker).
   - **Applicator (`UpsertFilhosPocoAsync`):** coleção presente → diff por Id: upsert os presentes, **DELETE os ausentes**; coleção omitida → não toca. Isso conserta a replicação mesmo com os services recriando filhos com Ids novos (os Ids velhos são deletados no destino porque não estão no JSON autoritativo).
   - **Edição só de filho:** nos 8 services que fazem `RemoveRange`+re-add (`Cliente`, `Convenio`, `Promocao`, `HierarquiaComissao`, `HierarquiaDesconto`, `Venda`, `SelfCheckoutVenda`, `Adquirente`), garantir que o pai sofra touch (`AtualizadoEm`) para a op do pai existir e o LWW do agregado andar. (Refatorar os services para diff-preservando-Ids é melhoria posterior, não pré-requisito.)
7. **`SyncGuid` guard:** migration diagnóstica de duplicados + índice **UNIQUE** por tabela; no apply com PK já existente, validar `payload.SyncGuid == existente.SyncGuid` — divergiu → quarentena `ColisaoIdentidade` (P0 real de nó gêmeo/faixa errada), nunca `SetValues`.
8. **Quarentena por `OpUid`:** chave passa a `OpUid` (unique); mantém dedup legado por `(Tabela,RegistroId,Operacao)` apenas para op sem `OpUid`. Preserva versões distintas (cura P1.3).

**Gate 3 (testes):** matriz completa U/I/D × (antes/depois/empate) × 2 nós converge — estado E lápides idênticos; `EditarCliente_DuplicaFilhosNoDestino` verde; venda finalizada sem `Include` de descontos NÃO apaga descontos no destino (coleção omitida); colisão de `SyncGuid` → quarentena, zero merge cego; corrida de dois applies na mesma linha → serializa, versão certa vence.

### FASE 4 — Registry fail-closed + domínio mínimo

> ✅ **EXECUTADA (núcleo de replicação) em 17/07/2026** (Fable, `dev-pc1`): commits `4c0c786` +
> `2f8904b` (fase 4b — revisão adversarial). **Suíte 61/61 VERDE — zero bugs conhecidos abertos no
> subsistema.** Entregue: `SyncRegistry` (classificação explícita de TODAS as entidades + validação
> fail-closed no boot: entidade nova sem classificar/fora do dicionário = boot não sobe com lista
> nominal); dono não resolvido = quarentena `DonoNaoResolvido` reabrível (nunca GLOBAL; exceção
> legítima: ContaBancaria corporativa); ledger append-only (`MovimentosEstoque/Lote` só aceitam I;
> outbox não emite U/D de ledger — cascata de exclusão de Perda coberta); `Codigo` = `{no}-{seq}`
> (separador mata a ambiguidade; sequencial puro colidiria com legado do nó 1) + índice único
> composto (Codigo, NoOrigemId) com re-codificação das duplicatas legadas NA migration (boot-morto
> confirmado no banco de dev e curado); seeds determinísticos (fiscal usa a filial do nó e hub não
> semeia; TiposPagamento 1-4 e IcmsUf 1-27 fixos, sem enfileirar; setval no hub).
> **PENDÊNCIAS da fase 4 (fazer em sessão própria, com o app rodando):**
> 1. **4b-transação (P0.15)**: `VendaService.FinalizarAsync` e suprimento/sangria do caixa em
>    transação única — mudança de fluxo de negócio; o bloqueio que causou a reversão antiga
>    (row-bump) já morreu com o nextval. Exige verificação em runtime (finalizar venda real).
> 2. **Frota mista TiposPagamento/IcmsUf** (pré-existente): hub tem 1-4, edge existente tem
>    1e9+1..4 — alinhar no bootstrap da fase 5 (ou remap manual das FKs).
> 3. Teto da quarentena por TEMPO no hub (herdado da fase 3).

*Invariante-alvo: nenhuma entidade replica (ou deixa de replicar) por acidente; ledger não sofre LWW; a finalização da venda é atômica; seeds não colidem entre nós.*

1. **Registry único de replicação** (novo arquivo em Infrastructure, ex.: `SyncRegistry.cs`): TODA entidade do modelo declara `Escopo (Global | PorFilial | Infra)` + resolver de dono (para PorFilial). **Validação no startup** (fail-fast): DbSet sem classificação → boot falha com mensagem nominal. Captura (outbox), applicator (`ResolverTipo`) e escopo (`FilialDonoId`) passam a consumir ESTE registry — eliminando o trio atual denylist (`_tabelasSemSync`) + allowlist (~61 no applicator) + dicionário por-filial, que hoje divergem. Fonte da classificação: `classificacao-replicacao.md` (GLOBAL ~55, POR-FILIAL ~45, INFRA ~12, híbridos `ContaBancaria`/`Feriado` por `FilialId` nullable). Isso fecha de uma vez o furo "**ContaPagar replica, ContaReceber não**", Caixa, MovimentoEstoque/Lote, ProdutoLote, Sngpc*, SelfCheckout* etc.
2. **Fail-closed no dono (cura P0.8):** entidade PorFilial cujo dono não resolve → a op vai para **quarentena local** com motivo `DonoNaoResolvido` (visível), NUNCA vira GLOBAL. Trocar o `return null` de `AppDbContext.cs:2158-2160`.
3. **`Codigo` sem ambiguidade:** `GerarCodigo` passa a retornar só o sequencial (`nextval`); unicidade garantida por índice `(NoOrigemId, Codigo)` nas tabelas com código visível; exibição `"{no}-{codigo}"` no frontend onde precisar distinguir. Códigos legados ficam como estão (únicos de fato).
4. **`MovimentoEstoque`/`MovimentoLote` viram append-only no protocolo:** applicator aceita só `I` (dedup por Id — a PK já é global); `U`/`D` remoto → quarentena `LedgerImutavel`. Localmente, correção de estoque = movimento de AJUSTE novo, nunca editar movimento (conferir services; bloquear edição). `SaldoApos` é informativo. (`ProdutoDados.EstoqueAtual` continua snapshot LWW por-filial — limitação conhecida, backlog B6.)
5. **Transação única na finalização (cura P0.15):** envolver `VendaService.FinalizarAsync` inteiro em `BeginTransaction` (o outbox já participa de tx ambiente — `AppDbContext.cs:2320`). O bloqueio que motivou a reversão anterior (row-bump de `SequenciasLocais` + `LogsAcao`) morreu quando `GerarCodigo` virou `nextval` — **confirmar em teste de carga leve antes de dar por resolvido** (2 finalizações concorrentes não serializam uma na outra). Mesmo tratamento para suprimento/sangria (`CaixaMovimentoService.cs:131-146,311-329`): caixa e banco no mesmo commit.
6. **Seeds seguros multi-nó (cura P0.12):**
   - `DatabaseSeeder.cs:37-44`: trocar `FilialId=1` hardcoded pelo `filialSeedId` do nó;
   - Dados de referência (`IcmsUf`, e revisar `NaturezaOperacao`/afins): IDs **fixos determinísticos** (mesmos em todos os nós) e criados com `AplicandoSync=true` (não enfileira). Todos os nós nascem com as mesmas linhas nas mesmas PKs → update replica por Id sem colisão. `TiposPagamento`: mesmo padrão;
   - Correções SQL de startup viram migrations idempotentes conscientes de escopo.

**Gate 4 (testes):** DbSet novo sem classificação → boot falha (teste cria entidade fake); `ContaReceber` replica e chega; `SequenciasCentrais`/`SyncEstadoLocal`/`Configuracoes` NÃO saem no pull; `U` remoto de `MovimentoEstoque` → quarentena; kill -9 no meio da finalização → banco íntegro (tudo ou nada); dois nós recém-seedados não geram nenhuma op de seed colidente.

### FASE 5 — Bootstrap, retenção e painel por nó

> ✅ **EXECUTADA (backend) em 17/07/2026** (Fable, `dev-pc1`): commits `f0b897b` + `a66877c` (fase 5b
> — revisão adversarial). **Suíte 71/71 VERDE.** Entregue: retenção da fila central por ACK
> religada com fail-closed (`/limpar` do hub apaga só `SeqEntrega <= MAX realmente apagável do
> MIN(ack dos Ativos)`, write-ahead da marca; nó sem ack retém tudo; nó não-Ativo sai do MIN só por
> ação explícita); guard de compactação (pull abaixo da marca = 409 → edge para com `REBOOTSTRAP`);
> `GET /bootstrap-info` + `POST /cursor` + runbook `runbook-bootstrap-no.md` (drenar outbox antes do
> restore, `Sync:Habilitado=false` até cravar cursor, re-semear `seq_codigo_*`); reposicionamento da
> faixa de Id imune a restore (`DecidirRestartSequence`, pura + 7 testes — o guard antigo por
> MAX-global causava colisão de PK entre nós); `GET /checksum` (contagem + md5 de (Id, AtualizadoEm)
> com `to_char` fixo); `GET /nos` com `atrasoSeq` + `alertaSla`. A 5ª revisão adversarial pegou 2
> críticos (colisão de faixa pós-restore; perda do outbox no rebootstrap) — 5ª fase seguida que o
> método salva. **PENDENTE do Gate 5 (só o dono faz):** upgrade visual do painel `/erp/sync`
> (seção de nós, filtro "descartados LWW", botões de bootstrap — o backend já expõe tudo) e o
> **piloto real** (2 PCs + Railway, 48h de caos). **Só depois do piloto o segundo nó real entra.**

*Invariante-alvo: nó novo nasce consistente e provado; a fila central não cresce sem teto; o painel responde "qual nó está atrasado e por quê" sem psql.*

1. **Bootstrap por janela de manutenção (decisão A6) — runbook + suporte mínimo no código:**
   1. cadastrar nó no hub (`Provisionando`) e emitir credencial;
   2. na janela: drenar pushes, rodar o publicador até `SeqEntrega` completo, anotar **watermark** = MAX(SeqEntrega);
   3. `pg_dump` do banco do tenant no hub **excluindo INFRA** (`SyncFila`, `SyncEstadoLocal`, `SyncNos`, quarentena, credenciais, certificados);
   4. restore no edge; seeder detecta banco populado e NÃO re-seeda; configurar `No:Modo=Edge`, código, chave;
   5. gravar cursor = watermark + geração do hub; status `Ativo`; ligar o loop;
   6. validar: contagem + hash por tabela do escopo (ferramenta de comparação — item 4).
2. **Retenção da fila central (religada SÓ agora, cura da §6.3 do synAteAqui):** apagar/compactar `SyncFila` do hub onde `SeqEntrega <= MIN(UltimoAckSeq dos nós Ativos)` e idade > margem (ex.: 7d). Nó `Suspenso`/`RebootstrapNecessario`/`Desativado` sai do MIN **somente por ação explícita** no painel (mudar o status é a ação — auditada). Nó parado há mais do que o SLA (A5: 30d) → alerta no painel sugerindo a ação; nunca automático. No edge, o `/limpar` existente já cobre (`Enviado=true` + idade).
3. **Mudança de escopo do nó** (`SyncNoFiliais`): ampliar escopo → hub marca `RebootstrapNecessario` (eventos antigos da filial nova não serão re-entregues pelo cursor). Reduzir escopo → ok sem rebootstrap. Regra simples no update do cadastro.
4. **Ferramenta de reconciliação:** endpoint admin `GET /api/sync/checksum?tabela=` → `{count, hash}` por tabela respeitando escopo (hash de `(Id, AtualizadoEm)` ordenado); painel compara hub × nó e pinta divergência.
5. **Painel (tela `/erp/sync` existente) — upgrade:** seção "Nós" (por nó: status, último push/pull/ACK, backlog = `seqMaxNumerado - UltimoAckSeq`, versão, alerta de SLA); filtro "Descartados (LWW)"; quarentena por `OpUid`; indicador de geração; progresso de bootstrap. Backend disso já existe quase todo das fases 1-2 — aqui é exposição.

**Gate 5 (piloto):** 2 PCs + Railway, dados sintéticos, 48h de operação com caos (matar processo em cada fronteira de commit/ACK, derrubar rede, relógio adiantado em um nó): checksum zerado ao final em todas as tabelas do escopo; painel refletiu cada anomalia injetada; fila central estável (retenção funcionando). **Só depois disso um segundo nó real.**

---

## 4. Migração V1 → V2 (corte do cursor)

Não haverá dupla escrita de protocolos — é evolução in-place, mas a troca de cursor (Id → SeqEntrega) precisa de corte:

1. Deploy das fases no hub e nos nós de DEV (hoje só existe 1 nó real + hub — a migração é barata AGORA; é mais um motivo pra não adiar);
2. Com o transporte parado: publicador numera todo o backlog elegível do hub; quarentenas antigas são resolvidas ou marcadas descartadas (auditoria por `OpUid`);
3. Converter cursor de cada nó: `novo cursor = SeqEntrega` correspondente ao último Id processado (script; na dúvida, cursor = 0 e re-pull completo — o apply é idempotente por Id/OpUid e o volume atual permite);
4. Validar com checksum (fase 5 item 4) antes de reabrir.

---

## 5. O que NÃO fazer (herdado e reforçado)

- NÃO usar `pg_snapshot_xmin` com cursor em `Id` (as ordens de XID e identity são independentes — contraexemplo formal no synAteAqui §6.1).
- NÃO varredura por tempo/`CriadoEm`, NÃO safety-window, NÃO `MAX(Id)`.
- NÃO numerar `SeqEntrega` dentro da transação de negócio.
- NÃO confiar em `filialId`/`filiais`/origem vindos do cliente.
- NÃO delete-missing de coleção sem a chave presente no JSON (contrato da fase 3).
- NÃO LWW em ledger (movimento de estoque/caixa), venda finalizada ou documento fiscal.
- NÃO purgar lápide/fence por idade.
- NÃO reciclar `NoCodigo`, PK ou sequence após restore.
- NÃO redistribuir op em quarentena/conflito antes de resolvida.
- NÃO merge automático de dois `Id`s pelo `SyncGuid` (quarentena + decisão humana).
- NÃO ligar segundo nó real antes do Gate 5. NÃO aceitar "compilou, sobe" — teste vermelho→verde ou não aconteceu.

## 5c. PENDÊNCIAS PRÉ-PRODUÇÃO (bloqueiam o 2º nó REAL — não o piloto controlado)

Estado em 18/07/2026: **backend das 6 fases + b+c (fase 6/6b) feito, suíte 81/81 verde, 5 revisões
adversariais por fase + auditoria cross-cutting + design workflow do b+c + revisão do b+c.** O núcleo
está sólido (converge, não vaza, não perde). O que falta antes de ligar um 2º nó REAL:

1. **✅ RESOLVIDO (fase 6/6b) — Conflito em coleção POCO sob concorrência.** Dono decidiu **b+c**:
   **(c)** as 5 folhas de `Cliente` (ClienteConvenio/Autorizacao/Desconto/UsoContinuo/Bloqueio)
   viraram `BaseEntity` → replicam sozinhas (união natural, nada se perde); **(b)** invariante de
   boot em `SyncRegistry.ValidarModelo` (coleção POCO de agregado Global tem que estar promovida OU
   na whitelist `ColecoesPocoSubstituicaoAceitas` — senão o boot cai com a lista nominal). Revisão do
   b+c pegou e curou: A1 (o `RemoveRange`+re-add do `ClienteService` duplicava sob edição concorrente
   → `ReconciliarFilhos` diff-preserve por chave natural), M2 (D de pai referenciado abandonado →
   `PrecisaRetry`; RESTRICT do PG é 23001, não 23503). Design em `spec-conflito-poco-bc.md`.
   **Sub-pendências:** (i) **✅ join-tables classificadas (18/07/2026)** — todas as 19 coleções da
   whitelist ficam substituição (decisão + prova no item 5 abaixo); nada a migrar; (ii)
   **churn residual** só nos serviços que ainda fazem RemoveRange+re-add fora do Cliente (não crítico —
   as coleções deles ficaram POCO/substituição); (iii) o diff do `ClienteService.AtualizarAsync` é
   lógica de negócio (tem teste de service, mas vale **smoke-test do fluxo de edição de cliente no
   app** antes de produção — junto do item 2).

2. **🟠 Transação única na finalização de COMPRA e VENDA (P0.15 + P0.15-compra, fase 4b).** Dois
   fluxos de negócio ainda efetivam em vários `SaveChanges` sem transação — crash no meio deixa estado
   parcial, e o outbox replica a parcialidade fielmente. **Ordem: compra PRIMEIRO** (foi onde o dono
   parou antes de pivotar pra replicação), venda depois. Ambos exigem **verificação em runtime**
   (efetivar de verdade, matar o processo no meio) — sessão própria com o app rodando. Parados agora
   porque o foco virou a implantação/piloto da replicação (runbook-implantacao.md).
   - **2a. ✅ Compra — `CompraService.FinalizarAsync` (:772): transação única IMPLEMENTADA (19/07).**
     Toda a efetivação (estoque, custo médio, movimentos, lotes via `RegistrarEntradaAsync`, contas a
     pagar, cabeçalho) agora commita numa `BeginTransactionAsync`/`CommitAsync` única. Os logs de
     auditoria saíram PRA FORA da tx (coletados no loop, emitidos após o commit — auditoria não aborta a
     compra nem envenena a tx). O flush por item foi preservado (mantém o custo médio, que lê o estoque
     dos SKUs no banco). **Provado com Postgres real** — `CompraTransacaoTests` (BugsAtivos): red→green
     (a falha no lote DEPOIS de mexer no estoque deixava estoque/movimento commitados; agora rollback
     total) + caminho feliz (estoque 10 / custo 5 / 1 movimento / lote / 1 conta a pagar / Finalizada).
     Suíte **83/83 verde**. Contexto original do bug: o estoque de LOTE entrava via
     `_loteService.RegistrarEntradaAsync` (salva sozinho, `ProdutoLoteService.cs:81,109`) e o log
     (`LogAcaoService:43`) commitava por produto — crash no meio = compra pela metade / refinalizar
     dobrava o estoque (o autor já sabia, comentário `:786-787`). **Estorno também feito (19/07):**
     `ExcluirAsync` (compra finalizada) na MESMA transação, logs fora — teste red→green
     (`ExcluirAsync_FalhaAoReverter`: reverter o estoque e estourar deixava estoque zerado + conta a
     pagar apagada + compra intacta; agora rollback total). Suíte **84/84 verde**. **Falta ainda:** a
     venda (2b). Commits locais, sem deploy.
   - **2b. Venda — `VendaService.FinalizarAsync` (:289), SEM `BeginTransaction`.** 6 `SaveChanges`
     soltos (contas a receber `:484`, estoque/movimentos/lotes `:563`, entrega `:578`, caixa
     `:611/616`, SNGPC `:627`) + suprimento/sangria do caixa (`CaixaMovimentoService.cs:131-146,
     311-329`). Sub-serviços (`_loteService`/`_entregaService`/`_receitaService`/`_fpService`) NÃO
     abrem tx própria e compartilham o mesmo `AppDbContext` scoped → alistam na tx ambiente
     (confirmado 18/07). A pré-auth Farmácia Popular (`:402`) é HTTP externo e fica FORA/antes da tx.
     Precedente pronto: `SelfCheckoutVendaService.cs:79` já finaliza venda em transação. O bloqueio da
     reversão antiga (row-bump de `SequenciasLocais`) morreu com o `nextval`. Prova preferível: teste
     de fault-injection no harness (falha entre saves → rollback total), melhor que matar processo vivo.
   - **Smoke-test do fluxo de edição de cliente** (o diff da fase 6b) cabe junto quando o app subir.
   - **Fora do escopo da transação da compra (pendências de FEATURE, decisão do dono 19/07 — não são
     atomicidade, são funcionalidade que a finalização NÃO faz hoje):** (a) **SNGPC de entrada** — a
     `CompraService.FinalizarAsync` não registra SNGPC de produto controlado que entra por compra
     (existe `CompraSngpcService` + `SngpcOptOut`, mas não é chamado na finalização); (b) **movimento
     bancário/caixa no pagamento** — com `NotaPaga=true` ela só marca a `ContaPagar` como `Pago` +
     `DataPagamento`, sem criar a saída bancária/caixa (a contrapartida financeira que a venda tem no
     recebimento); (c) **escrituração fiscal / livro de entradas / SPED** — carrega dados fiscais por
     item mas não gera lançamento fiscal. Avaliar cada uma como feature própria, depois.

3. **🟡 Frota mista de seeds** (pré-existente, não regressão): hub tem TiposPagamento/IcmsUf em faixa
   antiga, edge novo tem Ids fixos 1-4/1-27. Um edge EXISTENTE que só recebe o deploy novo continua
   na faixa antiga e suas vendas quebrariam FK no hub. Resolve no **bootstrap** do edge (herda as
   linhas do hub) OU num remap manual das FKs. Documentar no runbook de upgrade da frota.

4. **🟢 Teto da quarentena por TEMPO no hub** (herdado da fase 3): `PrecisaRetry` mede tentativas por
   EXECUÇÃO de drenagem, não por tempo; no hub (drena a cada push de qualquer edge) pode "prender"
   uma op legítima em ~40min. Cura: carimbo de próximo-retry (backoff temporal).

5. **✅ RESOLVIDO (18/07/2026) — Classificação das join-tables de vínculo.** Dono decidiu: **as 19
   coleções POCO da whitelist `ColecoesPocoSubstituicaoAceitas` ficam SUBSTITUIÇÃO** (nenhuma vira
   união). Prova de código: os 6 pais (Convênio/Promoção/HierarquiaDesconto/HierarquiaComissão/
   Adquirente/CampanhaFidelidade) editam os filhos em BLOCO (`RemoveRange`+re-add no `AtualizarAsync` —
   `ConvenioService:218,233`, `PromocaoService:186-190`, `HierarquiaDescontoService:112-116`,
   `HierarquiaComissaoService:104-106`, `AdquirenteService:80-81`, `CampanhaFidelidadeService:131-136`)
   → LWW-agregado (o form inteiro mais novo vence) é a semântica certa; união fundiria dois forms
   concorrentes num estado que nenhum editor autorou. Assimetria com os filhos de Cliente (união na
   fase 6) é PROPOSITAL: Cliente é multi-fluxo/alta-concorrência, estes são config central editada de
   uma vez. Decisão registrada no comentário da whitelist (`SyncRegistry.cs`). Nada a migrar; a
   invariante de boot continua forçando classificação explícita de qualquer coleção POCO nova. Revisar
   só se a operação real mostrar dois nós adicionando vínculos independentes ao mesmo pai.

**Gate do piloto → produção:** o item 2 é o bloqueador remanescente do 2º nó real (o POCO/b+c já foi
resolvido na fase 6/6b); 3, 4 e 5 podem ir com o rollout se documentados. O piloto controlado (dados
sintéticos) pode rodar ANTES, operando em torno deles, para exercitar o fluxo e reproduzir os
cenários de caos (ver §5 do relatório da auditoria).

## 6. Backlog ADIADO deliberadamente (com gatilho de retorno)

| # | Item (origem) | Gatilho para voltar |
|---|---|---|
| B1 | HLC (Codex P0.5) | conflitos reais atribuíveis a skew de relógio apesar de NTP + guard |
| B2 | `WriterEpoch`/fencing/takeover formal (Codex §7.2) | failover de escrita operacional pra nuvem virar requisito de produto |
| B3 | `SubscriptionGeneration` completo + backfill online | mudança de escopo de nó virar operação frequente (hoje: rebootstrap na janela) |
| B4 | `NodeDelivery` materializado por nó | >20 nós por tenant ou necessidade de auditoria por-entrega |
| B5 | mTLS / client assertion (Codex P0.2) | exposição pública multi-cliente do data plane |
| B6 | Saldo de estoque por projeção do ledger (snapshot vira cache) | divergência real de `EstoqueAtual` entre nó e hub no piloto |
| B7 | State machines venda/compra/financeiro + imutabilidade pós-fiscal (Codex §7.5) | após Gate 5, como programa de domínio |
| B8 | Transferência como saga 2 pernas (Codex §7.5) | reativação do fluxo de transferência multi-filial |
| B9 | Produto: template global × override por filial (Codex P0.9) | dono decidir que preço/fiscal PODEM divergir por filial |
| B10 | `Configuracao` por-filial `(FilialId, Chave)` | depois do Gate 5 (hoje: não replica) |
| B11 | Merge de cadastro duplicado por chave natural (tela de reconciliação) | primeiro caso real no piloto |
| B12 | Multi-tenant (`TenantId`) | decisão de consolidar clientes num banco (hoje: 1 banco/cliente) |
| B13 | Timestamps UTC em todo o app | junto com B1, se vier |

## 7. Limpeza documental (fazer na fase 0, é barato)

1. `sync.md`: resolver a contradição interna da lápide local (linhas ~88-99 dizem CORRIGIDO; ~146-158 descrevem como pendente — a versão corrigida é a verdadeira, apagar a seção obsoleta); atualizar lista de endpoints (faltam `/quarentena`, `/quarentena/reprocessar`, `/resetar-recebimento`).
2. Mover `multi-filial.md` para `ContextDocuments/archive/` com cabeçalho **HISTÓRICO — NÃO IMPLEMENTAR** (modelo de Id antigo contradiz o vigente).
3. `AppDbContext.cs:2072-2075`: apagar o comentário morto "TUDO replica".
4. `PROJECT_CONTEXT.md`: marcar como snapshot histórico.
5. Este plano passa a ser o documento de execução; `synAteAqui.md` e a orientação do Codex ficam como referência de diagnóstico.

## 7b. CHECKLIST DE DEPLOY DA FASE 0 (ação do dono — ANTES de este trabalho chegar ao main/Railway)

O `appsettings.json` versionado não carrega mais segredo nem identidade. A Railway hoje roda SEM env
vars (usa o appsettings) — sem os passos abaixo, o próximo deploy do main **não sobe** (fail-fast):

1. Na Railway (serviço do ErpPharma), criar as env vars:
   - `ConnectionStrings__DefaultConnection` = connection string do PG da Railway (aproveitar e trocar a senha do banco — a antiga está no histórico do git);
   - `JwtSettings__SecretKey` = valor NOVO (≥ 32 chars; gerar: `[Convert]::ToBase64String((1..48 | %% { Get-Random -Max 256 }))` no PowerShell);
   - `SistemaKey` = valor NOVO — e o MESMO valor no env do **ZulexAdmin**, senão a senha do dia diverge; se houver loja com sync ligado, atualizar o `appsettings.Development.json`/env da loja JUNTO (transporte usa login SISTEMA até a fase 1);
   - `No__Modo` = `Hub` (o `No__Codigo` pode ser omitido — Hub assume 0).
2. No PC local (dev): o `appsettings.Development.json` (gitignored) já foi ajustado com os valores atuais + `No:Modo=Edge`. Quando rotacionar a `SistemaKey` na Railway, trocar aqui também.
3. Rodar a suíte: `dotnet test backend/ZulexPharma.Tests` (usa o Postgres local; cria/derruba `zulexpharma_test`). Esperado HOJE: 12 verdes + 7 vermelhos (os vermelhos são os bugs ativos — viram verdes nas fases 1-4).

## 8. Protocolo de execução (para o Opus)

- **Trabalhe na branch `dev-pc1`** (ou branch de feature a partir dela, a critério do dono). Commits: `tipo(escopo): resumo` em PT com corpo explicando o PORQUÊ; um commit por preocupação; NUNCA `git add .` (stage por caminho explícito).
- **Test-first nas correções estruturais:** o teste vermelho da fase 0 vira verde no mesmo commit do fix. Postgres REAL nos testes; EF InMemory proibido neste subsistema.
- **Re-verifique cada `arquivo:linha` deste plano antes de editar** — o código anda.
- **Revisão adversarial ao fim de cada fase** (subagente: "prove que isso perde/duplica/vaza dado") — foi o único método que pegou os 4 desenhos ruins anteriores.
- Comentários de código em PT sem acentos (estilo do repo); docs podem ter acentos.
- Migrations: `dotnet ef migrations add X -p ZulexPharma.Infrastructure -s ZulexPharma.API`. Migration aplicada não se edita — fix retroativo é migration nova.
- Em dúvida entre "fazer bonito" e "fazer verificável": verificável. Este subsistema pune intuição.
