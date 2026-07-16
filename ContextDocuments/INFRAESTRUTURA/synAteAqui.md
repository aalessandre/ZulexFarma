# synAteAqui — Replicação do ErpPharma: o retrato completo

> **O que é este documento.** Um retrato honesto do subsistema de replicação em **2026-07-16**: o que ele
> precisa fazer, o que a infra é, o que está pronto, o que está quebrado, **os erros que eu (Claude) cometi**
> e as dificuldades reais do caminho. Serve de base para a revisão do zero.
>
> **Regra deste documento: fato verificado > memória.** Tudo aqui foi conferido contra o código (com
> `arquivo:linha`) por agentes independentes, **porque minha memória errou várias vezes** — inclusive
> afirmando coisas que este documento agora desmente. Onde algo não foi confirmado, está escrito
> `NÃO CONFIRMADO`.
>
> Companheiro obrigatório: `sync.md` (mesma pasta) tem o detalhe operacional de cada mecanismo.

---

## 1. Objetivos da replicação

O ErpPharma é um ERP de farmácia **multi-filial**. Cada loja roda seu próprio backend + Postgres, e existe
uma **central na nuvem (Railway)**. A replicação precisa garantir:

| # | Objetivo | Por quê |
|---|---|---|
| 1 | **Nenhuma operação se perde** | venda, estoque e fiscal não podem sumir |
| 2 | **Loja opera offline** e reconcilia ao voltar | internet de farmácia cai |
| 3 | **Escrita em qualquer ponto** (multi-master) | a nuvem é gravável (venda móvel fora da loja) |
| 4 | **Convergência**: todos os nós chegam ao mesmo estado | sem divergência permanente |
| 5 | **Isolamento por filial**: dado de uma loja não vaza pra outra | privacidade/escopo |
| 6 | **Número fiscal nunca duplica** | NFC-e é sequência legal, por nó |
| 7 | 🔴 **NADA PODE FICAR SILENCIOSO** | *regra inegociável do dono*: existe um painel de sincronismo e ele **precisa refletir o real**. Op descartada/perdida sem sinal é o pior defeito possível — pior que travar. |

O objetivo 7 é o critério de aceite de qualquer mudança aqui. **Falha ruidosa > perda silenciosa.**

---

## 2. O cenário

- **Não usa replicação nativa do Postgres** — é um mecanismo caseiro (outbox + fila + apply), por decisão.
- Multi-master **bidirecional**: a central é gravável, não é só um espelho.
- **1 banco por tenant/cliente**, VPS compartilhada.
- Hoje **não há produção multi-nó**: é fase de desenvolvimento/validação. Isso é o que torna aceitável ter
  bugs conhecidos abertos — mas eles **precisam** cair antes de ligar a segunda loja.

### Decisões travadas (do dono)

| Tema | Decisão |
|---|---|
| Código visível | único por nó |
| Contador fiscal | **pinado ao nó dono** (cada nó numera o seu; não replica) |
| Conflito | **LWW por linha** (last-writer-wins) |
| Topologia | uma VPS compartilhada + **banco por cliente** |
| Faixa de Id | 1e9 decimal por nó |
| Nomenclatura | `FilialOrigemId` → **`NoOrigemId`** (eixo do nó) |
| Escopo local | config explícita `No:Filiais` |
| FilialDono das filhas | **derivar do pai** no enfileiramento |

---

## 3. Infraestrutura definida (verificada)

### 3.1 Topologia: estrela

```
   Loja 1 (No:Codigo=1)  ─┐
   Loja 2 (No:Codigo=2)  ─┼─► PUSH  /api/sync/enviar  ─► CENTRAL (Railway, No:Codigo=0)
   Loja 3 (No:Codigo=3)  ─┘◄─ PULL  /api/sync/receber ◄─  aplica + RE-ENFILEIRA p/ os outros
```

