using System.Text.Json;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.BugsAtivos;

/// <summary>
/// BUG ATIVO (plano §2 / Codex P0.6, cura fase 3): UPDATE e DELETE das MESMAS operacoes logicas
/// nao convergem quando chegam em ordens diferentes. No no que recebe D antes do U, o U (mais novo,
/// que pela regra LWW deveria vencer e RECRIAR a linha) vira PrecisaRetry eterno — linha morta.
/// No no que recebe U antes do D, o D (mais velho) e' Stale — linha viva. Divergencia permanente.
/// VERMELHO ate' a fase 3 (U/D convergente: U mais novo que a lapide recria a linha).
/// </summary>
[Collection("pg")]
public class UdConvergenciaTests
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly PostgresFixture _pg;
    public UdConvergenciaTests(PostgresFixture pg) => _pg = pg;

    [Fact]
    public async Task UpdateDelete_MesmasOps_OrdensDiferentes_DevemConvergir()
    {
        var t0 = new DateTime(2026, 7, 1, 10, 0, 0);
        var tDelete = new DateTime(2026, 7, 1, 11, 0, 0);
        var tUpdate = new DateTime(2026, 7, 1, 12, 0, 0); // update MAIS NOVO que o delete -> deve vencer

        // Mesmo conjunto logico de ops {I@t0, D@tDelete, U@tUpdate} aplicado em DUAS ordens
        // (como dois nos recebendo em ordens diferentes). Estado final DEVE ser o mesmo.
        const long idOrdemDU = 3_000_000_102; // recebe D primeiro, depois U
        const long idOrdemUD = 3_000_000_103; // recebe U primeiro, depois D

        // Ordem 1: I -> D -> U
        await Aplicar("I", Payload(idOrdemDU, "ORIGINAL", null), t0);
        await Aplicar("D", null, tDelete, idOrdemDU);
        var resU1 = await Aplicar("U", Payload(idOrdemDU, "EDITADO DEPOIS DO DELETE", tUpdate), tUpdate);

        // Ordem 2: I -> U -> D
        await Aplicar("I", Payload(idOrdemUD, "ORIGINAL", null), t0);
        var resU2 = await Aplicar("U", Payload(idOrdemUD, "EDITADO DEPOIS DO DELETE", tUpdate), tUpdate);
        var resD2 = await Aplicar("D", null, tDelete, idOrdemUD);

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var linhaDU = await db.Fabricantes.FindAsync(idOrdemDU);
        var linhaUD = await db.Fabricantes.FindAsync(idOrdemUD);

        Assert.True((linhaDU != null) == (linhaUD != null),
            $"DIVERGENCIA PERMANENTE: as MESMAS ops em ordens diferentes deram estados diferentes — " +
            $"ordem D->U: linha {(linhaDU != null ? "VIVA" : "MORTA")} (U deu {resU1}); " +
            $"ordem U->D: linha {(linhaUD != null ? "VIVA" : "MORTA")} (U deu {resU2}, D deu {resD2}). " +
            "Pela regra LWW o U (mais novo que o D) vence nos DOIS casos: a linha deveria estar viva " +
            "com o conteudo do U. Cura (fase 3): U mais novo que a lapide RECRIA a linha (o JSON e' " +
            "estado completo); U sem linha e sem lapide vira upsert, nunca PrecisaRetry eterno.");
    }

    private Fabricante Payload(long id, string nome, DateTime? atualizadoEm) => new()
    {
        Id = id, Nome = nome, CriadoEm = new DateTime(2026, 7, 1, 10, 0, 0),
        AtualizadoEm = atualizadoEm, NoOrigemId = 3, Ativo = true,
        // guid ESTAVEL por registro (como no fluxo real: toda op da linha carrega o guid dela)
        SyncGuid = new Guid($"00000000-0000-0000-0000-{id:D12}")
    };

    private async Task<ResultadoSync> Aplicar(string op, Fabricante? payload, DateTime opCriadoEm, long? id = null)
    {
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        return await SyncApplicator.AplicarOperacaoAsync(
            db, "Fabricantes", op, id ?? payload!.Id,
            payload == null ? null : JsonSerializer.Serialize(payload, _json), opCriadoEm, noOrigemId: 3);
    }
}
