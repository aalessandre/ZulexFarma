using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using ZulexPharma.API.Controllers;

namespace ZulexPharma.Tests.BugsAtivos;

/// <summary>
/// BUG ATIVO (plano §2 / Codex P0.1, cura fase 1): o SyncController inteiro usa [Authorize] puro —
/// QUALQUER usuario autenticado (JWT humano) alcanca o data plane (/enviar com payload arbitrario,
/// /receber de qualquer escopo, /fila com DadosJson integral, /limpar, /resetar-recebimento).
/// VERMELHO ate' a fase 1 (policy de no no data plane + policy admin no painel).
/// Teste estrutural (reflection): nao sobe servidor, mas trava a FORMA da autorizacao.
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
}
