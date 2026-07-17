using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ZulexPharma.API.Controllers;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.Fase5;

/// <summary>
/// FASE 5 — retencao da fila central por ACK (religada com os pre-requisitos que a tentativa
/// revertida de 07/2026 nao tinha), guard da marca de compactacao e ferramentas de bootstrap.
/// </summary>
[Collection("pg")]
public class RetencaoTests
{
    private readonly PostgresFixture _pg;
    public RetencaoTests(PostgresFixture pg) => _pg = pg;

    private SyncController Controller(bool comoNo = false, int noCodigo = 9501)
    {
        var db = _pg.CriarContexto(aplicandoSync: true);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["No:Modo"] = "Hub", ["No:Codigo"] = "0" }).Build();
        var claims = comoNo
            ? new[] { new Claim("syncNode", "true"), new Claim("noCodigo", noCodigo.ToString()) }
            : new[] { new Claim("isAdmin", "True") };
        return new SyncController(db, config)
        {
            ControllerContext = new ControllerContext
            { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "teste")) } }
        };
    }

    private static JsonDocument Corpo(IActionResult r) =>
        JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(r).Value));

    [Fact]
    public async Task Retencao_ApagaSoAteOMinAckDosAtivos_EFailClosedSemAck()
    {
        var antigo = new DateTime(2026, 7, 1, 8, 0, 0);
        long seqA, seqB, seqC;

        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            // Slate limpo e DETERMINISTICO: a retencao computa MIN(ack) sobre TODOS os nos Ativos,
            // entao um no leftover de outro teste com ack baixo mascararia o resultado. SyncNos e'
            // pura infra de teste (o seeder nao cria) e a colecao 'pg' roda SEQUENCIAL -> apagar
            // todos aqui e' seguro (cada teste que precisa recria os seus). O finally limpa os meus.
            await db.SyncNos.ExecuteDeleteAsync();
            await db.SyncEstadoLocal.Where(e => e.Chave == "sync.retencao.marca").ExecuteDeleteAsync();
            await db.SyncFila.Where(f => f.Tabela == "RetencaoTeste").ExecuteDeleteAsync();

            db.SyncFila.AddRange(
                new SyncFila { Tabela = "RetencaoTeste", Operacao = "I", RegistroId = 1, NoOrigemId = 9502, DadosJson = "{}", CriadoEm = antigo },
                new SyncFila { Tabela = "RetencaoTeste", Operacao = "I", RegistroId = 2, NoOrigemId = 9502, DadosJson = "{}", CriadoEm = antigo },
                new SyncFila { Tabela = "RetencaoTeste", Operacao = "I", RegistroId = 3, NoOrigemId = 9502, DadosJson = "{}", CriadoEm = antigo });
            await db.SaveChangesAsync();
            await SyncPublicador.NumerarEObterMarcaAsync(db);

            var seqs = await db.SyncFila.Where(f => f.Tabela == "RetencaoTeste").OrderBy(f => f.SeqEntrega)
                .Select(f => f.SeqEntrega!.Value).ToListAsync();
            (seqA, seqB, seqC) = (seqs[0], seqs[1], seqs[2]);

            db.SyncNos.AddRange(
                new SyncNo { NoCodigo = 9501, Nome = "Ret A", Status = "Ativo", ChaveHash = "x", UltimoAckSeq = seqC },
                new SyncNo { NoCodigo = 9502, Nome = "Ret B", Status = "Ativo", ChaveHash = "x", UltimoAckSeq = seqB },
                new SyncNo { NoCodigo = 9503, Nome = "Ret suspenso", Status = "Suspenso", ChaveHash = "x", UltimoAckSeq = 0 });
            await db.SaveChangesAsync();
        }

        try
        {
            // min(ack dos ATIVOS) = seqB -> apaga seqA e seqB (antigas); seqC fica. O Suspenso (ack 0)
            // NAO segura a retencao — tirar o no do MIN e' a acao explicita de mudar o status.
            using var corpo = Corpo(await Controller().Limpar(dias: 1));
            Assert.Equal(2, corpo.RootElement.GetProperty("data").GetProperty("removidosRetencao").GetInt64());

            await using (var db = _pg.CriarContexto(aplicandoSync: true))
            {
                var restantes = await db.SyncFila.Where(f => f.Tabela == "RetencaoTeste").Select(f => f.SeqEntrega!.Value).ToListAsync();
                Assert.Equal(new[] { seqC }, restantes);

                // FAIL-CLOSED: um Ativo com ack 0 (registrado, nunca puxou) trava TUDO — o cenario
                // que a retencao revertida perdia (no invisivel pro MIN).
                db.SyncNos.Add(new SyncNo { NoCodigo = 9504, Nome = "Nunca puxou", Status = "Ativo", ChaveHash = "x", UltimoAckSeq = 0 });
                await db.SaveChangesAsync();
            }
            using var corpo2 = Corpo(await Controller().Limpar(dias: 1));
            Assert.Equal(0, corpo2.RootElement.GetProperty("data").GetProperty("removidosRetencao").GetInt64());

            // Guard da marca: pull com cursor ABAIXO da compactacao = 409 (nunca lote parcial silencioso)
            await using (var db = _pg.CriarContexto(aplicandoSync: true))
            {
                db.SyncNos.Add(new SyncNo { NoCodigo = 9505, Nome = "Voltou sem bootstrap", Status = "Ativo", ChaveHash = "x" });
                await db.SaveChangesAsync();
            }
            var resposta = await Controller(comoNo: true, noCodigo: 9505).Receber(cursor: 0, ack: 0, limite: 10);
            var obj = Assert.IsType<ObjectResult>(resposta);
            Assert.Equal(409, obj.StatusCode);
        }
        finally
        {
            // Limpeza COMPLETA (a marca de compactacao quebraria pulls de outros testes com 409):
            // marca, nos deste teste E as ops sobreviventes (seqC) — numeradas e servivies.
            await using var db = _pg.CriarContexto(aplicandoSync: true);
            await db.SyncEstadoLocal.Where(e => e.Chave == "sync.retencao.marca").ExecuteDeleteAsync();
            await db.SyncNos.Where(n => n.NoCodigo >= 9501 && n.NoCodigo <= 9505).ExecuteDeleteAsync();
            await db.SyncFila.Where(f => f.Tabela == "RetencaoTeste").ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task BootstrapInfo_ECursor_FechamOCiclo()
    {
        using var info = Corpo(await Controller().BootstrapInfo());
        var marca = info.RootElement.GetProperty("data").GetProperty("marca").GetInt64();
        var geracao = info.RootElement.GetProperty("data").GetProperty("geracao").GetString();
        Assert.False(string.IsNullOrEmpty(geracao));

        Assert.IsType<OkObjectResult>(await Controller().DefinirCursor(new SyncCursorDto(marca, geracao)));

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        Assert.Equal(marca.ToString(), (await db.SyncEstadoLocal.FirstAsync(e => e.Chave == "sync.cursor.entrega")).Valor);
        Assert.Equal(geracao, (await db.SyncEstadoLocal.FirstAsync(e => e.Chave == "sync.hub.geracao.vista")).Valor);

        // Nao deixar cursor/geracao cravados no banco compartilhado (o Receber de outro teste usaria).
        await db.SyncEstadoLocal.Where(e => e.Chave == "sync.cursor.entrega" || e.Chave == "sync.hub.geracao.vista").ExecuteDeleteAsync();
    }

    [Fact]
    public async Task Checksum_Deterministico_EDetectaMudanca()
    {
        long id;
        await using (var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1))
        {
            var fab = new Fabricante { Nome = "CHECKSUM F5" };
            db.Fabricantes.Add(fab);
            await db.SaveChangesAsync();
            id = fab.Id;
        }

        string Hash(IActionResult r) { using var c = Corpo(r); return c.RootElement.GetProperty("data").GetProperty("hash").GetString()!; }
        var h1 = Hash(await Controller().Checksum("Fabricantes"));
        var h2 = Hash(await Controller().Checksum("Fabricantes"));
        Assert.Equal(h1, h2); // deterministico

        await using (var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1))
        {
            var fab = await db.Fabricantes.FindAsync(id);
            fab!.Nome = "CHECKSUM F5 MUDOU";
            await db.SaveChangesAsync();
        }
        Assert.NotEqual(h1, Hash(await Controller().Checksum("Fabricantes"))); // AtualizadoEm mudou o hash

        var invalida = await Controller().Checksum("TabelaInvalida");
        Assert.IsType<BadRequestObjectResult>(invalida); // sem SQL arbitrario
    }
}
