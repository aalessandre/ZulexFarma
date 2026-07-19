using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.BugsAtivos;

/// <summary>
/// Cura fase 3 (Codex P0.7 / synAteAqui §6.2): reconciliacao de filhos POCO por contrato de colecao
/// (chave presente = autoritativa -> delete-missing; chave omitida = preserva). Sem isso, editar um
/// agregado com RemoveRange+re-add DUPLICAVA os filhos no destino.
/// NOTA (fase 6): usa Adquirente+AdquirenteBandeira (filho POCO whitelisted) — os filhos de Cliente
/// viraram BaseEntity (uniao, replicam sozinhos) e nao exercitam mais o mecanismo POCO.
/// </summary>
[Collection("pg")]
public class FilhosPocoTests
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly PostgresFixture _pg;
    public FilhosPocoTests(PostgresFixture pg) => _pg = pg;

    [Fact]
    public async Task EditarAgregado_NaoDeveDuplicarFilhosNoDestino()
    {
        var t0 = new DateTime(2026, 7, 1, 10, 0, 0);
        var t1 = new DateTime(2026, 7, 1, 12, 0, 0);
        const long adqId = 3_000_000_111;

        // Adquirente V1 com 2 bandeiras (Ids da faixa do no 3)
        var v1 = NovoAdquirente(adqId, t0, atualizadoEm: null);
        v1.Bandeiras.Add(new AdquirenteBandeira { Id = 3_000_000_112, AdquirenteId = adqId, Bandeira = "VISA" });
        v1.Bandeiras.Add(new AdquirenteBandeira { Id = 3_000_000_113, AdquirenteId = adqId, Bandeira = "MASTER" });
        Assert.Equal(ResultadoSync.Aplicado, await Aplicar("Adquirentes", "I", adqId, JsonSerializer.Serialize(v1, _json), t0));

        // Edicao na origem: RemoveRange + re-add -> as MESMAS 2 bandeiras voltam com Ids NOVOS.
        var v2 = NovoAdquirente(adqId, t0, atualizadoEm: t1);
        v2.Bandeiras.Add(new AdquirenteBandeira { Id = 3_000_000_114, AdquirenteId = adqId, Bandeira = "VISA" });
        v2.Bandeiras.Add(new AdquirenteBandeira { Id = 3_000_000_115, AdquirenteId = adqId, Bandeira = "MASTER" });
        var resV2 = await Aplicar("Adquirentes", "U", adqId, JsonSerializer.Serialize(v2, _json), t1);

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var filhos = await db.Set<AdquirenteBandeira>().Where(b => b.AdquirenteId == adqId).ToListAsync();
        Assert.True(filhos.Count == 2,
            $"DUPLICACAO: editado UMA vez (2 bandeiras) e o destino ficou com {filhos.Count} " +
            $"[{string.Join(", ", filhos.Select(f => $"{f.Id}:{f.Bandeira}"))}] (U deu {resV2}). Cura: colecao " +
            "PRESENTE no JSON = autoritativa -> diff por Id com delete-missing.");
    }

    /// <summary>Op "U" com a chave da colecao OMITIDA (pai salvo sem Include) NAO pode apagar os filhos.</summary>
    [Fact]
    public async Task ColecaoOmitidaNoJson_PreservaFilhosNoDestino()
    {
        var t0 = new DateTime(2026, 7, 1, 10, 0, 0);
        var t1 = new DateTime(2026, 7, 1, 12, 0, 0);
        const long adqId = 3_000_000_117;

        var v1 = NovoAdquirente(adqId, t0, atualizadoEm: null);
        v1.Bandeiras.Add(new AdquirenteBandeira { Id = 3_000_000_118, AdquirenteId = adqId, Bandeira = "VISA" });
        v1.Bandeiras.Add(new AdquirenteBandeira { Id = 3_000_000_119, AdquirenteId = adqId, Bandeira = "MASTER" });
        Assert.Equal(ResultadoSync.Aplicado, await Aplicar("Adquirentes", "I", adqId, JsonSerializer.Serialize(v1, _json), t0));

        // Edicao SEM Include na origem: o JSON da op vem SEM a chave 'bandeiras'
        var v2 = NovoAdquirente(adqId, t0, atualizadoEm: t1);
        var node = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(v2, _json))!.AsObject();
        node.Remove("bandeiras");
        Assert.Equal(ResultadoSync.Aplicado, await Aplicar("Adquirentes", "U", adqId, node.ToJsonString(_json), t1));

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var filhos = await db.Set<AdquirenteBandeira>().Where(b => b.AdquirenteId == adqId).CountAsync();
        Assert.True(filhos == 2,
            $"CHAVE OMITIDA = 'nao carregada, preserve' — mas o destino ficou com {filhos} filhos. " +
            "Apagar aqui e' o bug da v1 revertida.");
    }

    private static Adquirente NovoAdquirente(long id, DateTime criadoEm, DateTime? atualizadoEm) => new()
    {
        Id = id, Nome = "ADQUIRENTE FILHOS POCO", CriadoEm = criadoEm, AtualizadoEm = atualizadoEm,
        NoOrigemId = 3, SyncGuid = new Guid($"00000000-0000-0000-0000-{id:D12}"), Ativo = true
    };

    private async Task<ResultadoSync> Aplicar(string tabela, string op, long id, string json, DateTime opCriadoEm)
    {
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        return await SyncApplicator.AplicarOperacaoAsync(db, tabela, op, id, json, opCriadoEm, noOrigemId: 3);
    }
}
