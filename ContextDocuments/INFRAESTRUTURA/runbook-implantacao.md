# Runbook de IMPLANTAÇÃO — ErpPharma (nuvem + PCs locais)

> Como um implantador coloca um cliente no ar do zero: escolher a topologia, montar a nuvem, montar
> os PCs locais, o que configurar e como validar. Complementa o `runbook-bootstrap-no.md` (que cobre
> SÓ o passo de anexar um nó a um hub que já existe). Escrito a partir da verdade-de-código levantada
> em 18/07/2026 (workflow de 6 lentes) — cada afirmação tem `arquivo:linha`.
>
> **Estado do subsistema:** fases 0-6 codadas, suíte 81/81 verde. Falta o piloto real (Gate 5) e as
> pendências da §5c do `plano-correcao-replicacao-2026-07-17.md`. Ainda NÃO há UI para provisionar nó
> (é 100% por API) nem agendador de retenção — ver §9.

---

## 0. Conceitos que decidem tudo

### 0.1 São TRÊS modos de deployment, não dois (`No:Modo`)

`No:Modo` é a chave-mestra, **obrigatória e sem default** — se faltar, o boot cai ruidosamente
(`NoDeployment.Resolver`, `NoDeployment.cs:25-78`). Isso é de propósito: o modo de falha antigo era uma
loja subir SILENCIOSAMENTE como hub (sem faixa de Id, sem filial, sem sync) por esquecer a env var.

| Modo | Quem é | `No:Codigo` | Captura outbox? | Roda loop de sync? | Semeia filial/admin? |
|---|---|---|---|---|---|
| **Hub** | Central consolidadora na nuvem | **0** (ou ausente) | Sim | **Não** (é reativo: serve `/api/sync`) | **NÃO** — é ponto de consolidação, não loja |
| **StandaloneCloud** | Loja que opera 100% na nuvem, SEM replicação | **≥1 único** | **Não** (SyncFila não cresce) | Não | Sim (filial + `admin{N}` + faixa de Id) |
| **Edge** | Servidor de loja (PC local) que replica com o hub | **≥1 único** | Sim | **Sim** (push+pull) | Sim |

Fonte: enum e regras em `NoDeployment.cs:6-14,42-73`; seed do hub pulando filial/admin em
`DatabaseSeeder.cs:69-70,122-123`; captura por modo em `AppDbContext.cs:29`; early-return do loop por
modo em `SyncBackgroundService.cs:74-97`.

**Consequência prática nº1:** o cliente cloud-only dos seus primeiros clientes é **StandaloneCloud**,
não Hub. O Hub é uma peça de infraestrutura (consolida vários edges), não uma loja usável — ele nem
tem filial nem usuário admin (login só via SISTEMA/senha-do-dia).

### 0.2 Faixa de Id por nó

`No:Codigo` define a faixa de Id do nó: `offset = codigo × 1.000.000.000` (`DatabaseSeeder.cs:14,443`).
Toda PK criada naquele nó nasce na faixa dele → sem colisão entre nós, PK preservada ao replicar.
**O código nunca é reutilizado** (mesmo de loja desativada) — reciclar corromperia a faixa.

### 0.3 O segredo compartilhado (`SistemaKey`)

`SistemaKey` gera a senha-do-dia do login SISTEMA (`SenhaDiaService.cs:19` = SHA256(data+chave)).
**Tem que ser byte-a-byte idêntica** à `SistemaKey` do ZulexAdmin (que é quem exibe a senha do dia),
senão o suporte não loga como SISTEMA. Atenção: **não é fail-fast no boot** — se faltar, o app sobe e
só quebra na hora do login SISTEMA (`AuthService.cs:194`).

### 0.4 1 banco por tenant

Cada cliente (rede/farmácia) = seu próprio banco. A topologia é decidida **por cliente**. Um cliente
multi-nó = 1 banco Hub (nuvem) + N bancos Edge (PCs locais). Um cliente cloud-only = 1 banco
StandaloneCloud.

---

## 1. DECISÃO DE TOPOLOGIA (faça isto ANTES de vender/implantar o cliente)

Esta é a decisão mais importante do runbook, e o código **não** oferece conversão automática entre
topologias. Escolha por cliente:

### Caminho A — Cliente cloud-only que vai continuar só nuvem → **StandaloneCloud**
Mais simples: 1 deployment, opera na hora, SyncFila não cresce. É o padrão para seus primeiros
clientes. Segue a §3.