- Nó só fala com `Sync:UrlCentral`. **Não há peer-to-peer nem descoberta** (`SyncBackgroundService.cs:193,218`).
- A central **aplica** a op no banco consolidado **e re-enfileira** na própria `SyncFila` preservando o
  `NoOrigemId` da origem — é assim que os outros nós recebem (`SyncController.cs:103-138`).

> ⚠️ **A central não rodar o loop é ACIDENTE, não design.** Não existe flag "sou a central". O loop sai cedo
> em `SyncBackgroundService.cs:70-74` porque `_noCodigo == 0` cai na **mesma condição de "desabilitado"** —
> e o log ainda **mente** ("configure No:Codigo") justamente quando `0` é o valor **correto**. Se alguém
> setar `No:Codigo≠0` no hub, o loop liga e o hub tenta empurrar **pra si mesmo**.

### 3.2 Identidade do nó

- Vem de `No:Codigo` (fallback legado `Filial:Codigo`), reparseada em **5 lugares** sem opção tipada:
  `Program.cs:269`, `SyncBackgroundService.cs:50`, `SyncController.cs:25`, `AppDbContext.cs:24`,
  `ProdutoService.cs:24`.
- `Program.cs:269-283` tem fail-fast (throw + `Log.Fatal`, antes dos hosted services).

> 🔴 **O fail-fast é ILUSÃO** (e é trabalho meu da Fase 0 que reportei como pronto). O `appsettings.json:38-40`
> versionado **já traz `"No": { "Codigo": 0 }`** → a chave **nunca está ausente** → o caminho de fail-fast
> praticamente não dispara. **O modo de falha real é o oposto do prometido:** uma loja que esqueça de setar
> `No__Codigo` **sobe silenciosamente como Nó 0 (hub)** — sem faixa de Id, sem filial seedada e **sem sync**
> (cai no `return` do loop). Falha silenciosa — viola o objetivo 7.

### 3.3 Faixa de Id (evita colisão de PK entre nós)

- `ID_RANGE_PER_FILIAL = 1_000_000_000` (`DatabaseSeeder.cs:14`); offset = `noCodigo × 1e9`
  (`:406`); `ALTER ... RESTART WITH offset+1` **só se `maxId < offset`** (nunca reduz) (`:433-437`).
- Roda **só se `noCodigo > 0`** (`:47-48`) → **o hub fica em 1..999.999.999 por omissão**, não por decisão.
- Teto real ≈ 9,2 bi de nós (o "99 filiais" do doc antigo é arbitrário).

> 🔴 **Dois nós com o MESMO código = corrupção silenciosa, sem validação nenhuma.** Falha em dois eixos:
> 1. **Colisão de PK** — mesmo offset → Ids sobrepostos. O apply é idempotente **por Id**, então o "I" do
>    gêmeo B para o Id 1.000.000.042 encontra o registro **diferente** do gêmeo A e vira idempotente/stale
>    ou UPDATE cego: **dois registros de negócio distintos fundidos num só.**
> 2. **Anti-eco cega os gêmeos** — `f.NoOrigemId != filialId` (`SyncController.cs:169`): cada um enxerga o
>    outro como "eu mesmo" e **nunca recebe as ops dele**. Divergência permanente, painel limpo.

### 3.4 O que o seeder faz diferente no Nó 0

| Ação | Nó 0 (hub) | Nó > 0 (loja) | Linha |
|---|---|---|---|
| `ConfigurarSequences` (faixa de Id) | não | sim | `:47-48` |
| Seed da Filial própria | não | `Id = noCodigo` | `:57-58` |
| Seed do usuário admin | não | `admin{noCodigo}` | `:110-111` |
| `EnfileirarSeedParaSync` | não | sim | `:301-302` |
| `CriarSequencesCodigo` | **sim** | sim | `:144` |

Razão (documentada em `:53-56`): o hub recebe as filiais das lojas via sync; seedar "Matriz" Id=1 colidiria
com a "Filial 01" do nó 1.

