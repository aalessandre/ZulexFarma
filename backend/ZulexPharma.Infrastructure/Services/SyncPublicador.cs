using Microsoft.EntityFrameworkCore;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// FASE 2 — o PUBLICADOR que mata o gap do cursor (synAteAqui §6.1, opcao B).
///
/// O problema: SyncFila.Id e' alocado no INSERT mas so' fica VISIVEL no COMMIT. O cursor antigo
/// (Id > ultimoId) avancava por cima de uma op cuja transacao commitava tarde — perda PERMANENTE e
/// silenciosa sob escrita concorrente. pg_snapshot_xmin NAO cura com cursor em Id (ordens de xid e
/// identity sao independentes — contraexemplo formal no synAteAqui).
///
/// A cura: numerar (SeqEntrega = nextval) SOMENTE linhas ja' COMMITADAS — o UPDATE do publicador
/// nao enxerga linha de transacao em voo, entao ela pega um numero MAIOR na rodada seguinte.
/// Transacao longa nao trava nada. Buraco na numeracao e' inofensivo (nextval nao e' transacional;
/// o cursor e' ">"). Advisory lock TRY: quem pega numera, quem nao pega serve o que ja' esta'
/// numerado — sem worker novo (a central nao roda background loop; numeramos oportunisticamente
/// no /receber e no fim do /enviar).
/// </summary>
public static class SyncPublicador
{
    /// <summary>Chave do advisory lock (constante arbitraria, exclusiva do publicador).</summary>
    private const long ChaveLock = 7_202_607_170_001;

    /// <summary>
    /// Numera as linhas commitadas sem SeqEntrega (se conseguir o lock) e retorna a MARCA D'AGUA
    /// (MAX numerado). O /receber serve ate' ela e usa-a como cursorProximo quando o lote nao encheu.
    /// So' o HUB chama (edge nao numera — o push usa flag, imune ao gap).
    /// </summary>
    public static async Task<long> NumerarEObterMarcaAsync(AppDbContext db, CancellationToken ct = default)
    {
        // CONTRATO: nunca chamar dentro de transacao ambiente — o advisory xact lock viveria ate' o
        // commit DELA (retendo a numeracao alheia) e a marca enxergaria numeracao nao commitada.
        if (db.Database.CurrentTransaction != null)
            throw new InvalidOperationException(
                "SyncPublicador.NumerarEObterMarcaAsync nao pode rodar dentro de transacao ambiente.");

        // pg_try_advisory_xact_lock exige transacao (o lock vive ate' o commit). Abre propria.
        var txPropria = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var pegouLock = await db.Database
                .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({ChaveLock}) AS \"Value\"")
                .FirstAsync(ct);

            if (pegouLock)
            {
                // ORDER BY Id so' pra numeracao acompanhar a ordem de chegada no caso comum;
                // a corretude nao depende disso (LWW decide por versao, nao por ordem de entrega).
                await db.Database.ExecuteSqlRawAsync("""
                    UPDATE "SyncFila" f SET "SeqEntrega" = nextval('seq_sync_entrega')
                    FROM (SELECT "Id" FROM "SyncFila" WHERE "SeqEntrega" IS NULL ORDER BY "Id") p
                    WHERE f."Id" = p."Id"
                    """, ct);
            }

            if (txPropria != null) await txPropria.CommitAsync(ct);
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

        return await db.SyncFila.MaxAsync(f => (long?)f.SeqEntrega, ct) ?? 0;
    }
}