### Caminho B — Cliente que já nasce sabendo que terá PC local (ou vários) → **Hub (nó 0) + Edge(s)**
Monte o Hub na nuvem (§2) e cada loja como Edge (§4). Adicionar um PC local depois é só mais um edge —
sem migração. Custo: 2+ deployments/bancos por cliente.

### O risco que você levantou: "e se um cloud-only precisar de local depois?"
É possível — a intenção de design é **StandaloneCloud(nó N) → Edge(nó N)**, preservando a faixa de Id
(`NoDeployment.cs:67`). MAS **não é automatizado**: exige levantar um Hub (nó 0) novo, semear o hub com
os dados da loja, virar a loja para Edge e fazer bootstrap. É uma **migração manual, planejada e
testada** — não um flip de env var. Faça junto comigo numa sessão dedicada, nunca no improviso.

> **Recomendação:** para os primeiros clientes (explicitamente cloud-only), vá de **StandaloneCloud**
> — é o mais simples e o que menos código exercita. Só escolha o Caminho B para um cliente específico
> se você já tem forte indício de que ele vai querer PC local em semanas. Rodar um Hub "à toa"
> (sem edges) tem um custo: se o hub originar escritas de negócio, a SyncFila dele cresce sem teto
> porque a retenção não apaga nada sem um edge Ativo ackando (`SyncController.cs:906-912`).

---

## 2. Checklist — montar o HUB (nuvem / Railway)

> Só para o Caminho B (clientes com edges). Cliente cloud-only puro pula direto pra §3.

Deploy é 100% Docker (`backend/Dockerfile`, escuta em `$PORT`); **não há `appsettings.Production.json`
nem `railway.json`** — toda config mora nas env vars do painel da Railway (`appsettings.json` tem só
placeholders vazios).

**Env vars do Hub** (convenção .NET: `Secao:Chave` no appsettings vira `Secao__Chave` em env var):

```
No__Modo=Hub
No__Codigo=0                         # ou omitir; Hub é sempre 0
# NÃO setar Sync__Habilitado=true    # Hub + Habilitado=true DERRUBA o boot (NoDeployment.cs:47)
ConnectionStrings__DefaultConnection=Host=...;Port=5432;Database=...;Username=...;Password=...;
JwtSettings__SecretKey=<>=32 chars, valor NOVO>     # assina TAMBÉM o token de nó (SyncController.cs:107)
SistemaKey=<valor NOVO, IDÊNTICO ao do ZulexAdmin>
Sync__SlaOfflineDias=30              # opcional; só o hub lê (alerta de nó offline no painel)
```

Passos:
1. Criar o Postgres (Railway) e o serviço do backend (Dockerfile). Setar as env vars acima.
2. Subir. **Migrations rodam no startup** (`DatabaseSeeder.cs:22`, fail-fast) — boot demorado = migration
   rodando. O log deve dizer `No:Modo=Hub | No:Codigo=0` (`Program.cs:298`). Se cair com throw de config,
   é env var faltando/inválida — corrija antes de seguir.
3. O hub sobe **sem filial e sem admin** (`DatabaseSeeder.cs:70,123`) — isso é esperado. O acesso
   administrativo ao hub é via **login SISTEMA** (usuário `SISTEMA` + senha-do-dia do ZulexAdmin),
   que carrega a claim `isAdmin=True` e satisfaz a policy `SyncAdmin` (`AuthService.cs:215`).
4. **Frontend:** a URL da central é **compilada no bundle** (`environment.prod.ts:3`), não é env var —
   se o hub mudar de URL, tem que **rebuildar o frontend**. Garanta que aponta pro hub certo antes de publicar.

---

## 3. Checklist — montar uma LOJA CLOUD-ONLY (StandaloneCloud)

O caso dos seus primeiros clientes. Sobe como loja completa (filial + `admin{N}` + faixa de Id),
não captura outbox, não roda sync.

**Env vars:**
```
No__Modo=StandaloneCloud
No__Codigo=<inteiro >=1 único por cliente>
# NÃO setar Sync__Habilitado=true    # StandaloneCloud + Habilitado=true DERRUBA o boot (NoDeployment.cs:69)
ConnectionStrings__DefaultConnection=...
JwtSettings__SecretKey=<>=32 chars>
SistemaKey=<IDÊNTICO ao do ZulexAdmin>
```

Passos:
1. Criar banco + serviço na nuvem, setar env vars, subir.
2. Confirmar no log: `No:Modo=StandaloneCloud | No:Codigo=N` e a linha `modo StandaloneCloud — sem
   replicacao (captura de outbox desligada, transporte nao roda)` (`SyncBackgroundService.cs:82`).
