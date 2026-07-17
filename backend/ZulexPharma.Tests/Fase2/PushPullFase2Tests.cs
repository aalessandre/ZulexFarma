using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ZulexPharma.API.Controllers;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.Fase2;

/// <summary>
/// FASE 2 — push honesto (resposta POR OP; so' o aplicado redistribui), Stale auditavel,
/// drenagem que re-enfileira no hub, ACK por no e geracao do hub.
/// </summary>
[Collection("pg")]
public class PushPullFase2Tests
{
    private readonly PostgresFixture _pg;
    public PushPullFase2Tests(PostgresFixture pg) => _pg = pg;

    private const int No = 9401;

    private async Task RegistrarNoAsync(int codigo)
    {
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        await db.SyncNos.Where(n => n.NoCodigo == codigo).ExecuteDeleteAsync();
        db.SyncNos.Add(new SyncNo
        {
            NoCodigo = codigo, Nome = $"Fase2 {codigo}", Status = "Ativo",
            ChaveHash = SyncNoAuth.HashChave(SyncNoAuth.GerarChave())
        });
        await db.SaveChangesAsync();
    }

    private SyncController CriarController(int noCodigo)
    {
        var db = _pg.CriarContexto(aplicandoSync: true);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["No:Modo"] = "Hub", ["No:Codigo"] = "0",
        }).Build();
        return new SyncController(db, config)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim("syncNode", "true"),
                        new Claim("noCodigo", noCodigo.ToString())
                    }, "teste"))
                }
            }
        };
    }

    private static JsonDocument Corpo(IActionResult resposta) =>
        JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(resposta).Value));

    [Fact]
    public async Task PushHonesto_RespostaPorOp_ESoAplicadaRedistribui()
    {
        await RegistrarNoAsync(No);
        var uidValida = Guid.NewGuid();
        var uidDesconhecida = Guid.NewGuid();
        var t0 = new DateTime(2026, 7, 2, 10, 0, 0);

        var fab = new Fabricante { Id = 3_000_000_401, Nome = "PUSH HONESTO", CriadoEm = t0, NoOrigemId = No, Ativo = true };
        var ops = new List<SyncOperacaoDto>
        {
            new("Fabricantes", "I", fab.Id, null, JsonSerializer.Serialize(fab, JsonWeb()), No, null, t0, uidValida),
            new("TabelaQueNaoExiste", "I", 1, null, "{}", No, null, t0, uidDesconhecida),
        };

        using var corpo = Corpo(await CriarController(No).Enviar(ops));
        var resultados = corpo.RootElement.GetProperty("data").GetProperty("resultados").EnumerateArray().ToList();
        Assert.Equal(2, resultados.Count);
        Assert.Equal("Aplicado", resultados[0].GetProperty("resultado").GetString());
        Assert.Equal("TipoDesconhecido", resultados[1].GetProperty("resultado").GetString());

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        Assert.True(await db.SyncFila.AnyAsync(f => f.OpUid == uidValida),
            "op APLICADA precisa entrar na fila de redistribuicao (e ser numerada)");
        Assert.False(await db.SyncFila.AnyAsync(f => f.OpUid == uidDesconhecida),
            "op QUARENTENADA nao pode se espalhar antes de resolvida (P1.4)");
        var filaAplicada = await db.SyncFila.FirstAsync(f => f.OpUid == uidValida);
        Assert.NotNull(filaAplicada.SeqEntrega); // o publicador do fim do /enviar numerou
    }

    [Fact]
    public async Task Stale_EhAuditavel_ENaoRedistribui()
    {
        await RegistrarNoAsync(No);
        var t0 = new DateTime(2026, 7, 2, 10, 0, 0);
        var t1 = new DateTime(2026, 7, 2, 12, 0, 0);
        const long id = 3_000_000_402;

        Fabricante Payload(string nome, DateTime? atualizadoEm) => new()
        { Id = id, Nome = nome, CriadoEm = t0, AtualizadoEm = atualizadoEm, NoOrigemId = No, Ativo = true };

        var ctrl = CriarController(No);
        await ctrl.Enviar(new List<SyncOperacaoDto>
        {
            new("Fabricantes", "I", id, null, JsonSerializer.Serialize(Payload("V-NOVA", t1), JsonWeb()), No, null, t0, Guid.NewGuid()),
        });

        // Edicao MAIS VELHA chega depois (outro no atrasado) -> Stale
        var uidStale = Guid.NewGuid();
        using var corpo = Corpo(await CriarController(No).Enviar(new List<SyncOperacaoDto>
        {
            new("Fabricantes", "U", id, null, JsonSerializer.Serialize(Payload("V-VELHA", t0), JsonWeb()), No, null, t0, uidStale),
        }));
        Assert.Equal("Stale", corpo.RootElement.GetProperty("data").GetProperty("resultados")[0].GetProperty("resultado").GetString());

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        Assert.False(await db.SyncFila.AnyAsync(f => f.OpUid == uidStale), "op stale nao redistribui");
        Assert.True(await db.SyncQuarentena.AnyAsync(q =>
                q.Tabela == "Fabricantes" && q.RegistroId == id && q.Motivo == "Stale" && q.Resolvido),
            "OBJETIVO 7: descarte por LWW precisa deixar trilha auditavel (antes era 100% silencioso)");
    }

    [Fact]
    public async Task Drenagem_ReenfileiraQuandoRetryAplica()
    {
        await RegistrarNoAsync(No);
        var t0 = new DateTime(2026, 7, 2, 10, 0, 0);
        var t1 = new DateTime(2026, 7, 2, 11, 0, 0);
        const long id = 3_000_000_403;
        var uidU = Guid.NewGuid();

        Fabricante Payload(string nome, DateTime? atualizadoEm) => new()
        { Id = id, Nome = nome, CriadoEm = t0, AtualizadoEm = atualizadoEm, NoOrigemId = No, Ativo = true };

        // U chega ANTES do I -> PrecisaRetry -> quarentena; NAO entra na fila
        await CriarController(No).Enviar(new List<SyncOperacaoDto>
        {
            new("Fabricantes", "U", id, null, JsonSerializer.Serialize(Payload("EDITADO", t1), JsonWeb()), No, null, t1, uidU),
        });
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
            Assert.False(await db.SyncFila.AnyAsync(f => f.OpUid == uidU), "PrecisaRetry nao redistribui na chegada");

        // O I chega -> aplica. O push SEGUINTE drena a quarentena: o U aplica e ENTRA na fila.
        await CriarController(No).Enviar(new List<SyncOperacaoDto>
        {
            new("Fabricantes", "I", id, null, JsonSerializer.Serialize(Payload("ORIGINAL", null), JsonWeb()), No, null, t0, Guid.NewGuid()),
        });
        await CriarController(No).Enviar(new List<SyncOperacaoDto>()); // lote vazio: so' drena

        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            var fila = await db.SyncFila.FirstOrDefaultAsync(f => f.OpUid == uidU);
            Assert.True(fila != null && fila.SeqEntrega != null,
                "retry que APLICOU precisa ser re-enfileirado (com o OpUid preservado) e numerado — " +
                "senao a op resolvida nunca chega aos outros nos");
        }
    }

    [Fact]
    public async Task Ack_AtualizaRegistroDoNo_EGeracaoEstavel()
    {
        await RegistrarNoAsync(No);

        using var corpo1 = Corpo(await CriarController(No).Receber(cursor: 0, ack: 42, limite: 10));
        var geracao1 = corpo1.RootElement.GetProperty("geracao").GetString();
        Assert.False(string.IsNullOrEmpty(geracao1));

        using var corpo2 = Corpo(await CriarController(No).Receber(cursor: 0, ack: 40, limite: 10)); // ack menor NAO regride
        var geracao2 = corpo2.RootElement.GetProperty("geracao").GetString();
        Assert.Equal(geracao1, geracao2); // geracao e' estavel (so' muda em restore do hub)

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var no = await db.SyncNos.AsNoTracking().FirstAsync(n => n.NoCodigo == No);
        Assert.Equal(42, no.UltimoAckSeq);
    }

    private static JsonSerializerOptions JsonWeb() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
