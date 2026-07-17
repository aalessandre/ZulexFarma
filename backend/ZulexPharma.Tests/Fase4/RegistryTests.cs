using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.Fase4;

/// <summary>
/// FASE 4 — registry fail-closed, Codigo sem ambiguidade e ledger.
/// </summary>
[Collection("pg")]
public class RegistryTests
{
    private readonly PostgresFixture _pg;
    public RegistryTests(PostgresFixture pg) => _pg = pg;

    /// <summary>
    /// O modelo ATUAL passa na validacao (toda entidade classificada, dicionario completo, POCO com
    /// FK). Entidade NOVA sem classificacao derruba o boot com lista nominal — este teste e' o que
    /// obriga quem criar um DbSet a classificar conscientemente.
    /// </summary>
    [Fact]
    public async Task Registry_ModeloAtual_CompletoEConsistente()
    {
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var ex = Record.Exception(() => SyncRegistry.ValidarModelo(db.Model));
        Assert.True(ex == null, $"SyncRegistry invalido:\n{ex?.Message}");
    }

    /// <summary>Codigo novo: formato {no}-{seq} — mata a ambiguidade (no 1+seq 11 == no 11+seq 1 == '111').</summary>
    [Fact]
    public async Task Codigo_NovoFormato_ComSeparadorDoNo()
    {
        await using var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1);
        var fab = new Fabricante { Nome = "CODIGO FASE 4" };
        db.Fabricantes.Add(fab);
        await db.SaveChangesAsync();
        Assert.Matches(new Regex(@"^1-\d+$"), fab.Codigo!);
    }

    /// <summary>Ledger aceita I (com dedup por Id) — so' U/D sao imutaveis.</summary>
    [Fact]
    public async Task Ledger_InsertAplicaEReentregaEhIdempotente()
    {
        var t0 = new DateTime(2026, 7, 5, 10, 0, 0);
        const long id = 3_000_000_701;
        var mov = new MovimentoEstoque
        {
            Id = id, ProdutoId = 3_000_000_901, FilialId = 1, Data = t0,
            Tipo = ZulexPharma.Domain.Enums.TipoMovimentoEstoque.Compra, Quantidade = 5, SaldoApos = 5,
            CriadoEm = t0, NoOrigemId = 3, SyncGuid = new Guid($"00000000-0000-0000-0000-{id:D12}"), Ativo = true
        };
        var json = System.Text.Json.JsonSerializer.Serialize(mov, new System.Text.Json.JsonSerializerOptions
        { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        Assert.Equal(ResultadoSync.Aplicado, await SyncApplicator.AplicarOperacaoAsync(db, "MovimentosEstoque", "I", id, json, t0, 3));
        Assert.Equal(ResultadoSync.Idempotente, await SyncApplicator.AplicarOperacaoAsync(db, "MovimentosEstoque", "I", id, json, t0, 3));
        Assert.Equal(ResultadoSync.LedgerImutavel, await SyncApplicator.AplicarOperacaoAsync(db, "MovimentosEstoque", "D", id, null, t0.AddHours(1), 3));
        Assert.NotNull(await db.Set<MovimentoEstoque>().FindAsync(id)); // o D nao apagou o fato
    }
}