3. O seeder cria **Filial N**, usuário **`admin{N}`** com senha inicial **`admin123`** (`DatabaseSeeder.cs:129`)
   — **TROCAR na primeira entrada** — e a faixa de Id do nó. A loja opera normal a partir daí; a
   SyncFila não cresce (`AppDbContext.cs:29`).
4. Ajustar `CorsOrigins` (appsettings/env) para a origem do frontend do cliente.

---

## 4. Checklist — montar um PC LOCAL (Edge) + bootstrap

> Um edge só entra depois que um **Hub** existe (§2). Este é o resumo; o passo-a-passo detalhado do
> bootstrap (dump/restore/cursor) está no `runbook-bootstrap-no.md`.

### 4.1 No HUB — cadastrar o nó (login admin/SISTEMA, via API — NÃO há tela)
```
POST /api/sync/nos   { "noCodigo": <>=1 único>, "nome": "...", "filiais": [<ids>] }
```
- Devolve a **chave EM CLARO uma única vez** (`SyncController.cs:255`) — anote na hora; no banco fica só
  o hash. Se perder, rotacione com `POST /api/sync/nos/{n}/nova-chave`.
- `filiais` é **obrigatório se a loja atende filiais específicas** — sem isso o pull do edge leva
  `422 EscopoNaoConfigurado` (`SyncController.cs:559`).

### 4.2 No EDGE — env vars
```
No__Modo=Edge
No__Codigo=<mesmo do cadastro, >=1 único>
Sync__UrlCentral=<URL do hub>              # obrigatório quando Habilitado=true
Sync__NoChave=<chave do passo 4.1>         # ATENÇÃO: NÃO tem placeholder em nenhum appsettings — fácil esquecer
Sync__Habilitado=false                     # FALSE até cravar o cursor (passo 4.4); ligar antes = re-pull do mundo ou 409 fatal
ConnectionStrings__DefaultConnection=<PG LOCAL>
JwtSettings__SecretKey=<>=32 chars>        # para os usuários humanos do próprio PC
SistemaKey=<IDÊNTICO ao do ZulexAdmin>
# Opcionais:
No__Filiais=<csv de ids>                   # se a loja atende filiais específicas — alinhar com o cadastro (senão 422)
Sync__IntervaloSegundos=30                 # default 30
Sync__LoteTamanho=100                      # default 100, clamp 1..500
Sync__BackoffMaxSegundos=300               # default 300
Sync__AceitarCertInseguro=false            # true SÓ para hub com cert self-signed (nunca em prod real)
```

### 4.3 Bootstrap (nó novo com dados vindos do hub) — resumo do `runbook-bootstrap-no.md`
1. Subir o edge com `Sync__Habilitado=false`.
2. No hub: `GET /api/sync/bootstrap-info` → `{ marca, geracao }` (numera tudo commitado antes de responder).
3. `pg_dump` do hub **excluindo infra local** (`SyncFila`, `SyncQuarentena`, `SyncEstadoLocal`, `SyncNos`,
   `SyncNoFiliais`) — **manter `SyncTombstones`**. `pg_restore` no banco do edge (vazio).
4. O seeder detecta banco populado (guards `AnyAsync`) e não re-seeda; as sequences são reposicionadas
   pela **faixa do nó** (MAX-dentro-da-faixa, `DatabaseSeeder.cs:510`, imune às linhas de outros nós no dump).

### 4.4 Cravar cursor e ligar
5. No EDGE (login admin): `POST /api/sync/cursor { "cursor": <marca>, "geracao": "<geracao>" }`.
6. Setar `Sync__Habilitado=true` e **REINICIAR o serviço** — o loop que fez early-return **não religa
   sozinho** (`SyncBackgroundService.cs`; risco clássico de esquecer).
7. No HUB: `PUT /api/sync/nos/{codigo} { "status": "Ativo" }` (o data plane `/enviar` e `/receber` só
   respondem a nó `Ativo`). O primeiro handshake já crava o `InstanciaUid` (anti-gêmeo) e auto-ativa
   `Provisionando → Ativo` (`SyncNoAuth.cs:56`).

---

## 5. Validação (todo nó, pós-implantação)

Reconciliar hub × nó por **checksum** (via API — sem tela):
```
GET /api/sync/checksum?tabela=<Tabela>[&filialId=<id>]
```
Compare `count` + `hash` (md5 de `Id:AtualizadoEm`) entre hub e nó nas tabelas críticas: **Produtos,
ProdutosDados, Pessoas, Vendas**. Iguais = íntegro (`SyncController.cs:207`).

