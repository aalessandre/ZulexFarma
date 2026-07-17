using Microsoft.EntityFrameworkCore;
using ZulexPharma.Domain.Entities;

namespace ZulexPharma.Tests.Fase0;

/// <summary>
/// VERDES (fase 0.3, cura P1.1): StandaloneCloud nao captura outbox nem lapide — antes,
/// Sync:Habilitado=false so' desligava o transporte e a SyncFila crescia pra sempre.
/// Os carimbos de negocio (Codigo/NoOrigemId/AtualizadoEm) continuam funcionando.
/// </summary>
[Collection("pg")]
public class StandaloneCloudTests
{
    private readonly PostgresFixture _pg;
    public StandaloneCloudTests(PostgresFixture pg) => _pg = pg;

    [Fact]
    public async Task StandaloneCloud_NaoCapturaOutbox_MasMantemCarimbos()
    {
        long id;
        await using (var db = _pg.CriarContexto(modo: "StandaloneCloud", noCodigo: 1))
        {
            var fab = new Fabricante { Nome = "STANDALONE SEM OUTBOX" };
            db.Fabricantes.Add(fab);
            await db.SaveChangesAsync();
            id = fab.Id;

            Assert.False(string.IsNullOrEmpty(fab.Codigo), "carimbo de Codigo deve continuar em standalone");
            Assert.Equal(1, fab.NoOrigemId);
        }

        await using (var db = _pg.CriarContexto(modo: "StandaloneCloud", noCodigo: 1))
        {
            Assert.False(await db.SyncFila.AnyAsync(s => s.Tabela == "Fabricantes" && s.RegistroId == id),
                "standalone NAO pode enfileirar outbox (P1.1: fila crescia sem teto com sync desligado)");

            var fab = await db.Fabricantes.FindAsync(id);
            db.Fabricantes.Remove(fab!);
            await db.SaveChangesAsync();

            Assert.False(await db.SyncTombstones.AnyAsync(t => t.Tabela == "Fabricantes" && t.RegistroId == id),
                "standalone NAO deve cravar lapide (infra de sync)");
        }
    }

    [Fact]
    public async Task Edge_ContinuaCapturandoOutbox()
    {
        await using var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1);
        var fab = new Fabricante { Nome = "EDGE COM OUTBOX" };
        db.Fabricantes.Add(fab);
        await db.SaveChangesAsync();

        Assert.True(await db.SyncFila.AnyAsync(s => s.Tabela == "Fabricantes" && s.RegistroId == fab.Id && s.Operacao == "I"),
            "edge captura outbox normalmente (regressao da fase 0.3 se falhar)");
    }
}