### 3.5 Config, auth e deploy

- **Auth nó→central**: login `SISTEMA` com senha derivada de `SHA256(data + SistemaKey)`, token em cache
  (`SyncBackgroundService.cs`). **Sem `SistemaKey` o nó não autentica** (sem fallback compartilhado).
- **Migrations rodam no STARTUP** (fail-fast). Boot demorado = migration rodando.
- **Chaves**: `No:Codigo`, `No:Filiais` (escopo por-filial do PULL; vazio = só GLOBAL), `Sync:Habilitado`,
  `Sync:UrlCentral`, `Sync:IntervaloSegundos` (30), `Sync:LoteTamanho` (100), `Sync:AceitarCertInseguro`
  (false = TLS validado), `Sync:BackoffMaxSegundos` (300), `Sync:LimpezaDias`.
- **Relógio**: `DataHoraHelper.Agora()` = **hora-de-parede de Brasília** (`ConvertTimeFromUtc`, Kind=Unspecified),
  **não UTC**. Todo o LWW depende do relógio dos nós. (Brasil sem horário de verão desde 2019 → consistente,
  mas **skew entre nós afeta o LWW diretamente**.)

---

## 4. Como funciona hoje (verificado, com arquivo:linha)

### 4.1 Os dois eixos — a distinção central

| | `NoOrigemId` | `FilialDonoId` |
|---|---|---|
| Onde mora | `BaseEntity` + `SyncFila` | **só `SyncFila`** (não existe em BaseEntity) |
| Vem de | config `No:Codigo` (servidor) | usuário logado; ou **derivado do pai** (`_derivacaoFilialDono`, recursivo até prof. 6, memoizado por SaveChanges) |
| Serve p/ | anti-eco, faixa de Id, desempate do LWW | escopo por-filial do PULL |
| Semântica | **ONDE a op nasceu** | **DE QUEM é o dado** (null = GLOBAL) |

> ⚠️ `AppDbContext.cs:2281`: se `No:Codigo = 0`, o `NoOrigemId` do registro passa a vir do **claim `filialId`
> do JWT** — **misturando os dois eixos** exatamente no hub.

### 4.2 Mecanismos

| Mecanismo | Onde | Estado |
|---|---|---|
| **Outbox atômico** | `AppDbContext.SaveChangesAsync:2270-2392` | ✅ 2 saves, **1 commit** → sem estado fantasma. Abre tx própria só se não houver ambiente |
| **AplicandoSync short-circuit** | `:2272-2278` | ✅ ao aplicar sync não recarimba `AtualizadoEm`, não gera Codigo/op |
| **Codigo** | `GerarCodigo:2394-2427` | ✅ `nextval` (não segura lock) + **SAVEPOINT** no fallback |
| **LWW** | `AplicarUpdateComLww:275-287` | ✅ `AtualizadoEm ?? CriadoEm`; empate → **maior `NoOrigemId` vence** |
| **LWW delete vs update** | `SyncApplicator.cs:51-53` | ✅ update local mais novo vence o delete |
| **Lápide (tombstone)** | `SyncApplicator:58` (remoto) + `AppDbContext:2352` (**local**) | ✅ anti-ressurreição nos dois lados; retenção 90d |
| **Quarentena** | `:316-352`, `:370-406` | ✅ upsert por (Tabela,RegistroId,Operacao); tetos **5** (conflito) e **240** (`PrecisaRetry`); drena no PULL e a cada `/enviar` |
| **Idempotência `OpUid`** | outbox `:2364` + `SyncController:103-138` | ✅ Guid imutável; índice parcial único; reenvio não duplica |
| **Escopo por-filial** | `SyncController:170` | ✅ `FilialDonoId IS NULL OR = ANY(filiais)`; lista vazia = só GLOBAL |
| **Cascata → D + lápide** | `CarregarFilhosCascataAsync` | ✅ **verificado em runtime** (fecha 24 call-sites) |
| **23503 → PrecisaRetry** | `AplicarCabecalhoAsync` | ✅ ordem é transitória, não erro |
| **TLS + backoff** | `SyncBackgroundService` | ✅ TLS validado por padrão; backoff exponencial + jitter |