No dia-a-dia, a tela `/erp/sync` (menu "Sincronismo", **logado como admin**) mostra o status do
**próprio** nó: cards (Serviço Ativo/Parado, Pendentes, Falhas, Quarentena), grid da fila e da
quarentena com "Reprocessar", e as ações Buscar / Forçar Envio / Resetar recebimento / Limpar antigos.
⚠️ Usuário não-admin abre a tela e vê **tudo zerado sem aviso** (as chamadas tomam 403 e o componente
engole o erro, `sync.component.ts:87`) — não confunda com "sync parado".

---

## 6. Operação contínua

- **Retenção da fila do hub é MANUAL** — **não existe agendador** que chame `POST /api/sync/limpar`
  (`SyncController.cs:879`; confirmado: nenhum Hangfire/cron interno). **Monte um cron externo** (ex.:
  scheduler da Railway, ou um job que faz POST autenticado fora de pico), senão a SyncFila do hub
  cresce sem teto. A retenção só compacta até o `MIN(UltimoAckSeq)` dos nós **Ativos**; nó Ativo que
  nunca puxou (ack 0) segura a fila inteira (fail-closed proposital).
- **Cadência do edge:** ciclo a cada `Sync:IntervaloSegundos` (30s), push depois pull; em falha,
  backoff exponencial + jitter até `Sync:BackoffMaxSegundos` (300s) (`SyncBackgroundService.cs:49,179`).
- **Status que PARAM o loop e exigem ação humana** (o loop não religa sozinho, precisa restart):
  `REBOOTSTRAP` (cursor abaixo da marca de compactação / geração mudou / regressão de marca), `GEMEO`
  (dois nós com o mesmo `No:Codigo`), `CONFIG` (falta `Sync:NoChave`), `AUTH` (chave rejeitada).
- **Nó offline/atrasado:** hoje só via `GET /api/sync/nos` (traz `atrasoSeq` e `alertaSla`) — a tela
  ainda não tem a seção "Nós" (pendente Gate 5, §9).

---

## 7. Evolução SEM quebrar cliente cloud-only (o seu medo, respondido)

Investiguei os dois mecanismos que você temia e a conclusão honesta é: **nenhum dos dois consegue
quebrar um cliente cloud-only especificamente.**

- **SyncRegistry.ValidarModelo (fail-closed no boot)** valida o **modelo de código** (`IModel`), não os
  dados (`SyncRegistry.cs:139`). É determinístico por versão de código: ou falha em TODO ambiente
  (dev, staging, prod, cloud-only) igual, ou não falha em nenhum. Não discrimina cloud-only.
- **A "geração"** é um UUID criado uma vez no hub e comparado **só por edges** (`SyncController.cs:50`).
  Cloud-only (StandaloneCloud) nem cria; hub sem edges cria mas nunca compara. Sem efeito. E **não há
  operação que "incremente a geração"** de propósito — ela só muda se o `SyncEstadoLocal` do hub for
  recriado (restore de backup). A premissa de "bump de geração quebra cliente" não existe no código.

**O ÚNICO vetor real que quebra um cloud-only diferente do teste é a MIGRATION no startup**
(`DatabaseSeeder.cs:22`, fail-fast, data-dependente):
- Migration **pesada** → boot lento proporcional ao volume de dados **daquele** cliente.
- Migration que **assume dados limpos** (ex.: índice único novo sobre dados que já têm duplicata) →
  **mata o boot** só no cliente cujos dados violam a regra, mesmo que o ambiente de teste (com outros
  dados) tenha subido. Precedente real do próprio subsistema: o índice único `(Codigo, NoOrigemId)`
  deu "boot-morto confirmado no banco de dev" e precisou re-codificar duplicatas NA migration.

**Disciplina de migration (a regra de ouro para não quebrar cloud-only existente):**
1. Toda migration de constraint (índice único, NOT NULL, FK) **testada contra uma cópia dos DADOS REAIS**
   do cliente, não só banco limpo.
2. Índice em tabela quente → pré-criar via psql **fora de pico** (`CREATE INDEX CONCURRENTLY IF NOT
   EXISTS`) para o boot virar no-op.
3. Migration de dados (backfill) desenhada idempotente e defensiva (dado sujo existe).
4. Deploy de mudança de modelo em cliente cloud-only = mesma cautela de qualquer migration em produção;
   o risco NÃO está no sync, está no schema.

