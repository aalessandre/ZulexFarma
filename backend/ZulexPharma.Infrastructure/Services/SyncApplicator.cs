using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using System.Text.Json;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Entities.SelfCheckout;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>Resultado da aplicacao de uma operacao de sync (Fase 1 — corretude de conflito).</summary>
public enum ResultadoSync
{
    Aplicado,          // aplicado com sucesso
    Idempotente,       // ja' estava no estado alvo (reenvio/mesma versao) — nada a fazer
    Stale,             // LWW: a versao que chegou e' MAIS VELHA que a local — descartada de proposito
    PrecisaRetry,      // dependencia faltando (FK ainda nao replicada) — reprocessar depois
    Conflito,          // chave unica de OUTRA origem (23505) sem merge automatico — quarentena
    TipoDesconhecido,  // tabela fora do dicionario — quarentena/log (nunca silencioso)
    // Fase 3:
    ColisaoIdentidade, // PK igual mas SyncGuid diferente em linha JA' SINCronizada = no gemeo/faixa errada — quarentena, NUNCA SetValues
    RelogioSuspeito,   // timestamp da op > agora+5min: aplicar venceria o LWW por horas — segura na quarentena (teto alto: resolve quando o tempo alcanca)
    RecriacaoSemGrafo, // U recriaria agregado sobre lapide mas o JSON OMITE colecoes: recriar sem filhos = dado fiscal capenga — quarentena
    // Fase 4:
    LedgerImutavel     // U/D remoto em tabela de LEDGER (MovimentoEstoque/Lote): historia nao se reescreve — quarentena
}

