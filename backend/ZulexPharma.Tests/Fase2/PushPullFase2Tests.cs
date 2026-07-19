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

        // Desconhecida PRIMEIRO no lote de proposito: a aplicacao reordena ('ordenadas'), mas a
        // resposta por INDICE tem que seguir a ordem ORIGINAL — e' assim que o edge marca Enviado.
        var fab = new Fabricante { Id = 3_000_000_401, Nome = "PUSH HONESTO", CriadoEm = t0, NoOrigemId = No, Ativo = true };
        var ops = new List<SyncOperacaoDto>
        {
            new("TabelaQueNaoExiste", "I", 1, null, "{}", No, null, t0, uidDesconhecida),
            new("Fabricantes", "I", fab.Id, null, JsonSerializer.Serialize(fab, JsonWeb()), No, null, t0, uidValida),
        };

        using var corpo = Corpo(await CriarController(No).Enviar(ops));
        var resultados = corpo.RootElement.GetProperty("data").GetProperty("resultados").EnumerateArray().ToList();
        Assert.Equal(2, resultados.Count);
        Assert.Equal("TipoDesconhecido", resultados[0].GetProperty("resultado").GetString());
        Assert.Equal("Aplicado", resultados[1].GetProperty("resultado").GetString());

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
        {
            Id = id, Nome = nome, CriadoEm = t0, AtualizadoEm = atualizadoEm, NoOrigemId = No, Ativo = true,
            SyncGuid = new Guid($"00000000-0000-0000-0000-{id:D12}") // guid estavel da linha
        };

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
        // Fase 3 mudou o cenario: U-orfao agora aplica como UPSERT (nao e' mais PrecisaRetry).
        // O caso legitimo de quarentena e' DEPENDENCIA DE FK: Cliente chega antes da Pessoa.
        await RegistrarNoAsync(No);
        var t0 = new DateTime(2026, 7, 2, 10, 0, 0);
        const long pessoaId = 3_000_000_404;
        const long clienteId = 3_000_000_405;
        var uidCliente = Guid.NewGuid();

        var cliente = new Cliente { Id = clienteId, PessoaId = pessoaId, CriadoEm = t0, NoOrigemId = No, Ativo = true };
        var pessoa = new Pessoa { Id = pessoaId, Tipo = "F", Nome = "PESSOA DRENAGEM", CpfCnpj = "88877766655", CriadoEm = t0, NoOrigemId = No, Ativo = true };

        // Cliente chega ANTES da Pessoa -> 23503 -> PrecisaRetry -> quarentena; NAO entra na fila
        await CriarController(No).Enviar(new List<SyncOperacaoDto>
        {
            new("Clientes", "I", clienteId, null, JsonSerializer.Serialize(cliente, JsonWeb()), No, null, t0, uidCliente),
        });
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
            Assert.False(await db.SyncFila.AnyAsync(f => f.OpUid == uidCliente), "PrecisaRetry nao redistribui na chegada");

        // A Pessoa chega -> aplica. O push SEGUINTE drena a quarentena: o Cliente aplica e ENTRA na fila.
        await CriarController(No).Enviar(new List<SyncOperacaoDto>
        {
            new("Pessoas", "I", pessoaId, null, JsonSerializer.Serialize(pessoa, JsonWeb()), No, null, t0, Guid.NewGuid()),
        });
        await CriarController(No).Enviar(new List<SyncOperacaoDto>()); // lote vazio: so' drena

        await using (var dbFinal = _pg.CriarContexto(aplicandoSync: true))
        {
            var fila = await dbFinal.SyncFila.FirstOrDefaultAsync(f => f.OpUid == uidCliente);
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
        // Geracao e' ESTAVEL entre pulls. NOTA (revisao adversarial): restore de backup restaura o
        // MESMO uuid — quem detecta restore e' a REGRESSAO DA MARCA no edge (seqMaxNumerado < cursor
        // -> REBOOTSTRAP); a geracao cobre so' recriacao do banco do zero.
        Assert.Equal(geracao1, geracao2);

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var no = await db.SyncNos.AsNoTracking().FirstAsync(n => n.NoCodigo == No);
        Assert.Equal(42, no.UltimoAckSeq);
    }

    /// <summary>Fase 2b: edge FASE-1 (sem ack) tomaria pull congelado em silencio — 426 ruidoso.</summary>
    [Fact]
    public async Task ProtocoloAntigo_SemAck_Recebe426()
    {
        await RegistrarNoAsync(No);
        var resposta = await CriarController(No).Receber(cursor: 0, limite: 10); // ack default -1
        var obj = Assert.IsType<ObjectResult>(resposta);
        Assert.Equal(426, obj.StatusCode);
    }

    /// <summary>Exercita o ramo LOTE CHEIO do cursorProximo (para na ultima servida, nao na marca).</summary>
    [Fact]
    public async Task LoteCheio_CursorParaNaUltimaServida_EProximoPullContinua()
    {
        const int no = 9402;
        await RegistrarNoAsync(No); // emissor (9401) — sem depender da ordem dos outros testes
        await RegistrarNoAsync(no);
        var t0 = new DateTime(2026, 7, 2, 10, 0, 0);

        // Ancora o cursor na MARCA atual: a fila compartilhada acumula ops de outros testes e o
        // teste pagina de 1 em 1 — sem a ancora, os pulls nunca alcancariam as 3 ops novas.
        long cursor;
        using (var corpoAncora = Corpo(await CriarController(no).Receber(cursor: 0, ack: 0, limite: 1)))
            cursor = corpoAncora.RootElement.GetProperty("seqMaxNumerado").GetInt64();

        var uids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var ops = uids.Select((uid, i) =>
        {
            var fab = new Fabricante { Id = 3_000_000_410 + i, Nome = $"LOTE {i}", CriadoEm = t0, NoOrigemId = No, Ativo = true };
            return new SyncOperacaoDto("Fabricantes", "I", fab.Id, null, JsonSerializer.Serialize(fab, JsonWeb()), No, null, t0, uid);
        }).ToList();
        await CriarController(No).Enviar(ops); // origem = no 9401 -> visiveis pro 9402

        var recebidos = new List<Guid>();
        for (var pull = 0; pull < 8 && recebidos.Count < 3; pull++)
        {
            using var corpo = Corpo(await CriarController(no).Receber(cursor: cursor, ack: cursor, limite: 1));
            foreach (var e in corpo.RootElement.GetProperty("data").EnumerateArray())
            {
                var uid = e.GetProperty("OpUid").GetString();
                if (uid != null && uids.Contains(Guid.Parse(uid))) recebidos.Add(Guid.Parse(uid));
            }
            var proximo = corpo.RootElement.GetProperty("cursorProximo").GetInt64();
            Assert.True(proximo >= cursor, "cursor nunca regride");
            if (proximo == cursor) break; // nada mais a servir
            cursor = proximo;
        }

        Assert.True(uids.All(recebidos.Contains),
            $"paginacao com lote cheio perdeu op: recebidos {recebidos.Count}/3 — o ramo 'cursor = ultima " +
            "servida' (lote cheio) nao pode pular ops entre a ultima e a marca");
    }

    private static JsonSerializerOptions JsonWeb() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
