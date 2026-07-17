using System.Text.Json;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.Fase3;

/// <summary>
/// FASE 3 — convergencia: mesmas ops em qualquer ordem = mesmo estado final (linha E lapide),
/// guards de identidade (SyncGuid) e de relogio, e serializacao de applies concorrentes.
/// </summary>
[Collection("pg")]
public class ConvergenciaTests
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly PostgresFixture _pg;
    public ConvergenciaTests(PostgresFixture pg) => _pg = pg;

    private Fabricante Payload(long id, string nome, DateTime criadoEm, DateTime? atualizadoEm, long criador = 3) => new()
    {
        Id = id, Nome = nome, CriadoEm = criadoEm, AtualizadoEm = atualizadoEm, NoOrigemId = criador, Ativo = true,
        // guid ESTAVEL por registro (fluxo real: toda op da linha carrega o guid dela)
        SyncGuid = new Guid($"00000000-0000-0000-0000-{id:D12}")
    };

    private async Task<ResultadoSync> Aplicar(string op, long id, Fabricante? payload, DateTime opCriadoEm, long escritor)
    {
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        return await SyncApplicator.AplicarOperacaoAsync(db, "Fabricantes", op, id,
            payload == null ? null : JsonSerializer.Serialize(payload, _json), opCriadoEm, escritor);
    }

    [Fact]
    public async Task DeleteMaisNovoQueUpdate_MataNosDoisLados()
    {
        var t0 = new DateTime(2026, 7, 3, 10, 0, 0);
        var tU = new DateTime(2026, 7, 3, 11, 0, 0);
        var tD = new DateTime(2026, 7, 3, 12, 0, 0); // delete MAIS NOVO -> deve vencer
        const long idUD = 3_000_000_501;
        const long idDU = 3_000_000_502;

        // Replica 1 recebe U depois D; replica 2 recebe D depois U
        await Aplicar("I", idUD, Payload(idUD, "V0", t0, null), t0, 3);
        await Aplicar("U", idUD, Payload(idUD, "EDITADO", t0, tU), tU, 3);
        await Aplicar("D", idUD, null, tD, 3);

        await Aplicar("I", idDU, Payload(idDU, "V0", t0, null), t0, 3);
        await Aplicar("D", idDU, null, tD, 3);
        var resU = await Aplicar("U", idDU, Payload(idDU, "EDITADO", t0, tU), tU, 3);

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var l1 = await db.Fabricantes.FindAsync(idUD);
        var l2 = await db.Fabricantes.FindAsync(idDU);
        Assert.True(l1 == null && l2 == null,
            $"D mais novo deve matar nos DOIS lados: U-depois-D deu linha {(l2 != null ? "VIVA" : "morta")} " +
            $"(U={resU}) — o U mais velho que a lapide tem que ser Stale.");
        Assert.Equal(ResultadoSync.Stale, resU);
    }

    [Fact]
    public async Task ColisaoDeIdentidade_MesmoId_SyncGuidDiferente_EmLinhaSincronizada_Quarentena()
    {
        var t0 = new DateTime(2026, 7, 3, 10, 0, 0);
        var t1 = new DateTime(2026, 7, 3, 11, 0, 0);
        const long id = 3_000_000_503;

        // Linha chegou VIA SYNC (NoOrigemId=3, guid G1)
        var original = Payload(id, "REGISTRO DO NO 3", t0, null);
        original.SyncGuid = Guid.NewGuid();
        Assert.Equal(ResultadoSync.Aplicado, await Aplicar("I", id, original, t0, 3));

        // "Gemeo" com a MESMA faixa manda registro DIFERENTE no mesmo Id (guid G2)
        var impostor = Payload(id, "REGISTRO DO GEMEO", t0, t1);
        impostor.SyncGuid = Guid.NewGuid();
        var res = await Aplicar("U", id, impostor, t1, 3);

        Assert.Equal(ResultadoSync.ColisaoIdentidade, res);
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var linha = await db.Fabricantes.FindAsync(id);
        Assert.Equal("REGISTRO DO NO 3", linha!.Nome); // NUNCA SetValues em colisao — fusao silenciosa e' o pior defeito
    }

    [Fact]
    public async Task LinhaLocalPreSync_AdotaGuidRemoto_SemQuarentena()
    {
        var t0 = new DateTime(2026, 7, 3, 10, 0, 0);
        var t1 = new DateTime(2026, 7, 3, 11, 0, 0);
        const long id = 777_701; // linha "local" (seed/pre-sync): NoOrigemId null

        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            db.Fabricantes.Add(new Fabricante { Id = id, Nome = "SEED LOCAL", CriadoEm = t0, NoOrigemId = null, Ativo = true });
            await db.SaveChangesAsync();
        }

        var remoto = Payload(id, "MESMO REGISTRO, GUID DO PAR", t0, t1);
        remoto.SyncGuid = Guid.NewGuid();
        var res = await Aplicar("U", id, remoto, t1, 3);

        Assert.Equal(ResultadoSync.Aplicado, res); // linha nunca-sincronizada gerou guid proprio — adota o do par
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
            Assert.Equal(remoto.SyncGuid, (await db.Fabricantes.FindAsync(id))!.SyncGuid);
    }

    [Fact]
    public async Task OpDoFuturo_SeguraNaQuarentena()
    {
        const long id = 3_000_000_504;
        var futuro = ZulexPharma.Domain.Helpers.DataHoraHelper.Agora().AddHours(2);
        var res = await Aplicar("I", id, Payload(id, "DO FUTURO", futuro, futuro), futuro, 3);
        Assert.Equal(ResultadoSync.RelogioSuspeito, res);
    }

    [Fact]
    public async Task AppliesConcorrentes_DaMesmaLinha_ConvergemProMaisNovo()
    {
        var t0 = new DateTime(2026, 7, 3, 10, 0, 0);
        var tVelha = new DateTime(2026, 7, 3, 11, 0, 0);
        var tNova = new DateTime(2026, 7, 3, 12, 0, 0);
        const long id = 3_000_000_505;
        await Aplicar("I", id, Payload(id, "V0", t0, null), t0, 3);

        // Dois applies em PARALELO (contextos separados): o advisory lock por registro serializa;
        // o resultado tem que ser a versao MAIS NOVA independentemente de quem termina por ultimo.
        for (var rodada = 0; rodada < 3; rodada++)
        {
            await Task.WhenAll(
                Aplicar("U", id, Payload(id, "VELHA", t0, tVelha), tVelha, 1),
                Aplicar("U", id, Payload(id, "NOVA", t0, tNova), tNova, 2));

            await using var db = _pg.CriarContexto(aplicandoSync: true);
            var linha = await db.Fabricantes.FindAsync(id);
            Assert.True(linha!.Nome == "NOVA",
                $"rodada {rodada}: corrida do apply deixou '{linha.Nome}' — o SELECT+SetValues sem lock " +
                "permitia o update VELHO commitar por ultimo e vencer. O lock por registro serializa.");
        }
    }
}