/// <summary>
/// Lógica compartilhada para aplicar operações de sync no banco de dados.
/// Usada pelo SyncBackgroundService (filiais) e SyncController (Railway).
/// </summary>
public static class SyncApplicator
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Aplica uma operação (INSERT/UPDATE/DELETE) no banco de dados.
    /// Retorna true se aplicada com sucesso, false se ignorada (ex: já existe / não existe).
    /// Lança exceção em caso de erro.
    /// </summary>
    public static async Task<ResultadoSync> AplicarOperacaoAsync(
        AppDbContext db, string tabela, string operacao, long registroId,
        string? dadosJson, DateTime opCriadoEm, long noOrigemId, CancellationToken ct = default)
    {
        var tipo = ResolverTipo(tabela);
        if (tipo == null) return ResultadoSync.TipoDesconhecido;

        // FASE 4 — LEDGER (fatos imutaveis): estoque e' historia, nao snapshot. Aceita SO' 'I'
        // (reentrega e' dedup por Id no fluxo normal); 'U'/'D' remoto = reescrita de historico via
        // LWW (dois incrementos concorrentes perderiam um) -> quarentena LedgerImutavel. Correcao
        // legitima e' movimento de AJUSTE novo.
        if (SyncRegistry.TabelasLedger.Contains(tabela) && operacao != "I")
            return ResultadoSync.LedgerImutavel;

        BaseEntity? entidade = null;
        if (operacao is "I" or "U")
        {
            if (dadosJson == null) return ResultadoSync.Conflito;
            entidade = (BaseEntity?)JsonSerializer.Deserialize(dadosJson, tipo, _jsonOpts);
            if (entidade == null) return ResultadoSync.Conflito;
        }
        else if (operacao != "D") return ResultadoSync.Conflito;

        // FASE 3 — guard de relogio (decisao A2 do plano): op "do futuro" venceria o LWW por horas
        // contra qualquer edicao legitima. Segura na quarentena (RelogioSuspeito) ate' o tempo
        // alcancar ou o admin agir. 5min de tolerancia pra jitter normal de NTP.
        var tsOp = operacao == "D" ? opCriadoEm : (entidade!.AtualizadoEm ?? entidade.CriadoEm);
        if (tsOp > Domain.Helpers.DataHoraHelper.Agora().AddMinutes(5))
            return ResultadoSync.RelogioSuspeito;

        // FASE 3 — ATOMICIDADE (P0.5): cada op roda numa transacao com advisory lock por
        // (tabela, registro): dois applies concorrentes da MESMA linha serializam, e a decisao
        // linha×lapide (e cabecalho×filhos) e' um commit so'. O lock por hash cobre tambem o caso
        // linha-ausente (nao ha' row pra SELECT FOR UPDATE). SaveChanges internos usam savepoint
        // automatico do EF -> os catches de 23505/23503 continuam funcionando dentro da tx.
        var txPropria = db.Database.CurrentTransaction == null
            ? await db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({tabela + ":" + registroId}, 0))", ct);

            // FASE 3b (achado CRITICO C2 da revisao): o contexto e' COMPARTILHADO pelo lote e o
            // identity map do EF devolve a instancia JA' RASTREADA descartando os valores frescos do
            // banco — o LWW compararia contra versao STALE (escrita concorrente de outro request
            // ficava invisivel, e tx revertida deixava entidade suja envenenando o resto do lote).
            // Limpa o tracker POR OP (preserva SyncFila/SyncQuarentena — linhas de painel pendentes).
            LimparTrackerDaOp(db);

            var res = await AplicarNucleoAsync(db, tipo, tabela, operacao, registroId, entidade, dadosJson, opCriadoEm, noOrigemId, ct);

            if (txPropria != null) await txPropria.CommitAsync(ct);
            return res;
        }
        catch
        {
            if (txPropria != null) await txPropria.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (txPropria != null) await txPropria.DisposeAsync();
        }
    }

    private static async Task<ResultadoSync> AplicarNucleoAsync(
        AppDbContext db, Type tipo, string tabela, string operacao, long registroId,
        BaseEntity? entidade, string? dadosJson, DateTime opCriadoEm, long noOrigemId, CancellationToken ct)
    {
        if (operacao == "D")
        {
            var existente = await BuscarPorId(db, tipo, registroId);
            if (existente != null)
            {
                // COMPARADOR UNICO (fase 3): versao da LINHA (ts, escritor real) vs versao do DELETE.
                if (CompararVersao(TsDe(existente), EscritorDe(existente), opCriadoEm, noOrigemId) > 0)
                    return ResultadoSync.Stale; // linha local MAIS NOVA vence o delete
                db.Remove(existente);
                await db.SaveChangesAsync(ct);
            }
            // Crava a lapide (mesmo se ja' nao existia) — impede ressurreicao por INSERT/UPDATE velho.
            await RegistrarTombstoneAsync(db, tabela, registroId, opCriadoEm, noOrigemId, ct);
            return existente != null ? ResultadoSync.Aplicado : ResultadoSync.Idempotente;
        }

        // I/U — o cabecalho decide (LWW/lapide/insert); os filhos POCO sao reconciliados em seguida,
        // na MESMA transacao (fase 3: header+filhos atomicos — a janela de venda-sem-itens morreu).
        var ehAgregado = _tiposAgregado.Contains(tipo);
        var grafoCompleto = !ehAgregado || PayloadTemTodasAsColecoes(db, tipo, dadosJson!);
        var res = await AplicarCabecalhoAsync(db, tipo, tabela, operacao, registroId, entidade!, noOrigemId, grafoCompleto, ct);

        if (_tiposAgregado.Contains(tipo) && res is ResultadoSync.Aplicado or ResultadoSync.Idempotente)
        {
            // Copia FRESCA do grafo (o cabecalho estripa as navigations da instancia aplicada).
            var grafo = (BaseEntity)JsonSerializer.Deserialize(dadosJson!, tipo, _jsonOpts)!;
            var resFilhos = await ReconciliarFilhosPocoAsync(db, grafo, dadosJson!, ct);
            if (resFilhos == ResultadoSync.PrecisaRetry) return ResultadoSync.PrecisaRetry;
        }

        return res;
    }

    /// <summary>
    /// COMPARADOR UNICO de versao (fase 3): timestamp primeiro; empate -> ESCRITOR maior vence.
    /// Vale pra linha×linha, linha×lapide e lapide×linha — uma unica ordem total, todos os nos
    /// chegam a' mesma decisao independente da ordem de chegada.
    /// </summary>
    private static int CompararVersao(DateTime tsA, long escritorA, DateTime tsB, long escritorB)
        => tsA != tsB ? tsA.CompareTo(tsB) : escritorA.CompareTo(escritorB);

    private static DateTime TsDe(BaseEntity e) => e.AtualizadoEm ?? e.CriadoEm;

    /// <summary>Escritor da versao ATUAL da linha. Null (legado) cai pro criador; sem nada = 0 (hub).</summary>
    private static long EscritorDe(BaseEntity e) => e.AtualizadoPorNoId ?? e.NoOrigemId ?? 0;

    /// <summary>
    /// FASE 3b (C2): detacha TUDO exceto SyncFila/SyncQuarentena (linhas de painel/quarentena
    /// pendentes). Cada op comeca com tracker limpo -> BuscarPorId le o BANCO, nao a instancia
    /// stale da op anterior do lote.
    /// </summary>
    private static void LimparTrackerDaOp(AppDbContext db)
    {
        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is SyncFila or SyncQuarentena) continue;
            entry.State = EntityState.Detached;
        }
    }

    /// <summary>Fase 3b (A1): o JSON traz TODAS as colecoes POCO do agregado? (chave presente, mesmo vazia)</summary>
    private static bool PayloadTemTodasAsColecoes(AppDbContext db, Type tipo, string dadosJson)
    {
        var et = db.Model.FindEntityType(tipo);
        if (et == null) return true;
        using var doc = JsonDocument.Parse(dadosJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;
        foreach (var nav in et.GetNavigations().Where(n =>
                     n.IsCollection && !typeof(BaseEntity).IsAssignableFrom(n.TargetEntityType.ClrType)))
        {
            var chave = JsonNamingPolicy.CamelCase.ConvertName(nav.Name);
            if (!doc.RootElement.TryGetProperty(chave, out var v) || v.ValueKind != JsonValueKind.Array)
                return false;
        }
        return true;
    }

    // Agregados cujos filhos POCO (nao-BaseEntity) precisam viajar/aplicar junto do cabecalho.
    // Agregados cujos filhos POCO viajam no JSON do pai + upsert em cascata (mesmo mecanismo do 2d).
    // Cliente/Convenio/Promocao/Hierarquias/Adquirente/CampanhaFidelidade tem filhos POCO que NAO
    // replicavam (mesmo bug do VendaItem pre-2d). Filhos BaseEntity (ex.: CampanhaFidelidadeItem) sao
    // ignorados por ExtrairFilhosPoco (replicam sozinhos).
    private static readonly HashSet<Type> _tiposAgregado = new()
    {
        typeof(Venda), typeof(Cliente), typeof(Convenio), typeof(Promocao),
        typeof(HierarquiaDesconto), typeof(HierarquiaComissao), typeof(Adquirente),
        typeof(CampanhaFidelidade)
    };

    /// <summary>Agregados com filhos POCO — o outbox usa pra aplicar o contrato de colecao (fase 3).</summary>
    public static IReadOnlySet<Type> TiposAgregado => _tiposAgregado;

    /// <summary>Aplica so' o CABECALHO (LWW/insert/tombstone/23505). Os filhos POCO sao tratados a' parte.</summary>
    private static async Task<ResultadoSync> AplicarCabecalhoAsync(AppDbContext db, Type tipo, string tabela, string operacao, long registroId, BaseEntity entidade, long noOrigemId, bool grafoCompleto, CancellationToken ct)
    {
        // FASE 3: o escritor da versao que chega e' o NO DA OP (credencial no hub) — forca aqui
        // porque payload de no antigo pode nao trazer o campo.
        entidade.AtualizadoPorNoId = noOrigemId;

        var existentePorId = await BuscarPorId(db, tipo, registroId);
        if (existentePorId != null)
        {
            // FASE 3 — guard de IDENTIDADE (P0.10): mesma PK com SyncGuid DIFERENTE em linha que JA'
            // veio de sync = dois registros de negocio distintos no mesmo Id (no gemeo / faixa
            // errada). SetValues fundiria os dois em silencio — quarentena, decisao humana.
            // EXCECAO: linha local que NUNCA sincronizou (NoOrigemId null = seed/pre-sync) gerou o
            // proprio guid — adota o do par via LWW (senao TODO dado seedado travaria).
            if (entidade.SyncGuid != Guid.Empty && existentePorId.SyncGuid != entidade.SyncGuid)
            {
                if (existentePorId.NoOrigemId != null)
                    return ResultadoSync.ColisaoIdentidade;
                Log.Warning("Sync: adotando SyncGuid remoto em linha local pre-sync {Tabela}/{Id} ({Local} -> {Remoto})",
                    tabela, registroId, existentePorId.SyncGuid, entidade.SyncGuid);
            }
            return await AplicarUpdateComLww(db, existentePorId, entidade, noOrigemId, ct);
        }

        // Registro nao existe -> checar LAPIDE (anti-ressurreicao) com o COMPARADOR UNICO.
        var tomba = await BuscarTombstoneAsync(db, tabela, registroId, ct);
        var incomingTs = entidade.AtualizadoEm ?? entidade.CriadoEm;
        if (tomba != null && CompararVersao(incomingTs, noOrigemId, tomba.DeletadoEm, tomba.NoOrigemId) <= 0)
            return ResultadoSync.Stale; // edicao MAIS VELHA (ou empate) que a morte -> nao ressuscita

        // FASE 3b (achado ALTO A1): recriar AGREGADO sobre a lapide a partir de um "U" cujo JSON
        // OMITE colecoes (origem editou sem Include) criaria o pai SEM filhos — e o cascade do D ja'
        // levou os antigos: venda viva com zero itens, pra sempre. Quarentena (decisao humana /
        // estado completo), nunca upsert cego.
        if (tomba != null && operacao == "U" && !grafoCompleto)
            return ResultadoSync.RecriacaoSemGrafo;

        // FASE 3 (P0.6): INSERT novo, revivencia legitima (incoming MAIS NOVO que a lapide) OU um
        // "U" orfao. O U carrega o ESTADO COMPLETO (snapshot JSON), entao upsert e' correto — o
        // PrecisaRetry eterno de antes deixava a linha morta so' no no que recebeu D antes do U
        // (divergencia permanente provada em UdConvergenciaTests). FK faltando ainda cai no 23503
        // -> PrecisaRetry legitimo (dependencia, nao ordem de U/I).
        LimparNavigations(db, entidade);
        db.Add(entidade);
        db.Entry(entidade).Property("Id").IsTemporary = false;
        try
        {
            await db.SaveChangesAsync(ct);
            if (tomba != null) await RemoverTombstoneAsync(db, tomba.Id, ct); // reviveu -> limpa a lapide
            return ResultadoSync.Aplicado;
        }
        catch (DbUpdateException ex) when (EhUniqueViolation(ex))
        {
            // 23505: chave (CPF/CNPJ/login) ja' existe. Tenta merge por SyncGuid (identidade estavel);
            // se for de OUTRA origem (SyncGuid diferente) -> quarentena (merge e' decisao de negocio).
            Desanexar(db);
            var porGuid = await BuscarPorSyncGuid(db, tipo, entidade.SyncGuid);
            if (porGuid != null) return await AplicarUpdateComLww(db, porGuid, entidade, noOrigemId, ct);
            return ResultadoSync.Conflito;
        }
        catch (DbUpdateException ex) when (EhFkViolation(ex))
        {
            // 23503: o PAI ainda nao replicou (ordem) ou foi apagado no outro no (cascata). Dependencia
            // transitoria -> PrecisaRetry (teto 240 = a drenagem resolve quando o pai chegar), NAO "Erro"
            // (teto 5, que aposentava a op em 5 tentativas por um problema de ORDEM). Mesmo tratamento que
            // o UpsertFilhosPocoAsync ja' dava aos filhos POCO — o cabecalho estava de fora.
            Desanexar(db);
            return ResultadoSync.PrecisaRetry;
        }
    }

    /// <summary>
    /// FASE 3 — RECONCILIACAO DE FILHOS POCO com CONTRATO DE COLECAO (P0.7/synAteAqui §6.2):
    /// no JSON do pai, chave de colecao AUSENTE = "nao carregada na origem, PRESERVE os filhos
    /// locais"; chave PRESENTE (mesmo []) = AUTORITATIVA -> upsert por Id + DELETE dos ausentes.
    /// Isso mata a duplicacao dos services RemoveRange+re-add (os Ids velhos morrem no destino) SEM
    /// o risco da v1 revertida (apagar filhos legitimos quando o pai foi salvo sem Include — esses
    /// caminhos agora OMITEM a chave no outbox). Recursivo: Venda -> Itens (presente?) -> Descontos.
    /// </summary>
    private static async Task<ResultadoSync> ReconciliarFilhosPocoAsync(AppDbContext db, BaseEntity grafo, string dadosJson, CancellationToken ct)
    {
        // FASE 3b (achado CRITICO C1): os DELETE-MISSING (SQL cru) executam ANTES do SaveChanges dos
        // upserts. Se o insert falhar (FK ainda nao replicada), o savepoint automatico do EF desfaz
        // SO' os upserts — os DELETEs ficariam e a op commitaria com PrecisaRetry: venda sem NENHUM
        // item no destino ate' a drenagem (ou pra sempre, no teto). SAVEPOINT MANUAL cobre o bloco
        // inteiro: falhou -> volta tudo -> os filhos antigos continuam la' ate' o retry aplicar.
        var tx = db.Database.CurrentTransaction;
        if (tx != null) await tx.CreateSavepointAsync("reconcilia_filhos", ct);
        try
        {
            using var doc = JsonDocument.Parse(dadosJson);
            await ReconciliarColecoesAsync(db, grafo, doc.RootElement, ct);
            await db.SaveChangesAsync(ct); // um commit logico: EF ordena inserts por FK (item antes do desconto)
            if (tx != null) await tx.ReleaseSavepointAsync("reconcilia_filhos", ct);
            return ResultadoSync.Aplicado;
        }
        catch (DbUpdateException ex) when (EhFkViolation(ex))
        {
            // filho aponta pra FK que ainda nao replicou (ex.: ProdutoVariacao) = dependencia
            // transitoria -> PrecisaRetry (teto alto), nao "Erro" (teto 5).
            if (tx != null) await tx.RollbackToSavepointAsync("reconcilia_filhos", ct);
            Desanexar(db);
            return ResultadoSync.PrecisaRetry;
        }
    }

    private static async Task ReconciliarColecoesAsync(AppDbContext db, object entidade, JsonElement json, CancellationToken ct)
    {
        var et = db.Model.FindEntityType(entidade.GetType());
        if (et == null || json.ValueKind != JsonValueKind.Object) return;
        var paiId = Convert.ToInt64(entidade.GetType().GetProperty("Id")!.GetValue(entidade));

        foreach (var nav in et.GetNavigations().Where(n =>
                     n.IsCollection && !typeof(BaseEntity).IsAssignableFrom(n.TargetEntityType.ClrType)))
        {
            var chave = JsonNamingPolicy.CamelCase.ConvertName(nav.Name);
            if (!json.TryGetProperty(chave, out var arrJson) || arrJson.ValueKind != JsonValueKind.Array)
                continue; // AUSENTE no JSON = colecao nao carregada na origem -> preserva os filhos locais

            var tipoFilho = nav.TargetEntityType.ClrType;
            var fkProp = nav.ForeignKey.Properties[0].Name; // colunas deste projeto = nome da propriedade
            var tabelaFilho = nav.ForeignKey.DeclaringEntityType.GetTableName()!;
            var itens = ((System.Collections.IEnumerable)(nav.PropertyInfo!.GetValue(entidade)
                         ?? Array.Empty<object>())).Cast<object>().ToList();

            var idsPresentes = new List<long>();
            var indice = 0;
            foreach (var filho in itens)
            {
                var idFilho = Convert.ToInt64(tipoFilho.GetProperty("Id")!.GetValue(filho));
                if (idFilho > 0) idsPresentes.Add(idFilho);

                // NETOS primeiro (o strip abaixo perde as sub-colecoes) — pareado por indice com o
                // elemento do array JSON (mesma serializacao => mesma ordem).
                if (indice < arrJson.GetArrayLength())
                    await ReconciliarColecoesAsync(db, filho, arrJson[indice], ct);
                indice++;

                var existenteFilho = idFilho > 0 ? await db.FindAsync(tipoFilho, new object[] { idFilho }, ct) : null;
                LimparNavigations(db, filho);
                if (existenteFilho == null)
                {
                    db.Add(filho);
                    db.Entry(filho).Property("Id").IsTemporary = false; // preserva o Id da faixa-por-no
                }
                else
                {
                    db.Entry(existenteFilho).CurrentValues.SetValues(filho);
                }
            }

            // DELETE-MISSING: a colecao PRESENTE e' autoritativa — filho local fora do JSON morre
            // (era o orfao da duplicacao). Ids sao longs (sem injecao); FK cascade do banco leva os
            // netos dos removidos junto. Roda na MESMA transacao do cabecalho (atomico).
            var sqlIds = idsPresentes.Count > 0 ? string.Join(",", idsPresentes) : "0";
            await db.Database.ExecuteSqlRawAsync(
                $"DELETE FROM \"{tabelaFilho}\" WHERE \"{fkProp}\" = {paiId} AND \"Id\" NOT IN ({sqlIds})", ct);
        }
    }

    private static bool EhFkViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.ForeignKeyViolation;

    // ── Lapides (tombstone anti-ressurreicao) ──────────────────────────────
    // FASE 3 (decisao A7 do plano): lapide NAO e' mais purgada por idade — sao 4 campos sem PII e a
    // purga era exatamente o que permitia ressurreicao por no/backup mais atrasado que a retencao.
    // Lapide so' some ao REVIVER (RemoverTombstoneAsync) ou em decommission explicito (fase 5).

    private static async Task<SyncTombstone?> BuscarTombstoneAsync(AppDbContext db, string tabela, long registroId, CancellationToken ct)
        => await db.SyncTombstones.AsNoTracking().FirstOrDefaultAsync(x => x.Tabela == tabela && x.RegistroId == registroId, ct);

    private static async Task RemoverTombstoneAsync(AppDbContext db, long id, CancellationToken ct)
        => await db.SyncTombstones.Where(x => x.Id == id).ExecuteDeleteAsync(ct);

    /// <summary>Upsert da lapide por (Tabela,RegistroId), guardando a morte MAIS NOVA (comparador unico: ts, escritor).</summary>
    private static async Task RegistrarTombstoneAsync(AppDbContext db, string tabela, long registroId, DateTime deletadoEm, long noOrigemId, CancellationToken ct)
    {
        var t = await db.SyncTombstones.FirstOrDefaultAsync(x => x.Tabela == tabela && x.RegistroId == registroId, ct);
        if (t == null)
        {
            db.SyncTombstones.Add(new SyncTombstone { Tabela = tabela, RegistroId = registroId, DeletadoEm = deletadoEm, NoOrigemId = noOrigemId });
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (EhUniqueViolation(ex))
            {
                // corrida: outra aplicacao cravou a mesma lapide — recarrega e mantem a mais nova
                Desanexar(db);
                t = await db.SyncTombstones.FirstOrDefaultAsync(x => x.Tabela == tabela && x.RegistroId == registroId, ct);
                if (t != null && CompararVersao(deletadoEm, noOrigemId, t.DeletadoEm, t.NoOrigemId) > 0)
                { t.DeletadoEm = deletadoEm; t.NoOrigemId = noOrigemId; await db.SaveChangesAsync(ct); }
            }
        }
        else if (CompararVersao(deletadoEm, noOrigemId, t.DeletadoEm, t.NoOrigemId) > 0)
        {
            t.DeletadoEm = deletadoEm; t.NoOrigemId = noOrigemId;
            await db.SaveChangesAsync(ct);
        }
    }

    // RETENCAO/COMPACTACAO da fila central: TENTADA e REVERTIDA (2026-07-16) — ver revisao adversarial.
    // O desenho "apaga Id <= MIN(cursor dos nos)" parece seguro mas a PREMISSA E' FALSA:
    //  (1) o conjunto de nos era descoberto por OBSERVACAO (cursor criado no 1o pull) -> no que nunca pulou
    //      e' INVISIVEL pro MIN -> a 1a pull pos-deploy compactava contra UM no so' e apagava o backlog do
    //      no que estava desligado (perda silenciosa e PERMANENTE);
    //  (2) mais grave: o cursor NAO PROVA consumo. SyncFila.Id sai no INSERT mas so' fica visivel no COMMIT,
    //      entao um pull entre dois commits concorrentes pula um Id menor e o ponteiro passa por cima dele
    //      (GAP PRE-EXISTENTE — ver nota no /receber). Apagar com base nesse cursor transforma um gap
    //      recuperavel em perda definitiva.
    // Pre-requisito pra voltar: fechar o gap do cursor (horizonte de estabilidade via xmin/pg_snapshot ou
    // ack por no) + registro EXPLICITO de nos esperados (fail-closed) + marca-d'agua de compactacao exposta
    // no /status + deteccao de "no pediu abaixo da marca" (responder gap, nao lote parcial em silencio).
    // Fila crescendo e' custo; apagar op que ninguem recebeu e' perda de dado.

    /// <summary>
    /// UPDATE com Last-Writer-Wins pelo COMPARADOR UNICO (fase 3): AtualizadoEm primeiro; empate ->
    /// ESCRITOR REAL maior vence (op.NoOrigemId vs AtualizadoPorNoId da linha). O desempate antigo
    /// comparava criador==criador (imutavel) -> "primeiro que chegou vence" -> nos divergiam.
    /// </summary>
    private static async Task<ResultadoSync> AplicarUpdateComLww(AppDbContext db, BaseEntity existente, BaseEntity entidade, long noOrigemId, CancellationToken ct)
    {
        var cmp = CompararVersao(
            entidade.AtualizadoEm ?? entidade.CriadoEm, noOrigemId,
            TsDe(existente), EscritorDe(existente));
        if (cmp < 0) return ResultadoSync.Stale;
        if (cmp == 0) return ResultadoSync.Idempotente; // mesma versao (mesmo instante E mesmo escritor)
        LimparNavigations(db, entidade);
        entidade.Id = existente.Id; // preserva a PK local (evita erro de mudar chave; Ids batem no caso normal)
        db.Entry(existente).CurrentValues.SetValues(entidade);
        await db.SaveChangesAsync(ct);
        return ResultadoSync.Aplicado;
    }

    private static bool EhUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;

    /// <summary>
    /// Solta do ChangeTracker o que sujou num apply que falhou, pra nao poluir o proximo — MAS
    /// preserva os registros de PAINEL (SyncFila do "recebido") e a propria SyncQuarentena, senao
    /// um erro numa op posterior do lote sumia com a evidencia das anteriores (nada pode ser silencioso).
    /// </summary>
    public static void Desanexar(AppDbContext db)
    {
        foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged).ToList())
        {
            if (entry.Entity is SyncFila or SyncQuarentena) continue;
            entry.State = EntityState.Detached;
        }
    }

    public const int MaxTentativasQuarentena = 5;
    // PrecisaRetry (U chegou antes do I) e' TRANSITORIO — resolve quando o I chega. Cap ALTO pra
    // sobreviver a um no de origem atrasado (nao o teto baixo de conflito genuino), senao o U ficaria
    // preso pra sempre e divergiria em silencio.
    public const int MaxTentativasReordenacao = 240;

    /// <summary>
    /// Coloca (upsert por Tabela+RegistroId+Operacao) uma op que NAO pode ser aplicada na quarentena,
    /// pra retry — em vez de perde-la avancando o ponteiro. Sobe a contagem de tentativas.
    /// Fase 2: preserva OpUid/FilialDonoId pra re-enfileirar no hub quando o retry aplicar.
    /// </summary>
    public static async Task QuarentenarAsync(AppDbContext db, string tabela, string operacao, long registroId,
        string? dadosJson, DateTime opCriadoEm, long noOrigemId, string motivo, string? erro,
        Guid? opUid = null, long? filialDonoId = null, CancellationToken ct = default)
    {
        var q = await db.SyncQuarentena.FirstOrDefaultAsync(
            x => x.Tabela == tabela && x.RegistroId == registroId && x.Operacao == operacao, ct);
        if (q == null)
        {
            db.SyncQuarentena.Add(new SyncQuarentena
            {
                Tabela = tabela, Operacao = operacao, RegistroId = registroId, DadosJson = dadosJson,
                OpCriadoEm = opCriadoEm, NoOrigemId = noOrigemId, Motivo = motivo, Tentativas = 1,
                UltimoErro = Truncar(erro, 1000), Resolvido = false, OpUid = opUid, FilialDonoId = filialDonoId
            });
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (EhUniqueViolation(ex))
            {
                // corrida: outro push inseriu a mesma (Tabela,RegistroId,Operacao) — recarrega e atualiza
                Desanexar(db);
                q = await db.SyncQuarentena.FirstOrDefaultAsync(
                    x => x.Tabela == tabela && x.RegistroId == registroId && x.Operacao == operacao, ct);
                if (q != null)
                {
                    AtualizarQuarentena(q, dadosJson, opCriadoEm, motivo, erro, opUid, filialDonoId, noOrigemId);
                    await db.SaveChangesAsync(ct);
                }
            }
        }
        else
        {
            AtualizarQuarentena(q, dadosJson, opCriadoEm, motivo, erro, opUid, filialDonoId, noOrigemId);
            await db.SaveChangesAsync(ct);
        }
        Log.Warning("Sync quarentena [{Motivo}] {Tabela}/{Op} Id={Id}: {Erro}", motivo, tabela, operacao, registroId, erro ?? motivo);
    }

    private static void AtualizarQuarentena(SyncQuarentena q, string? dadosJson, DateTime opCriadoEm,
        string motivo, string? erro, Guid? opUid = null, long? filialDonoId = null, long? noOrigemId = null)
    {
        // FASE 2b (achado ALTO da revisao): a chave logica e' (Tabela,RegistroId,Operacao), entao ops
        // DIFERENTES do mesmo registro dividem a linha — e a quarentena e' a UNICA copia da op (o
        // "push honesto" ja' confirmou durabilidade ao edge). Sobrescrever pelo ULTIMO a chegar
        // deixava uma op MAIS VELHA substituir a mais nova: quando o retry aplicasse, o hub
        // distribuia a versao velha e a nova morria so' no no autor — divergencia permanente.
        // Regra: o payload guardado e' sempre o da op MAIS NOVA (LWW da propria quarentena).
        // FASE 3b (A2): o ESCRITOR acompanha o payload — senao o retry aplicava o payload do no 3
        // carimbando escritor 2, e o anti-eco escondia a op justamente do no errado.
        if (opCriadoEm >= q.OpCriadoEm)
        {
            q.DadosJson = dadosJson;
            q.OpCriadoEm = opCriadoEm;
            q.OpUid = opUid ?? q.OpUid;
            q.FilialDonoId = filialDonoId ?? q.FilialDonoId;
            if (noOrigemId != null) q.NoOrigemId = noOrigemId.Value;
        }
        q.Motivo = motivo;
        q.Tentativas += 1;
        q.UltimoErro = Truncar(erro, 1000);
        q.Resolvido = false;
        q.AtualizadoEm = Domain.Helpers.DataHoraHelper.Agora();
    }

    /// <summary>
    /// FASE 2 (objetivo 7 do plano: NADA silencioso): registra o DESCARTE por LWW (Stale) como
    /// trilha auditavel — linha de quarentena ja' Resolvida (sem retry), com o payload perdedor.
    /// Nao sobrescreve uma linha PENDENTE da mesma op (o retry pendente importa mais que a auditoria).
    /// </summary>
    public static async Task RegistrarDescarteLwwAsync(AppDbContext db, string tabela, string operacao, long registroId,
        string? dadosJson, DateTime opCriadoEm, long noOrigemId, Guid? opUid = null, long? filialDonoId = null,
        CancellationToken ct = default)
    {
        try
        {
            var q = await db.SyncQuarentena.FirstOrDefaultAsync(
                x => x.Tabela == tabela && x.RegistroId == registroId && x.Operacao == operacao, ct);
            if (q == null)
            {
                db.SyncQuarentena.Add(new SyncQuarentena
                {
                    Tabela = tabela, Operacao = operacao, RegistroId = registroId, DadosJson = dadosJson,
                    OpCriadoEm = opCriadoEm, NoOrigemId = noOrigemId, Motivo = "Stale", Tentativas = 0,
                    UltimoErro = "Descartada por LWW: a versao local e' mais nova que a recebida.",
                    Resolvido = true, OpUid = opUid, FilialDonoId = filialDonoId
                });
                await db.SaveChangesAsync(ct);
            }
            else if (q.Resolvido)
            {
                q.Motivo = "Stale";
                q.DadosJson = dadosJson;
                q.OpCriadoEm = opCriadoEm;
                q.UltimoErro = "Descartada por LWW: a versao local e' mais nova que a recebida.";
                q.AtualizadoEm = Domain.Helpers.DataHoraHelper.Agora();
                await db.SaveChangesAsync(ct);
            }
            // pendente: nao toca — o descarte fica no log; a linha de retry preserva o caso em aberto
        }
        catch (DbUpdateException ex) when (EhUniqueViolation(ex))
        {
            Desanexar(db); // corrida com outra trilha da mesma op — auditoria e' melhor-esforco
        }
        Log.Information("Sync: op DESCARTADA por LWW (Stale) {Tabela}/{Op} Id={Id} (origem {No})",
            tabela, operacao, registroId, noOrigemId);
    }

    /// <summary>
    /// Reprocessa itens da quarentena (retry) ate' sucesso (Resolvido) ou o teto de tentativas.
    /// Chamada a cada ciclo — resolve o caso "U chegou antes do I" quando o I finalmente chega.
    /// Retorna quantos foram resolvidos.
    /// FASE 2 — enfileirarAoResolver (SO' no hub): op quarentenada NAO foi enfileirada na chegada
    /// (conflito nao se espalha antes de resolvido — P1.4); quando o retry APLICA, a op entra na
    /// SyncFila aqui (com o OpUid/FilialDonoId preservados) e o publicador a numera em seguida.
    /// </summary>
    public static async Task<int> DrenarQuarentenaAsync(AppDbContext db, CancellationToken ct = default,
        bool enfileirarAoResolver = false)
    {
        var pendentes = await db.SyncQuarentena.AsNoTracking()
            // DonoNaoResolvido e' op LOCAL de saida (fase 4): reaplicar localmente = Idempotente ->
            // marcaria Resolvido e a op sumiria SEM replicar (perda silenciosa). Fica presa como
            // alarme ate' acao humana (corrigir derivacao + reprocessar).
            .Where(q => !q.Resolvido && q.Motivo != "DonoNaoResolvido" && (
                ((q.Motivo == "PrecisaRetry" || q.Motivo == "RelogioSuspeito") && q.Tentativas < MaxTentativasReordenacao) ||
                (q.Motivo != "PrecisaRetry" && q.Motivo != "RelogioSuspeito" && q.Tentativas < MaxTentativasQuarentena)))
            .OrderBy(q => q.Id).Take(200).ToListAsync(ct);
        if (pendentes.Count == 0) return 0;

        var resolvidos = 0;
        foreach (var q in pendentes)
        {
            ResultadoSync res;
            string? erro = null;
            try
            {
                res = await AplicarOperacaoAsync(db, q.Tabela, q.Operacao, q.RegistroId, q.DadosJson, q.OpCriadoEm, q.NoOrigemId, ct);
            }
            catch (Exception ex)
            {
                Desanexar(db);
                res = ResultadoSync.Conflito;
                erro = ex.Message;
            }

            var ok = res is ResultadoSync.Aplicado or ResultadoSync.Idempotente or ResultadoSync.Stale;
            if (ok) resolvidos++;

            // Hub: o retry APLICOU -> a op agora e' estado canonico e PRECISA ser distribuida.
            // (So' Aplicado: Idempotente/Stale = uma versao mais nova ja' passou e ja' foi distribuida.)
            if (ok && res == ResultadoSync.Aplicado && enfileirarAoResolver)
            {
                var jaEnfileirada = q.OpUid != null &&
                    await db.SyncFila.AnyAsync(f => f.OpUid == q.OpUid, ct);
                if (!jaEnfileirada)
                {
                    db.SyncFila.Add(new SyncFila
                    {
                        Tabela = q.Tabela, Operacao = q.Operacao, RegistroId = q.RegistroId,
                        DadosJson = q.DadosJson, NoOrigemId = q.NoOrigemId, FilialDonoId = q.FilialDonoId,
                        OpUid = q.OpUid, CriadoEm = q.OpCriadoEm, Enviado = false
                    });
                    await db.SaveChangesAsync(ct);
                }
            }

            // ExecuteUpdate (sem tracking) pra nao misturar com as entidades aplicadas no tracker.
            await db.SyncQuarentena.Where(x => x.Id == q.Id).ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Resolvido, ok)
                .SetProperty(x => x.Tentativas, x => x.Tentativas + (ok ? 0 : 1))
                .SetProperty(x => x.Motivo, res.ToString())
                .SetProperty(x => x.UltimoErro, Truncar(erro, 1000))
                .SetProperty(x => x.AtualizadoEm, Domain.Helpers.DataHoraHelper.Agora()), ct);
        }
        return resolvidos;
    }

    private static string? Truncar(string? s, int max) => s == null || s.Length <= max ? s : s.Substring(0, max);

    /// <summary>Busca por SyncGuid (identidade estavel cross-no) via reflexao no DbSet.</summary>
    public static async Task<BaseEntity?> BuscarPorSyncGuid(AppDbContext db, Type tipo, Guid syncGuid)
    {
        if (syncGuid == Guid.Empty) return null;
        var method = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!.MakeGenericMethod(tipo);
        var dbSet = (IQueryable<BaseEntity>)method.Invoke(db, null)!;
        return await dbSet.FirstOrDefaultAsync(e => e.SyncGuid == syncGuid);
    }

    /// <summary>
    /// Resolve tipo CLR a partir do nome da tabela.
    /// </summary>
    public static Type? ResolverTipo(string tabela) => _tiposPorTabela.GetValueOrDefault(tabela);

    /// <summary>
    /// Busca entidade por Id usando reflexão no DbSet correto.
    /// </summary>
    public static async Task<BaseEntity?> BuscarPorId(AppDbContext db, Type tipo, long id)
    {
        if (id <= 0) return null;
        var method = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!.MakeGenericMethod(tipo);
        var dbSet = (IQueryable<BaseEntity>)method.Invoke(db, null)!;
        return await dbSet.FirstOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>
    /// Anula todas as navigation properties para evitar tracking de grafo.
    /// </summary>
    public static void LimparNavigations(AppDbContext db, object entidade)
    {
        var entityType = db.Model.FindEntityType(entidade.GetType());
        if (entityType == null) return;

        foreach (var nav in entityType.GetNavigations())
        {
            var prop = nav.PropertyInfo;
            if (prop != null && prop.CanWrite)
                prop.SetValue(entidade, null);
        }

        foreach (var nav in entityType.GetSkipNavigations())
        {
            var prop = nav.PropertyInfo;
            if (prop != null && prop.CanWrite)
                prop.SetValue(entidade, null);
        }
    }

    /// <summary>
    /// Prioridades de dependência para ordenação no sync.
    /// INSERT/UPDATE: menor número primeiro (pais antes de filhos).
    /// DELETE: maior número primeiro (filhos antes de pais).
    /// </summary>
    public static int GetOrdemTabela(string tabela) => tabela switch
    {
        // Nível 0 — sem dependência
        "Filiais"                  => 0,
        "UsuariosGrupos"           => 0,
        "Ncms"                     => 0,

        // Nível 1 — depende apenas de nível 0
        "Pessoas"                  => 1,
        "UsuariosGruposPermissao"  => 1,
        "NcmFederais"              => 1,
        "NcmIcmsUfs"               => 1,
        "NcmStUfs"                 => 1,
        "Fabricantes"              => 1,
        "Prescritores"             => 1,
        "PlanosContas"             => 1,
        "TiposPagamento"           => 1,
        "Convenios"                => 2,
        "Clientes"                 => 2,
        "HierarquiaDescontos"      => 3,
        "Vendas"                   => 4,
        "VendaFiscais"             => 5,
        "VendaItemFiscais"         => 6,
        "VendaReceitas"            => 5,
        "VendaReceitaItens"        => 6,
        "EntregaPerfis"            => 2,
        "EntregaFaixas"            => 3,
        "EntregaAgendas"           => 3,
        "Feriados"                 => 2,
        "Entregas"                 => 5,
        "EntregaEventos"           => 6,
        "CampanhasFidelidade"       => 3,
        "CampanhasFidelidadeItens"  => 4,
        "PremiosFidelidade"         => 1,
        "Promocoes"                => 2,
        "ContasBancarias"          => 2,
        "ContasPagar"              => 3,
        "Substancias"              => 1,
        "GruposPrincipais"         => 1,
        "GruposProdutos"           => 1,
        "SubGrupos"                => 1,
        "Secoes"                   => 1,
        "ProdutoFamilias"          => 1,
        "ProdutosLocais"           => 1,

        // Nível 2 — depende de nível 1 (Pessoa → Colaborador/Fornecedor, classificações → Produto)
        "Colaboradores"            => 2,
        "Fornecedores"             => 2,
        "PessoasContato"           => 2,
        "PessoasEndereco"          => 2,
        "Produtos"                 => 2,

        // Nível 3 — depende de nível 2 (Produto → sub-tabelas, Colaborador → Usuário)
        "Usuarios"                 => 3,
        "ProdutosBarras"           => 3,
        "ProdutosMs"               => 3,
        "ProdutosSubstancias"      => 3,
        "ProdutosFornecedores"     => 3,
        "ProdutosFiscal"           => 3,
        "ProdutosDados"            => 3,

        // Nível 3 — Compras (depende de Fornecedores nível 2)
        "Compras"                  => 3,

        // Nível 4 — depende de nível 3
        "ComprasProdutos"          => 4,
        "UsuarioFilialGrupos"      => 4,

        // Nível 5 — depende de nível 4 (CompraFiscal depende de ComprasProdutos)
        "ComprasFiscal"            => 5,

        // Nível 0 — referência
        "IcmsUfs"                  => 0,
        "AtualizacoesPreco"        => 3,
        "AtualizacoesPrecoItens"   => 4,

        // ── Fase 3: furo do dicionario (ordem de FK) ──
        // GLOBAL
        "Municipios"                     => 0,
        "AtributosVariacao"              => 1,
        "NaturezasOperacao"              => 1,
        "Adquirentes"                    => 1,
        "ValoresAtributo"                => 2,
        "NaturezaOperacaoRegras"         => 2,
        "ComissaoFaixasDesconto"         => 2,
        "ProdutosVariacoes"              => 3,
        "ProdutosAtributos"              => 3,
        "HierarquiasComissao"            => 3,
        "ColaboradorComissaoAgrupadores" => 3,
        "ProdutosVariacoesValores"       => 4,
        "Vouchers"                       => 5,
        // POR-FILIAL
        "SelfCheckoutTerminais"          => 1,
        "MovimentosEstoque"              => 3,
        "Caixas"                         => 3,
        "SelfCheckoutChamadosAtendente"  => 3,
        "ProdutosLotes"                  => 4,
        "InventariosSngpc"               => 4,
        "SngpcMapas"                     => 4,
        "SelfCheckoutConfiguracoes"      => 4,
        "CaixaFechamentoDeclarados"      => 4,
        "ComprasProdutosLotes"           => 5,
        "InventariosSngpcItens"          => 5,
        "VendaFarmaciaPopulares"         => 5,
        "SelfCheckoutConciliacoesEstoque" => 5,
        "ContasReceber"                  => 6,
        "MovimentosLote"                 => 6,
        "VendaFarmaciaPopularItens"      => 6,
        "CaixaMovimentos"                => 7,
        "MovimentosContaBancaria"        => 8,

        // Nível 0 — configurações (sem dependência)
        "Configuracoes"            => 0,

        // Tabelas locais (não replicam, mas listadas por completude)
        "LogsAcao"                 => 5,
        "LogsErro"                 => 5,

        // Fallback — tabelas desconhecidas por último
        _ => 10
    };

    private static readonly Dictionary<string, Type> _tiposPorTabela = new()
    {
        ["Filiais"] = typeof(Filial),
        ["Pessoas"] = typeof(Pessoa),
        ["PessoasContato"] = typeof(PessoaContato),
        ["PessoasEndereco"] = typeof(PessoaEndereco),
        ["Colaboradores"] = typeof(Colaborador),
        ["Fornecedores"] = typeof(Fornecedor),
        ["Usuarios"] = typeof(Usuario),
        ["UsuariosGrupos"] = typeof(GrupoUsuario),
        ["UsuariosGruposPermissao"] = typeof(GrupoPermissao),
        ["UsuarioFilialGrupos"] = typeof(UsuarioFilialGrupo),
        ["Fabricantes"] = typeof(Fabricante),
        ["Prescritores"] = typeof(Prescritor),
        ["PlanosContas"] = typeof(PlanoConta),
        ["TiposPagamento"] = typeof(TipoPagamento),
        ["Convenios"] = typeof(Convenio),
        ["Clientes"] = typeof(Cliente),
        ["HierarquiaDescontos"] = typeof(HierarquiaDesconto),
        ["Vendas"] = typeof(Venda),
        ["VendaFiscais"] = typeof(VendaFiscal),
        ["VendaItemFiscais"] = typeof(VendaItemFiscal),
        ["VendaReceitas"] = typeof(VendaReceita),
        ["VendaReceitaItens"] = typeof(VendaReceitaItem),
        ["EntregaPerfis"] = typeof(EntregaPerfil),
        ["EntregaFaixas"] = typeof(EntregaFaixa),
        ["EntregaAgendas"] = typeof(EntregaAgenda),
        ["Feriados"] = typeof(Feriado),
        ["Entregas"] = typeof(Entrega),
        ["EntregaEventos"] = typeof(EntregaEvento),
        ["CampanhasFidelidade"] = typeof(CampanhaFidelidade),
        ["CampanhasFidelidadeItens"] = typeof(CampanhaFidelidadeItem),
        ["PremiosFidelidade"] = typeof(PremioFidelidade),
        ["Promocoes"] = typeof(Promocao),
        ["ContasBancarias"] = typeof(ContaBancaria),
        ["ContasPagar"] = typeof(ContaPagar),
        ["Substancias"] = typeof(Substancia),
        ["GruposPrincipais"] = typeof(GrupoPrincipal),
        ["GruposProdutos"] = typeof(GrupoProduto),
        ["SubGrupos"] = typeof(SubGrupo),
        ["Secoes"] = typeof(Secao),
        ["ProdutoFamilias"] = typeof(ProdutoFamilia),
        ["ProdutosLocais"] = typeof(ProdutoLocal),
        ["Produtos"] = typeof(Produto),
        ["ProdutosBarras"] = typeof(ProdutoBarras),
        ["ProdutosMs"] = typeof(ProdutoMs),
        ["ProdutosSubstancias"] = typeof(ProdutoSubstancia),
        ["ProdutosFornecedores"] = typeof(ProdutoFornecedor),
        ["ProdutosFiscal"] = typeof(ProdutoFiscal),
        ["ProdutosDados"] = typeof(ProdutoDados),
        ["Ncms"] = typeof(Ncm),
        ["NcmFederais"] = typeof(NcmFederal),
        ["NcmIcmsUfs"] = typeof(NcmIcmsUf),
        ["NcmStUfs"] = typeof(NcmStUf),
        // "Configuracoes" REMOVIDA na fase 2 (INFRA, nao replica mais — o cursor do pull morava nela).
        // Op antiga em transito cai como TipoDesconhecido na quarentena (visivel, descartavel pelo admin).
        ["IcmsUfs"] = typeof(IcmsUf),
        ["AtualizacoesPreco"] = typeof(AtualizacaoPreco),
        ["AtualizacoesPrecoItens"] = typeof(AtualizacaoPrecoItem),
        ["Compras"] = typeof(Compra),
        ["ComprasProdutos"] = typeof(CompraProduto),
        ["ComprasFiscal"] = typeof(CompraFiscal),
        ["LogsAcao"] = typeof(LogAcao),
        ["LogsErro"] = typeof(LogErro),

        // ── Fase 3: reconciliacao do furo (BaseEntity que enfileiravam mas nao aplicavam) ──
        // GLOBAL
        ["Municipios"] = typeof(Municipio),
        ["AtributosVariacao"] = typeof(AtributoVariacao),
        ["ValoresAtributo"] = typeof(ValorAtributo),
        ["ProdutosAtributos"] = typeof(ProdutoAtributo),
        ["ProdutosVariacoes"] = typeof(ProdutoVariacao),
        ["ProdutosVariacoesValores"] = typeof(ProdutoVariacaoValor),
        ["HierarquiasComissao"] = typeof(HierarquiaComissao),
        ["ComissaoFaixasDesconto"] = typeof(ComissaoFaixaDesconto),
        ["ColaboradorComissaoAgrupadores"] = typeof(ColaboradorComissaoAgrupador),
        ["Adquirentes"] = typeof(Adquirente),
        ["Vouchers"] = typeof(Voucher),
        ["NaturezasOperacao"] = typeof(NaturezaOperacao),
        ["NaturezaOperacaoRegras"] = typeof(NaturezaOperacaoRegra),
        // POR-FILIAL
        ["ProdutosLotes"] = typeof(ProdutoLote),
        ["MovimentosLote"] = typeof(MovimentoLote),
        ["MovimentosEstoque"] = typeof(MovimentoEstoque),
        ["Caixas"] = typeof(Caixa),
        ["CaixaMovimentos"] = typeof(CaixaMovimento),
        ["CaixaFechamentoDeclarados"] = typeof(CaixaFechamentoDeclarado),
        ["MovimentosContaBancaria"] = typeof(MovimentoContaBancaria),
        ["ContasReceber"] = typeof(ContaReceber),
        ["ComprasProdutosLotes"] = typeof(CompraProdutoLote),
        ["VendaFarmaciaPopulares"] = typeof(VendaFarmaciaPopular),
        ["VendaFarmaciaPopularItens"] = typeof(VendaFarmaciaPopularItem),
        ["InventariosSngpc"] = typeof(InventarioSngpc),
        ["InventariosSngpcItens"] = typeof(InventarioSngpcItem),
        ["SngpcMapas"] = typeof(SngpcMapa),
        ["SelfCheckoutTerminais"] = typeof(SelfCheckoutTerminal),
        ["SelfCheckoutConfiguracoes"] = typeof(SelfCheckoutConfiguracao),
        ["SelfCheckoutChamadosAtendente"] = typeof(SelfCheckoutChamadoAtendente),
        ["SelfCheckoutConciliacoesEstoque"] = typeof(SelfCheckoutConciliacaoEstoque),
    };
}
