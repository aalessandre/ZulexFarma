using System.Security.Claims;
using ZulexPharma.API.Middleware;

namespace ZulexPharma.Tests.Fase1;

/// <summary>
/// Fase 1b (achado ALTO da revisao adversarial): o token de MAQUINA e' assinado com a mesma chave
/// dos tokens humanos e passaria em qualquer [Authorize] puro do app (ex.: /api/vendas,
/// /api/clientes/pesquisar — PII do hub inteiro). O gate confina o principal de no ao data plane.
/// </summary>
public class SyncNodeGateTests
{
    [Theory]
    [InlineData("/api/sync/handshake", true)]
    [InlineData("/api/sync/enviar", true)]
    [InlineData("/api/sync/receber", true)]
    [InlineData("/api/sync/fila", false)]           // painel: so' humano admin
    [InlineData("/api/sync/nos", false)]            // cadastro de nos: so' humano admin
    [InlineData("/api/sync/limpar", false)]
    [InlineData("/api/vendas", false)]              // resto da API: NUNCA com token de no
    [InlineData("/api/clientes/pesquisar", false)]
    [InlineData("/api/auth/login", false)]
    public void TokenDeNo_SoAlcancaODataPlane(string caminho, bool esperado)
        => Assert.Equal(esperado, SyncNodeGate.PermitidoParaNo(caminho));

    [Fact]
    public void DetectaPrincipalDeNo_PorClaim()
    {
        var deNo = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("syncNode", "true") }, "t"));
        var humano = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("isAdmin", "True") }, "t"));
        Assert.True(SyncNodeGate.EhTokenDeNo(deNo));
        Assert.False(SyncNodeGate.EhTokenDeNo(humano));
        Assert.False(SyncNodeGate.EhTokenDeNo(null));
    }
}