### 4.3 PUSH vs PULL — assimetria que importa

| | PUSH (nó→central) | PULL (central→nó) |
|---|---|---|
| Seleção | **flag** `Where(!Enviado)` | **cursor** `Id > ultimoId` |
| Imune ao gap? | ✅ **SIM** — linha que commita tarde continua `!Enviado` e é pega no ciclo seguinte | ❌ **NÃO** — ver §6.1 |

**O gap é só no PULL. O conserto é localizado no `/receber`.**

---

## 5. Como deve ser (o alvo)

1. **Entrega provada, não inferida.** O cursor precisa refletir **ordem de visibilidade** (commit), não ordem
   de alocação de Id. → `SeqEntrega` por publicador (§6.1).
2. **Conjunto autoritativo nos agregados.** O JSON do pai deve carregar o grafo completo **ou declarar o que
   não carregou** — para reconciliar filhos sem apagar dado legítimo (§6.2).
3. **Registro explícito de nós.** Sem ele não há retenção segura, nem detecção de nó gêmeo, nem onboarding.
4. **Retenção segura** da fila central (hoje cresce sem teto) — depende de 1 e 3.
5. **Fail-fast de verdade** na identidade do nó (§3.2).
6. **Painel = verdade.** Toda decisão importante (descarte, compactação, gap, nó parado) precisa aparecer.

---

## 6. O que falta (as duas peças estruturais)

### 6.1 🔴 GAP DO CURSOR — perda silenciosa sob escrita concorrente (ATIVO)

**Mecanismo:** `SyncFila.Id` é alocado no **INSERT**, mas só fica visível no **COMMIT**. O PULL serve
`Id > ultimoId` ordenado por `Id` e o ponteiro avança para o **maior Id servido**
(`SyncBackgroundService.cs:271`).

**Cenário real (verificado):**
```
T0      push do nó 1 (100 ops) → aloca Ids 101-200, tx AINDA ABERTA
T0+100  push do nó 2 (5 ops)   → aloca 201-205 → COMMITA em T0+150
T0+200  nó 3 puxa → enxerga só 201-205 → ponteiro = 205
T0+300  push do nó 1 COMMITA (101-200)
depois  nó 3 pede Id > 205 → 101-200 NUNCA são entregues. Perda permanente e silenciosa.
```

> 🔴 **CORREÇÃO IMPORTANTE À NOTA QUE EU MESMO DEIXEI NO CÓDIGO.** `SyncController.cs:184-186` e o `sync.md`
> prescrevem a cura como *"horizonte de estabilidade via `pg_snapshot_xmin`"* — **sem dizer que o cursor
> precisa mudar junto**. Quem seguir a nota como está **reproduz o bug**. A formulação correta é:
>
> **O horizonte de estabilidade só fecha o gap SE o cursor for o xid. Manter o cursor em `Id` não fecha,
> porque as duas ordens são independentes** (o xid nasce na 1ª escrita da tx; o `Id` sai no insert do outbox,
> depois — e neste código a distância é **estrutural**: `:2324` salva o negócio (xid nasce aí) e só `:2377`
> insere o outbox, com serialização JSON e upsert de lápide no meio).
>
> Contraexemplo formal (perde **mesmo com o horizonte**):
> ```
> tx S: 1ª escrita → xid 500        tx R: 1ª escrita → xid 501
> tx R: outbox → Id 101             tx S: outbox → Id 102
> tx S commita                      tx R lenta, commita depois
> pull: h = pg_snapshot_xmin = 501 (R ainda roda)
>   → serve S (xmin 500 < 501, Id 102); cursor = 102
> R commita com Id 101 < 102  →  PERDIDO
> ```
> Basta `xid_S < xid_R` **e** `Id_R < Id_S`.

