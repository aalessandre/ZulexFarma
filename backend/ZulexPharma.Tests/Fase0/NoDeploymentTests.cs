using Microsoft.Extensions.Configuration;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Tests.Fase0;

/// <summary>
/// VERDES (fase 0.3): provam o fail-fast REAL da identidade do no. O modo de falha antigo era
/// o oposto do prometido — appsettings versionado com No:Codigo=0 fazia loja sem env var subir
/// silenciosamente como hub.
/// </summary>
public class NoDeploymentTests
{
    private static IConfiguration Config(params (string chave, string valor)[] pares)
        => new ConfigurationBuilder().AddInMemoryCollection(
            pares.ToDictionary(p => p.chave, p => (string?)p.valor)).Build();

    [Fact]
    public void ModoAusente_FalhaAlto()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => NoDeployment.Resolver(Config(("No:Codigo", "1"))));
        Assert.Contains("No:Modo", ex.Message);
    }

    [Fact]
    public void Hub_ComCodigoDiferenteDeZero_FalhaAlto()
        => Assert.Throws<InvalidOperationException>(() =>
            NoDeployment.Resolver(Config(("No:Modo", "Hub"), ("No:Codigo", "3"))));

    [Fact]
    public void Hub_SemCodigo_AssumeZero()
    {
        var (modo, codigo) = NoDeployment.Resolver(Config(("No:Modo", "Hub")));
        Assert.Equal(NoModo.Hub, modo);
        Assert.Equal(0, codigo);
    }

    [Fact]
    public void Hub_ComSyncHabilitado_FalhaAlto()
        => Assert.Throws<InvalidOperationException>(() =>
            NoDeployment.Resolver(Config(("No:Modo", "Hub"), ("Sync:Habilitado", "true"))));

    [Fact]
    public void Edge_SemCodigo_FalhaAlto()
        => Assert.Throws<InvalidOperationException>(() => NoDeployment.Resolver(Config(("No:Modo", "Edge"))));

    [Fact]
    public void Edge_HabilitadoSemUrlCentral_FalhaAlto()
        => Assert.Throws<InvalidOperationException>(() =>
            NoDeployment.Resolver(Config(("No:Modo", "Edge"), ("No:Codigo", "1"), ("Sync:Habilitado", "true"))));

    [Fact]
    public void Edge_Valido_Resolve()
    {
        var (modo, codigo) = NoDeployment.Resolver(Config(
            ("No:Modo", "Edge"), ("No:Codigo", "2"),
            ("Sync:Habilitado", "true"), ("Sync:UrlCentral", "https://central.exemplo")));
        Assert.Equal(NoModo.Edge, modo);
        Assert.Equal(2, codigo);
    }

    [Fact]
    public void Edge_FallbackLegadoFilialCodigo_Aceito()
    {
        var (modo, codigo) = NoDeployment.Resolver(Config(("No:Modo", "Edge"), ("Filial:Codigo", "4")));
        Assert.Equal(NoModo.Edge, modo);
        Assert.Equal(4, codigo);
    }

    [Fact]
    public void Standalone_ComSyncHabilitado_FalhaAlto()
        => Assert.Throws<InvalidOperationException>(() =>
            NoDeployment.Resolver(Config(("No:Modo", "StandaloneCloud"), ("No:Codigo", "1"), ("Sync:Habilitado", "true"))));

    [Fact]
    public void Standalone_Valido_Resolve()
    {
        var (modo, codigo) = NoDeployment.Resolver(Config(("No:Modo", "StandaloneCloud"), ("No:Codigo", "7")));
        Assert.Equal(NoModo.StandaloneCloud, modo);
        Assert.Equal(7, codigo);
    }
}
