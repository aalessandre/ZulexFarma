using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using ZulexPharma.API.Controllers;

namespace ZulexPharma.Tests.BugsAtivos;

/// <summary>
/// Codex P0.1 (cura fase 1): o SyncController usava [Authorize] puro — qualquer JWT humano alcancava
/// o data plane. VERDE apos a fase 1: policy de NO (syncNode) no data plane + policy admin no painel.
/// Testes estruturais (reflection): travam a FORMA da autorizacao pra endpoint novo nao nascer aberto.
/// </summary>
public class SegurancaTests
{
    [Fact]
    public void SyncController_DataPlane_NaoPodeAceitarQualquerJwtHumano()
    {
        var controller = typeof(SyncController);

        var deClasse = controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
        var enviar = controller.GetMethod("Enviar")?.GetCustomAttributes<AuthorizeAttribute>() ?? [];
        var receber = controller.GetMethod("Receber")?.GetCustomAttributes<AuthorizeAttribute>() ?? [];

        bool Restrito(IEnumerable<AuthorizeAttribute> attrs) =>
            attrs.Any(a => !string.IsNullOrEmpty(a.Policy) || !string.IsNullOrEmpty(a.Roles));

        var dataPlaneRestrito = Restrito(deClasse) || (Restrito(enviar) && Restrito(receber));

        Assert.True(dataPlaneRestrito,
            "FRONTEIRA DE AUTORIZACAO QUEBRADA: o SyncController exige so' [Authorize] (qualquer JWT " +
            "autenticado, humano inclusive). Um usuario comum pode empurrar operacoes arbitrarias " +
            "(/enviar), baixar escopo de qualquer filial (/receber?filiais=...), ler payload integral " +
            "(/fila) e apagar/resetar estado. Cura (fase 1): policy de NO (claim syncNode) no data " +
            "plane + policy de admin nos endpoints de painel.");
    }

    [Fact]
    public void TodasAsActions_DeclaramPolicyOuAnonymous()
    {
        // Endpoint novo no SyncController NAO pode nascer so' com o [Authorize] de classe (linha de
        // base): ou declara a policy certa (SyncNode/SyncAdmin) ou e' explicitamente anonimo (handshake).
        var actions = typeof(SyncController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());

        Assert.NotEmpty(actions);
        foreach (var action in actions)
        {
            var ok = action.GetCustomAttributes<AllowAnonymousAttribute>().Any()
                  || action.GetCustomAttributes<AuthorizeAttribute>().Any(a => !string.IsNullOrEmpty(a.Policy));
            Assert.True(ok, $"Action '{action.Name}' sem policy explicita nem [AllowAnonymous] — " +
                "endpoint do sync nao pode nascer aberto pra qualquer JWT autenticado.");
        }
    }
}