**As duas curas que funcionam:**

| | Como | Custo |
|---|---|---|
| **A. Cursor = xid** | coluna `TxId xid8 DEFAULT pg_current_xact_id()` (xid8 = 64 bits, **imune a wraparound**; o `xmin` de sistema é 32 bits e **não** serve); serve `WHERE TxId >= cursor AND TxId < pg_snapshot_xmin(pg_current_snapshot())` | **transação longa PARALISA a fila inteira** — inclusive tx alheia ao sync (relatório, autovacuum) |
| **B. Publicador + `SeqEntrega`** ⭐ | `SeqEntrega bigint` atribuída por `nextval` **só a linhas já commitadas** (o publicador não enxerga as não-commitadas), sob advisory lock; cursor passa a ser `SeqEntrega` | linha que commita tarde só pega um número **maior** na rodada seguinte — **transação longa não trava nada** |

**B é estritamente superior** — e o `sync.md` atual aceita um custo ("transação longa trava o sync") que
**não precisa ser aceito**. Buracos na numeração são inofensivos (`nextval` é não-transacional; com cursor
`>`, o que importa é monotonicidade + ausência de chegada tardia — ambas garantidas).

**Obstáculo prático:** a central **não roda background loop**, então não há onde pendurar o publicador.
Caminho viável sem infra nova: numerar de forma **oportunista** no início do `/receber` (e/ou fim do
`/enviar`) sob `pg_try_advisory_xact_lock` — quem pega o lock numera; quem não pega serve o que já está
numerado.

### 6.2 🔴 RECONCILIAÇÃO DE FILHOS — duplicação em edição rotineira (ATIVO)

`UpsertFilhosPocoAsync` (`SyncApplicator.cs:183-213`) é **append/update-only — não existe caminho de delete**.
E os services fazem **`RemoveRange` de todos os filhos + re-add** com **Ids novos da faixa daquele nó**
(ex.: `ClienteService.cs:182-197`). Resultado: **editar um cliente já duplica o conjunto no par** (o destino
fica com os Ids velhos **+** os novos); e se o par depois editar o mesmo registro, o JSON dele leva os órfãos
de volta = **ressurreição efetiva** do filho que o outro apagou.

**Agregados afetados:** Cliente, Convenio, Promocao, HierarquiaDesconto, HierarquiaComissao, Adquirente,
CampanhaFidelidade, Venda.

**Por que a v1 (delete-missing) foi revertida:** assumia JSON completo, mas vários caminhos salvam o pai
**sem `Include`** dos filhos (`FinalizarAsync` não carrega `Itens.Descontos`; `CancelarAsync` sem Include;
fallback excluir→desativar) → apagaria dado **legítimo**.

**Cura certa:** garantir o grafo completo na serialização (forçar `Load` das coleções POCO no outbox) **OU**
o outbox **nular** coleção não-`IsLoaded` → o JSON omite a chave → o applicator reconcilia **só** as coleções
com chave **presente** no JSON (distingue "carregada e vazia" = apaga, de "não carregada" = preserva).

### 6.3 Outras pendências

