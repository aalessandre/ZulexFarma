using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Tests.Fase1;

/// <summary>
/// FASE 1 (P0.3): no HUB (No:Codigo=0), a origem da op NAO pode vir do claim filialId do JWT do
/// usuario — isso mistura os eixos (origem/no vs filial-dona) e quebra o anti-eco: um registro
/// criado no no 1 e editado na NUVEM ganharia NoOrigemId=1 (claim) e o anti-eco esconderia a
/// edicao justamente do no 1. Origem correta do hub e' SEMPRE 0.
/// </summary>
[Collection("pg")]
public class HubOrigemTests
{
    private readonly PostgresFixture _pg;
    public HubOrigemTests(PostgresFixture pg) => _pg = pg;

    [Fact]
    public async Task Hub_OpNasceComOrigemZero_MesmoComClaimFilialNoJwt()
    {
        // Simula um request autenticado no hub por um usuario logado na filial 5
        var http = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim("filialId", "5") }, "teste"))
            }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["No:Modo"] = "Hub",
            ["No:Codigo"] = "0",
        }).Build();
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_pg.ConnTeste).Options;

        long id;
        await using (var db = new AppDbContext(opts, http, config))
        {
            var fab = new Fabricante { Nome = "CRIADO NO HUB" };
            db.Fabricantes.Add(fab);
            await db.SaveChangesAsync();
            id = fab.Id;
        }

        await using var leitura = _pg.CriarContexto(aplicandoSync: true);
        var op = await leitura.SyncFila.FirstAsync(s => s.Tabela == "Fabricantes" && s.RegistroId == id);

        Assert.True(op.NoOrigemId == 0,
            $"MISTURA DE EIXOS: a op nasceu com NoOrigemId={op.NoOrigemId} (claim filialId do JWT) em vez " +
            "de 0 (o no do SERVIDOR). Com isso o anti-eco do PULL esconde a edicao da nuvem exatamente do " +
            "no que criou o registro. Origem vem da CONFIG do servidor, nunca do usuario.");
    }
}
