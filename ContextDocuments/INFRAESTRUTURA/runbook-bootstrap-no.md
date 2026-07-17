# Runbook — Bootstrap de nó novo (fase 5 do plano de replicação)

> Como colocar um nó (loja) novo na replicação **sem gap e sem re-pull do mundo**: fotografia do hub
> na janela de manutenção + cursor cravado na marca. Vale também para REBOOTSTRAP (nó que caiu no
> status `RebootstrapNecessario`: geração mudou, escopo ampliado ou cursor abaixo da marca de
> retenção). Pré-requisito: fases 0-5 deployadas no hub e no nó.

## Passo a passo

1. **Cadastrar o nó na central** (admin): `POST /api/sync/nos { noCodigo, nome, filiais: [...] }`.
   Guarde a **chave** (aparece UMA vez). `filiais` é obrigatório se o nó atende filiais.
2. **Janela de manutenção** (fora de pico): parar os writers relevantes se possível; garantir que os
   edges existentes fizeram push (fila deles vazia ou aceitável).
   ⚠️ **REBOOTSTRAP (nó que JÁ operava): obrigatório drenar o outbox dele ANTES do restore** — o
   restore sobre banco vazio destrói toda `SyncFila` local `Enviado=false` (vendas não enviadas
   somem do universo). Confirme `PendentesEnvio = 0` no painel do nó; se o transporte estiver
   travado (409/GEMEO), exporte antes: `COPY (SELECT * FROM "SyncFila" WHERE NOT "Enviado") TO ...`
   e reaplique via `/api/sync/enviar` na central.
3. **Watermark**: `GET /api/sync/bootstrap-info` na central → devolve `{ marca, geracao }`.
   (O endpoint numera tudo que está commitado antes de responder.)
4. **Dump da central** (Railway → local), EXCLUINDO infra local:
   ```bash
   pg_dump "$PGCONN_HUB" -Fc \
     --exclude-table-data '"SyncFila"' \
     --exclude-table-data '"SyncQuarentena"' \
     --exclude-table-data '"SyncEstadoLocal"' \
     --exclude-table-data '"SyncNos"' \
     --exclude-table-data '"SyncNoFiliais"' \
     -f bootstrap.dump
   ```
   **NÃO** excluir `SyncTombstones` (as lápides são estado anti-ressurreição e VIAJAM).
5. **Restore no nó novo** (banco vazio): `pg_restore -d "$PGCONN_NO" --no-owner bootstrap.dump`.
6. **Config do nó**: `No__Modo=Edge`, `No__Codigo=<codigo>`, `Sync:UrlCentral`, `Sync:NoChave=<chave>`
   e **`Sync:Habilitado=false`** (⚠️ NÃO ligar ainda: o primeiro pull sairia com cursor 0 antes do
   passo 7 — re-pull do mundo, ou 409 fatal se a central já compactou). Subir o backend — o seeder
   detecta banco populado e NÃO re-seeda; as sequences de faixa são reposicionadas pro código do nó
   (pelo MAX **dentro da faixa**, imune às linhas de outros nós que vieram no dump).
6b. **Contador de Código** (só REBOOTSTRAP de nó que já emitia códigos): o dump traz as
   `seq_codigo_*` na posição da CENTRAL — re-semear pelo maior sufixo já emitido POR ESTE nó, senão
   a próxima venda colide no índice único `(Codigo, NoOrigemId)`:
   ```sql
   DO $$ DECLARE s record; m bigint; BEGIN
     FOR s IN SELECT sequencename FROM pg_sequences WHERE sequencename LIKE 'seq_codigo_%' LOOP
       EXECUTE format('SELECT COALESCE(MAX(split_part("Codigo",''-'',2)::bigint), 0) FROM %I WHERE "Codigo" LIKE ''<NO>-%%''',
                      replace(s.sequencename, 'seq_codigo_', '')) INTO m;
       IF m > 0 THEN PERFORM setval(s.sequencename::regclass, m, true); END IF;
     END LOOP; END $$;   -- trocar <NO> pelo codigo do no
   ```
7. **Cravar o cursor** (no NÓ, admin): `POST /api/sync/cursor { cursor: <marca>, geracao: "<geracao>" }`.
   Depois setar `Sync:Habilitado=true` e **reiniciar o serviço** (o loop parado não religa sozinho).
8. **Ativar**: na central, `PUT /api/sync/nos/<codigo> { status: "Ativo" }` (se o cadastro não ativou
   no primeiro handshake). O transporte liga no próximo ciclo.
9. **Validar**: comparar `GET /api/sync/checksum?tabela=X` (e `&filialId=` nas por-filial) entre
   central e nó para as tabelas críticas (Produtos, ProdutosDados, Pessoas, Vendas…). Contagem+hash
   iguais = bootstrap íntegro.

## Regras que este runbook honra (não pular)

- **Mudança de escopo** (adicionar filial a nó que já puxou) → o `PUT /nos` já marca
  `RebootstrapNecessario` sozinho: refazer este runbook (o histórico da filial nova está atrás do cursor).
- **Hub restaurado de backup** → os edges param sozinhos com `REBOOTSTRAP` (regressão da marca).
  Refazer este runbook em cada edge. `resetar-recebimento` (re-pull completo) SÓ funciona se a
  central NUNCA compactou a fila (marca de retenção zero) — senão o pull leva 409 e o bootstrap é o
  único caminho.
- **Retenção**: o nó só volta a segurar a fila central quando `Ativo`. Nó parado além do SLA
  (`Sync:SlaOfflineDias`, default 30) aparece com alerta no `GET /nos` — a decisão de rebaixar pra
  `RebootstrapNecessario` é SEMPRE humana.
- **Frota mista de seeds** (TiposPagamento/IcmsUf com Ids de faixa em nós antigos): o bootstrap
  resolve naturalmente — o nó novo herda as linhas do hub. Nós ANTIGOS com Ids de faixa continuam
  funcionando localmente; alinhá-los = rebootstrap deles também (ou remap manual das FKs).
