using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ZulexPharma.API.Controllers;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.BugsAtivos;

/// <summary>
/// GAP DO CURSOR — FECHADO na fase 2 (era o bug mais grave do subsistema; synAteAqui §6.1).
/// Antes: o PULL servia "Id > ultimoId", mas o Id e' alocado no INSERT e visivel so' no COMMIT —
/// op de transacao que commitava tarde era PULADA pra sempre. Agora: o publicador numera
/// (SeqEntrega) SOMENTE linhas commitadas, e o cursor e' a SeqEntrega — commit tardio pega numero
/// MAIOR na rodada seguinte e E' entregue. Este teste reproduz exatamente o cenario que perdia dado.
/// </summary>
[Collection("pg")]
public class CursorGapTests
{
    private readonly PostgresFixture _pg;
    public CursorGapTests(PostgresFixture pg) => _pg = pg;

    private const int NoLeitor = 9301;   // no que puxa
    private const int NoEscritor = 9302; // origem das ops (anti-eco nao filtra pro leitor)

    private const string InsertSql = """
        INSERT INTO "SyncFila" ("Tabela","Operacao","RegistroId","NoOrigemId","DadosJson","CriadoEm","Enviado")
        VALUES ('GapTeste','I', {0}, 9302, '{{}}', now(), false) RETURNING "Id"
        """;

    [Fact]
    public async Task CommitTardio_EhEntregue_PeloCursorDeSeqEntrega()
    {
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            await db.SyncFila.Where(f => f.Tabela == "GapTeste").ExecuteDeleteAsync();
            await db.SyncNos.Where(n => n.NoCodigo == NoLeitor).ExecuteDeleteAsync();
            db.SyncNos.Add(new SyncNo
            {
                NoCodigo = NoLeitor, Nome = "Leitor do gap", Status = "Ativo",
                ChaveHash = SyncNoAuth.HashChave(SyncNoAuth.GerarChave())
            });
            await db.SaveChangesAsync();
        }

        // Tx A: aloca o Id MENOR e fica ABERTA (lote grande ainda commitando)
        await using var connA = await _pg.AbrirConexaoAsync();
        var txA = await connA.BeginTransactionAsync();
        var idA = await InserirAsync(connA, txA, registroId: 9001);

        // Tx B: aloca o Id MAIOR e commita PRIMEIRO
        await using var connB = await _pg.AbrirConexaoAsync();
        var txB = await connB.BeginTransactionAsync();
        var idB = await InserirAsync(connB, txB, registroId: 9002);
        await txB.CommitAsync();
        Assert.True(idA < idB, "pre-condicao: A alocou Id menor que B");

        // PULL 1: o publicador numera SO' a B (a A esta' em voo) e o cursor avanca
        var (entregues1, cursor1) = await PullAsync(cursor: 0);

        // A tx A commita DEPOIS do pull — no desenho antigo essa op estava PERDIDA pra sempre
        await txA.CommitAsync();

        // PULL 2: a A e' numerada AGORA (numero maior) e entregue mesmo com o cursor ja' na frente
        var (entregues2, _) = await PullAsync(cursor1);

        var todas = entregues1.Concat(entregues2).ToHashSet();
        Assert.True(todas.Contains(9001) && todas.Contains(9002),
            $"GAP REGREDIU: op de commit tardio nao foi entregue. Pull1={string.Join(",", entregues1)} " +
            $"(cursor {cursor1}), Pull2={string.Join(",", entregues2)}. O publicador numera so' linha " +
            "COMMITADA e o cursor e' SeqEntrega — commit tardio DEVE pegar numero maior e ser entregue.");
    }

    private async Task<long> InserirAsync(NpgsqlConnection conn, NpgsqlTransaction tx, long registroId)
    {
        await using var cmd = new NpgsqlCommand(string.Format(InsertSql, registroId), conn, tx);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<(List<long> RegistroIds, long CursorProximo)> PullAsync(long cursor)
    {
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["No:Modo"] = "Hub", ["No:Codigo"] = "0",
        }).Build();
        var controller = new SyncController(db, config)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim("syncNode", "true"),
                        new Claim("noCodigo", NoLeitor.ToString())
                    }, "teste"))
                }
            }
        };

        var resposta = await controller.Receber(cursor: cursor, limite: 500, ack: 0);
        var ok = Assert.IsType<OkObjectResult>(resposta);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var ids = json.RootElement.GetProperty("data").EnumerateArray()
            .Where(e => e.GetProperty("Tabela").GetString() == "GapTeste")
            .Select(e => e.GetProperty("RegistroId").GetInt64())
            .ToList();
        var cursorProximo = json.RootElement.GetProperty("cursorProximo").GetInt64();
        return (ids, cursorProximo);
    }
}