---

## 8. Ambiente de teste (pc1 + pc2 + nuvem) — o piloto do Gate 5

Você quer montar pc1, pc2 e nuvem. Isso é **um tenant** com topologia Hub + 2 Edges:

| Máquina | Modo | `No:Codigo` |
|---|---|---|
| Nuvem (Railway) | Hub | 0 |
| pc1 | Edge | 1 |
| pc2 | Edge | 2 |

Receita:
1. **Nuvem:** montar o Hub (§2). Confirmar boot `No:Modo=Hub`.
2. **pc1:** cadastrar nó 1 no hub (§4.1), montar o Edge (§4.2), bootstrap (§4.3-4.4), ativar, validar
   por checksum (§5).
3. **pc2:** idem com nó 2.
4. **Caos (Gate 5, 48h)** — o plano pede exercitar: matar o processo em cada fronteira de commit/ACK,
   derrubar a rede entre edge e hub, adiantar o relógio de um nó. Ao final: **checksum zerado** em todas
   as tabelas do escopo, o painel refletiu cada anomalia, e a fila central estável (retenção funcionando).
5. **Só depois do piloto** um segundo nó REAL entra em produção (regra do plano). E o bloqueador de
   produção que ainda falta é a §5c item 2 do plano (transações únicas de compra e venda) — ver
   `plano-correcao-replicacao-2026-07-17.md`.

---

## 9. Gaps conhecidos que o implantador PRECISA saber

Levantados na pesquisa; alguns são pendências do Gate 5, outros são tarefas do dono.

**Sem UI (provisionamento é 100% por API/curl com JWT de admin):** cadastrar nó, rotacionar chave,
resetar instância, bootstrap-info, cravar cursor, checksum, e a seção "Nós" (status/atraso/SLA por nó
remoto) **não têm tela** (`sync.component.ts` só consome status/fila/quarentena do próprio nó). O
backend já expõe tudo — é o upgrade visual pendente do Gate 5 (plano §FASE 5 item 5).

**Perda por LWW é invisível no painel:** descartes por conflito gravam `SyncQuarentena` com
`Motivo=Stale, Resolvido=true`, e a tela só lista `Resolvido=false` (`SyncController.cs:736`) — não há
filtro "Descartados (LWW)". Para auditar perda por conflito hoje, consultar o banco direto.

**Retenção sem agendador** (§6) — precisa de cron externo.

**Config sem placeholder no appsettings versionado** (fácil esquecer): `Sync:NoChave` (crítico — sem
ela o edge fica em `CONFIG` em silêncio), `Sync:BackoffMaxSegundos`, `Sync:SlaOfflineDias`, `No:Filiais`,
`Sync:AceitarCertInseguro`. **Não existe um appsettings de referência** com todas as chaves por modo —
esta tabela (§2-§4) é a referência.

**`Sync:LimpezaDias` do appsettings é config MORTA** — a janela de retenção real vem da config de
**banco** `sync.limpeza.dias` ou do `?dias=` (`SyncController.cs:888`). Setar no env não tem efeito.

**Tarefas do DONO ainda pendentes (não são do implantador):**
- **Rotacionar os segredos** (connection string, `JwtSettings:SecretKey`, `SistemaKey`): os valores
  atuais são os antigos, comprometidos no histórico do git (Gate 0 "batido com ressalva"). Gerar novos
  na Railway + PG antes de clientes reais.
- **Criar as env vars na Railway**: hoje o serviço roda sem elas (usava o appsettings). O próximo deploy
  do `main` **não sobe** até as env vars existirem (fail-fast é o comportamento correto).
- `SistemaKey` idêntica entre ErpPharma (todas as instâncias) e ZulexAdmin — não há verificação
  automática; divergência = suporte não loga como SISTEMA.

---

## 10. Ordem de decisão recomendada (resumo executivo)

1. **Rotacionar segredos + criar env vars na Railway** (dono) — sem isso nada sobe.
2. **Por cliente, decidir a topologia** (§1): cloud-only puro = StandaloneCloud; vai ter local = Hub+Edge.
3. **Montar o ambiente de teste pc1+pc2+nuvem** (§8) e rodar o caos do Gate 5.
4. **Resolver o bloqueador de produção** (transações de compra e venda, plano §5c item 2) antes do 2º nó real.
5. Só então implantar clientes com replicação real. Clientes cloud-only (StandaloneCloud) podem entrar
   antes disso — não exercitam a maquinaria de sync — desde que a disciplina de migration (§7) seja seguida.