| Item | Gravidade |
|---|---|
| **Retenção da fila central**: linhas de redistribuição ficam `Enviado=false` pra sempre → `/limpar` apaga **zero** na central → **cresce sem teto**. **Monitorar disco da Railway.** Depende de §6.1 + registro de nós | alta (custo, não perda) |
| **Formato do `Codigo` é AMBÍGUO**: `$"{_noCodigo}{ultimo}"` — concatenação **sem separador** → nó 1+seq 11 e nó 11+seq 1 dão **"111"**. Eu tinha classificado como "cosmético". **Não é.** | média |
| **Nó gêmeo** (§3.3) — sem validação | alta |
| **Fail-fast ilusório** (§3.2) | alta |
| **Purga de lápides da central** só roda dentro do `/enviar` → central sem PUSH não faz faxina | baixa |
| `SyncFila.CriadoEm` é `timestamptz` mas `SyncQuarentena.OpCriadoEm/CriadoEm` são `timestamp` — mistura real, com `EnableLegacyTimestampBehavior=true`. Efeito exato **NÃO CONFIRMADO** | a investigar |
| Op descartada por LWW (`Stale`) é **100% silenciosa** (`RegistrarRecebido` só roda em `Aplicado`) | média (viola obj. 7) |
| `Configuracao`/`LogAcao`/`LogErro` são GLOBAIS mas talvez devessem ser por-filial (exige coluna `FilialId`) | a decidir |

---

## 7. Os erros que eu cometi

Sejamos diretos: **4 desenhos meus foram derrubados por revisão adversarial nesta sessão**, e várias
premissas minhas eram falsas. O padrão importa mais que os itens.

| # | Erro | O que aconteceu | Como foi pego |
|---|---|---|---|
| 1 | **Reconciliação de filhos v1** | delete-missing assumindo JSON completo → apagaria os descontos de **toda venda finalizada** e os itens de **toda venda cancelada** | revisão, antes do commit |
| 2 | **Retenção por MIN(cursor)** | premissa falsa em 2 eixos: nó sem cursor é **invisível** pro MIN (1ª pull pós-deploy apagaria o backlog do nó desligado) e o cursor **não prova consumo** | revisão (13 achados, 6 críticos) |
| 3 | **Varredura por tempo** | escolhi por ser "estritamente aditiva" — **e não é**: ressuscita registro apagado localmente. Além disso a cobertura era **inversamente correlacionada com o risco** (`CriadoEm` é o relógio da **origem**) | revisão (23 achados, 5 críticos) |
| 4 | **`pg_snapshot_xmin`** | recomendei ao dono como cura do gap. **Não fecha** com cursor em `Id` — e **deixei essa prescrição incompleta no código e no doc**, onde induziria o próximo a repetir o bug | eu mesmo, ao ir implementar (e refinado pelo levantamento) |

**Premissas que assumi e eram falsas:**

- ❌ *"O ErpPharma força todas as FKs para `Restrict` por política global"* — **isso é o ZulexSac**. Confundi
  projetos. Aqui **todo `OnDelete(Cascade)` vale**, o que **amplia** o escopo do bug de cascata.
- ❌ *"Cursor gap-proof ✅ já feito"* — **reportei isso ao dono como pronto e era falso**. O ponteiro-avança-sempre
  + quarentena cobrem falha de **aplicação**, não o gap de **visibilidade**.
- ❌ *"O fallback do `GerarCodigo` se auto-cura"* (comentário que eu escrevi) — não se curava: statement que
  falha **aborta a tx inteira** no Postgres.
- ❌ *"A lápide fecha a ressurreição"* (Fase 1) — fechava **metade**: só o delete remoto.
- ❌ *"Formato do Código é cosmético"* — é **ambíguo**.
- ❌ *"Suas 225 regras de permissão estão com a sintaxe errada"* — **estavam certas**; o problema era meu
  encadeamento com `&&`.

**A lição central:** toda vez que raciocinei *"isso claramente funciona"*, **estava errado**. Este subsistema
pune intuição. O que funcionou, sem exceção: **revisão adversarial** e **teste em runtime**.

---

## 8. As dificuldades enfrentadas

### Do domínio (as que custaram caro)

- **"Build verde" não significa nada aqui.** Os 4 desenhos derrubados **compilavam limpos**. Só o harness em
  runtime provou alguma coisa.
- **Semântica de transação do Postgres**: statement que falha **envenena a tx inteira** → tornou o fallback do
  `GerarCodigo` uma ilusão, e fez meu teste falhar com um `25P02` enigmático.
