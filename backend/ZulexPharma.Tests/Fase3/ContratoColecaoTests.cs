using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.Fase3;

/// <summary>
/// FASE 3 — CONTRATO DE COLECAO no OUTBOX e no APPLICATOR (mecanismo dos filhos POCO):
/// - editar o pai SEM Include -> a chave da colecao e' OMITIDA no JSON -> o destino PRESERVA os filhos
///   (o motivo da reversao da v1: FinalizarAsync sem Itens.Descontos apagaria desconto de toda venda);
/// - editar COM Include -> chave presente = autoritativa -> destino reconcilia (delete-missing);
/// - editar SO' o filho -> o pai ganha touch e a op "U" sai com a colecao presente.
/// NOTA (fase 6): usa Adquirente+AdquirenteBandeira (filho POCO whitelisted) — os filhos de Cliente
/// foram promovidos a BaseEntity (replicam sozinhos) e nao exercitam mais este mecanismo.
/// </summary>
[Collection("pg")]
public class ContratoColecaoTests
{
    private readonly PostgresFixture _pg;
    public ContratoColecaoTests(PostgresFixture pg) => _pg = pg;

    private async Task<long> CriarAdquirenteComFilhosAsync()
    {
        await using var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1);
        var adq = new Adquirente { Nome = $"CONTRATO {Guid.NewGuid():N}" };
        adq.Bandeiras.Add(new AdquirenteBandeira { Bandeira = "VISA" });
        adq.Bandeiras.Add(new AdquirenteBandeira { Bandeira = "MASTER" });
        db.Adquirentes.Add(adq);
        await db.SaveChangesAsync();
        return adq.Id;
    }

    private async Task<string> UltimoJsonDaOpAsync(long adqId, string operacao)
    {
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var op = await db.SyncFila.Where(f => f.Tabela == "Adquirentes" && f.RegistroId == adqId && f.Operacao == operacao)
            .OrderByDescending(f => f.Id).FirstAsync();
        return op.DadosJson!;
    }

    private static bool TemChave(string json, string chave)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty(chave, out var v) && v.ValueKind == JsonValueKind.Array;
    }

    [Fact]
    public async Task Outbox_InsertIncluiColecao_EdicaoSemIncludeOmite_ComIncludeInclui()
    {
        var adqId = await CriarAdquirenteComFilhosAsync();

        // INSERT: grafo recem-criado em memoria = verdade -> colecao PRESENTE
        Assert.True(TemChave(await UltimoJsonDaOpAsync(adqId, "I"), "bandeiras"),
            "no INSERT do agregado a colecao em memoria E' o estado — precisa viajar");

        // Edicao SEM Include: a colecao default vazia NAO pode virar [] autoritativo
        await using (var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1))
        {
            var adq = await db.Adquirentes.FirstAsync(a => a.Id == adqId);
            adq.Nome = "EDITADO SEM INCLUDE";
            await db.SaveChangesAsync();
        }
        Assert.False(TemChave(await UltimoJsonDaOpAsync(adqId, "U"), "bandeiras"),
            "PERIGO DA V1: edicao sem Include serializou a colecao vazia como [] — o destino apagaria " +
            "filhos LEGITIMOS. Colecao nao carregada = chave OMITIDA (preservar).");

        // Edicao COM Include: colecao carregada = autoritativa -> presente
        await using (var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1))
        {
            var adq = await db.Adquirentes.Include(a => a.Bandeiras).FirstAsync(a => a.Id == adqId);
            adq.Nome = "EDITADO COM INCLUDE";
            await db.SaveChangesAsync();
        }
        Assert.True(TemChave(await UltimoJsonDaOpAsync(adqId, "U"), "bandeiras"));
    }

    [Fact]
    public async Task EdicaoSoDeFilho_GeraOpDoPai_ComColecaoPresente()
    {
        var adqId = await CriarAdquirenteComFilhosAsync();
        long filaAntes;
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
            filaAntes = await db.SyncFila.Where(f => f.Tabela == "Adquirentes" && f.RegistroId == adqId).CountAsync();

        // Padrao dos services: RemoveRange + re-add, SEM tocar o cabecalho
        await using (var db2 = _pg.CriarContexto(modo: "Edge", noCodigo: 1))
        {
            var adq = await db2.Adquirentes.Include(a => a.Bandeiras).FirstAsync(a => a.Id == adqId);
            db2.Set<AdquirenteBandeira>().RemoveRange(adq.Bandeiras);
            adq.Bandeiras.Clear();
            adq.Bandeiras.Add(new AdquirenteBandeira { Bandeira = "ELO" });
            await db2.SaveChangesAsync();
        }

        await using var db3 = _pg.CriarContexto(aplicandoSync: true);
        var opsDepois = await db3.SyncFila.Where(f => f.Tabela == "Adquirentes" && f.RegistroId == adqId).CountAsync();
        Assert.True(opsDepois > filaAntes,
            "EDICAO SO'-DE-FILHO NAO REPLICOU: o pai ficou Unchanged e nenhuma op saiu — o touch do " +
            "pai (fase 3) precisa promover o agregado pra Modified.");

        var json = await UltimoJsonDaOpAsync(adqId, "U");
        Assert.True(TemChave(json, "bandeiras"), "a op do touch precisa levar a colecao (carregada) como autoritativa");
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("bandeiras").GetArrayLength());
    }
}
