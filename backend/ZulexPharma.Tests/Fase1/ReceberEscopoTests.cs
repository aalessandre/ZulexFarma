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
}