- **Ordem de visibilidade ≠ ordem de alocação** — a raiz do gap, e o motivo do `xmin` sozinho não servir.
- **Relógio da origem vs. da central** — matou a varredura. E o LWW inteiro roda em hora de parede.
- **Cascata é invisível pro EF** — o banco apaga e o ChangeTracker nunca sabe.
- **POCO vs. BaseEntity**: dois mecanismos de replicação no mesmo agregado, com furos diferentes.
- **Acoplamentos por acidente**: a faxina de lápides "funcionava" só porque nascia dentro do `Receber`; a
  central "não roda o loop" só porque `0` cai no `if` de desabilitado.

### Do ambiente

- **Working tree compartilhado** com outras sessões de IA → nunca `git add .`; stage por caminho explícito.
- **Banco local do dono estava atrás** (sem `seq_codigo_*`, sem `OpUid`) — o backend local não subia desde a
  Fase 2. Fez o teste falhar de forma confusa até a causa aparecer. Migrations aplicadas em 16/07.
- **Harness contra o EF**: conflito 9.0.1 vs 9.0.4 (`CS1705`) → exige refs **explícitas** 9.0.4; e sem
  `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` (igual `Program.cs:9`) o harness
  **diverge do runtime e mente**.
- **Não existe suíte de testes do sync** — todo harness foi construído do zero.
- **Doc mentindo**: `sync.md` prometia "limpeza automática" inexistente; o slider de limpeza era **controle
  morto** (ninguém lia a config); nomes obsoletos (`FilialOrigemId`). **Confiar no doc levava a erro** — por
  isso a regra do topo deste arquivo.
- **Prompts de permissão**: ~1000 cliques do dono, porque meus comandos encadeados com `&&` furavam as regras
  (cada subcomando precisa casar sozinho). Resolvido com `defaultMode: bypassPermissions`.

---

## 9. Como testar (o que funcionou)

**Harness em `scratchpad/cascadetest`** — receita que funciona:

1. Refs **explícitas** do EF **9.0.4** (`EntityFrameworkCore` + `.Relational` + `Npgsql.EntityFrameworkCore.PostgreSQL`), senão `CS1705`.
2. `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` — igual ao `Program.cs:9`.
3. `new AppDbContext(opts, null, config)` — `http` e `config` são **opcionais**.
4. **Transação + rollback no `finally`** → não suja o banco de dev.
5. **`ChangeTracker.Clear()`** antes de testar cascata — senão o filho fica rastreado, o EF cascateia sozinho
   e **o teste passa por engano**.

**Exemplo real (o que provou a cascata):** criar `Ncm` + 3 `NcmFederal` → `ChangeTracker.Clear()` → confirmar
**0 filhos rastreados** → `Remove(pai)` **sem Include** → esperar **4 ops** (`NcmFederais D` ×3 + `Ncms D` ×1)
e **4 lápides**. Antes do fix: 1 e 1.

---

## 10. Recomendação para a revisão do zero

Ordem sugerida, do mais estrutural ao menos:

1. **Gap do cursor** → publicador + `SeqEntrega` (§6.1, opção B). **Exige teste de concorrência real.**
2. **Reconciliação de filhos** (§6.2) — é o que corrompe dado em uso **rotineiro**.
3. **Registro de nós esperados** → destrava retenção, nó gêmeo e onboarding.
4. **Fail-fast real** + **formato do Código** (§3.2, §6.3).
5. Só então **religar a retenção**.

**E o meta-conselho:** dado o placar de 4 desenhos meus derrubados, **não aceite "compilou, sobe"** neste
subsistema — meu incluso. O que pegou todos os defeitos foi revisão adversarial + teste em runtime.

---

*Última atualização: 2026-07-16. Estado do código: `main` @ `8327e53`. Fases 0→4b + lápide local + cascata
no ar e verificadas; §6.1 e §6.2 abertas.*
