using System.Text.Json;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.BugsAtivos;

/// <summary>
/// BUG ATIVO (plano §2 / Codex P0.5, cura fase 3): o desempate do LWW usa entidade.NoOrigemId,
/// que e' o CRIADOR da linha (imutavel) — nao o ESCRITOR da operacao (SyncFila.NoOrigemId).
/// Duas edicoes com o MESMO AtualizadoEm feitas em nos diferentes empatam sempre em
/// "criador == criador" -> a segunda op e' descartada NAO IMPORTA QUEM escreveu -> o resultado
/// depende da ORDEM DE CHEGADA e os nos divergem. VERMELHO ate' a fase 3 (AtualizadoPorNoId
/// + comparador unico com escritor real).
/// </summary>
[Collection("pg")]
public class LwwTests
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly PostgresFixture _pg;
    public LwwTests(PostgresFixture pg) => _pg = pg;

    [Fact]
    public async Task Lww_EmpateDeTimestamp_DeveVencerOEscritorMaior_NaoOPrimeiroQueChegou()
    {
        var t0 = new DateTime(2026, 7, 1, 10, 0, 0);
        var t1 = new DateTime(2026, 7, 1, 12, 0, 0); // MESMO timestamp nas duas edicoes (empate real)
        const long id = 3_000_000_101;               // faixa do no 3 (criador)
        var guid = Guid.NewGuid();

        Fabricante Payload(string nome, DateTime? atualizadoEm) => new()
        {
            Id = id, Nome = nome, CriadoEm = t0, AtualizadoEm = atualizadoEm,
            NoOrigemId = 3, SyncGuid = guid, Ativo = true // NoOrigemId da ENTIDADE = criador, nunca muda
        };

        // INSERT original (criado no no 3)
        await Aplicar("I", Payload("ORIGINAL", null), opWriter: 3, t0);

        // Edicao X escrita no NO 1 e edicao Y escrita no NO 3 — AtualizadoEm identico.
        // Regra travada (dono + fase 3): empate -> vence o ESCRITOR maior => Y (no 3) em qualquer ordem.
        var resX = await Aplicar("U", Payload("EDITADO NO 1", t1), opWriter: 1, t1);
        var resY = await Aplicar("U", Payload("EDITADO NO 3", t1), opWriter: 3, t1);

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var final = await db.Fabricantes.FindAsync(id);
        Assert.NotNull(final);
        Assert.True(final!.Nome == "EDITADO NO 3",
            $"LWW DIVERGENTE: no empate de timestamp venceu '{final.Nome}' (primeiro que chegou; X={resX}, Y={resY}). " +
            "O desempate comparou criador==criador (entidade.NoOrigemId) em vez do ESCRITOR da op " +
            "(SyncFila.NoOrigemId): nos que recebem em ordens diferentes terminam com conteudos diferentes. " +
            "Cura (fase 3): AtualizadoPorNoId + comparador unico pelo escritor real.");
    }

    private async Task<ResultadoSync> Aplicar(string op, Fabricante payload, long opWriter, DateTime opCriadoEm)
    {
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        return await SyncApplicator.AplicarOperacaoAsync(
            db, "Fabricantes", op, payload.Id, JsonSerializer.Serialize(payload, _json), opCriadoEm, opWriter);
    }
}
