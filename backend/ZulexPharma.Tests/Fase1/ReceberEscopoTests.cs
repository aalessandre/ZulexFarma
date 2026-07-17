using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ZulexPharma.API.Controllers;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.Fase1;

/// <summary>
/// FASE 1: o escopo do PULL vem do CADASTRO do no no servidor (SyncNoFiliais) — os parametros
/// filialId/filiais da query sao ignorados. Um no nao consegue pedir escopo alheio nem falsificar
/// a propria identidade (que vem do token).
/// </summary>
[Collection("pg")]
public class ReceberEscopoTests
{
    private readonly PostgresFixture _pg;
    public ReceberEscopoTests(PostgresFixture pg) => _pg = pg;

    [Fact]
    public async Task Receber_IgnoraQueryString_EUsaEscopoDoCadastro()
    {
        const int noCodigo = 9201;
        const long filialAutorizada = 9210;
        const long filialAlheia = 9211;

        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            await db.SyncFila.Where(f => f.Tabela == "EscopoTeste").ExecuteDeleteAsync();
            await db.SyncNos.Where(n => n.NoCodigo == noCodigo).ExecuteDeleteAsync();

            db.SyncNos.Add(new SyncNo
            {
                NoCodigo = noCodigo, Nome = "No de escopo", Status = "Ativo",
                ChaveHash = SyncNoAuth.HashChave(SyncNoAuth.GerarChave()),
                Filiais = { new SyncNoFilial { NoCodigo = noCodigo, FilialId = filialAutorizada } }
            });

            db.SyncFila.AddRange(
                new SyncFila { Tabela = "EscopoTeste", Operacao = "I", RegistroId = 1, NoOrigemId = 9202, FilialDonoId = null, DadosJson = "{}" },              // GLOBAL -> recebe
                new SyncFila { Tabela = "EscopoTeste", Operacao = "I", RegistroId = 2, NoOrigemId = 9202, FilialDonoId = filialAutorizada, DadosJson = "{}" },  // minha filial -> recebe
                new SyncFila { Tabela = "EscopoTeste", Operacao = "I", RegistroId = 3, NoOrigemId = 9202, FilialDonoId = filialAlheia, DadosJson = "{}" },      // filial ALHEIA -> nunca
                new SyncFila { Tabela = "EscopoTeste", Operacao = "I", RegistroId = 4, NoOrigemId = noCodigo, FilialDonoId = null, DadosJson = "{}" }           // eco proprio -> nunca
            );
            await db.SaveChangesAsync();
        }

        await using var dbCtrl = _pg.CriarContexto(aplicandoSync: true);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["No:Modo"] = "Hub", ["No:Codigo"] = "0",
        }).Build();
        var controller = new SyncController(dbCtrl, config)
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

        // Tentativa de SPOOF: pede escopo da filial alheia e identidade falsa na query
        var resposta = await controller.Receber(filialId: 12345, filiais: $"{filialAutorizada},{filialAlheia}", ultimoId: 0, limite: 100);

        var ok = Assert.IsType<OkObjectResult>(resposta);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var registros = json.RootElement.GetProperty("data").EnumerateArray()
            .Where(e => e.GetProperty("Tabela").GetString() == "EscopoTeste")
            .Select(e => e.GetProperty("RegistroId").GetInt64())
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(new List<long> { 1, 2 }, registros); // GLOBAL + filial autorizada; NUNCA a alheia (3) nem o eco (4)
    }

    /// <summary>
    /// Fase 1b (achado ALTO da revisao): edge declara filiais (?filiais=) mas o cadastro na central
    /// nao tem NENHUMA — servir so' GLOBAL avancaria o cursor por cima das ops por-filial e, quando o
    /// admin corrigisse o cadastro, o gap seria permanente. Resposta: 422 ruidoso, nao lote parcial.
    /// </summary>
    [Fact]
    public async Task Receber_CadastroSemFiliais_ComEdgeDeclarandoFiliais_Retorna422()
    {
        const int noCodigo = 9203;
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            await db.SyncNos.Where(n => n.NoCodigo == noCodigo).ExecuteDeleteAsync();
            db.SyncNos.Add(new SyncNo
            {
                NoCodigo = noCodigo, Nome = "Sem filiais", Status = "Ativo",
                ChaveHash = SyncNoAuth.HashChave(SyncNoAuth.GerarChave())
                // cadastro SEM SyncNoFiliais (esquecimento no provisionamento)
            });
            await db.SaveChangesAsync();
        }

        var resposta = await CriarController(noCodigo).Receber(filiais: "9210", ultimoId: 0, limite: 100);
        var obj = Assert.IsType<ObjectResult>(resposta);
        Assert.Equal(422, obj.StatusCode);
    }

    /// <summary>Fase 1b (achado MEDIO): Suspenso no painel corta o data plane JA (token vale 1h).</summary>
    [Fact]
    public async Task Receber_NoSuspenso_Retorna403()
    {
        const int noCodigo = 9204;
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            await db.SyncNos.Where(n => n.NoCodigo == noCodigo).ExecuteDeleteAsync();
            db.SyncNos.Add(new SyncNo
            {
                NoCodigo = noCodigo, Nome = "Suspenso", Status = "Suspenso",
                ChaveHash = SyncNoAuth.HashChave(SyncNoAuth.GerarChave())
            });
            await db.SaveChangesAsync();
        }

        var resposta = await CriarController(noCodigo).Receber(ultimoId: 0, limite: 100);
        var obj = Assert.IsType<ObjectResult>(resposta);
        Assert.Equal(403, obj.StatusCode);
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
}
