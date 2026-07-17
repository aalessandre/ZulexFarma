using Microsoft.Extensions.Configuration;

namespace ZulexPharma.Infrastructure.Data;

/// <summary>Papel do deployment no esquema de replicacao (fase 0 do plano de replicacao).</summary>
public enum NoModo
{
    /// <summary>Central consolidadora (nuvem). No:Codigo = 0. Captura outbox; NAO roda o loop de transporte (recebe via /api/sync).</summary>
    Hub,
    /// <summary>Servidor de loja (local ou hospedado) que replica com a central. No:Codigo >= 1 unico.</summary>
    Edge,
    /// <summary>Banco unico na nuvem, SEM replicacao: nao captura outbox nem roda transporte (cura P1.1: antes a SyncFila crescia pra sempre).</summary>
    StandaloneCloud
}

/// <summary>
/// Fonte UNICA de parse/validacao da identidade do no (No:Modo + No:Codigo).
/// REGRA (fase 0): No:Modo e' OBRIGATORIO e NAO tem default versionado. O modo de falha antigo era o
/// oposto do prometido: o appsettings.json versionado trazia No:Codigo=0, entao uma loja que esquecesse
/// a env var subia SILENCIOSAMENTE como hub — sem faixa de Id, sem filial seedada e sem sync.
/// Falha ruidosa > default silencioso (objetivo 7 do plano).
/// </summary>
public static class NoDeployment
{
    public static (NoModo Modo, int NoCodigo) Resolver(IConfiguration config)
    {
        var modoRaw = config["No:Modo"];
        if (string.IsNullOrWhiteSpace(modoRaw) || !Enum.TryParse<NoModo>(modoRaw, ignoreCase: true, out var modo))
            throw new InvalidOperationException(
                "Config 'No:Modo' ausente ou invalida. Defina EXPLICITAMENTE (env var No__Modo em prod, " +
                "appsettings.Development.json em dev): 'Hub' (central/nuvem consolidadora, No:Codigo=0), " +
                "'Edge' (servidor de loja que replica com a central, No:Codigo >= 1 unico) ou " +
                "'StandaloneCloud' (banco unico sem replicacao, No:Codigo >= 1). Sem default silencioso.");

        // Fallback legado "Filial:Codigo" mantido so' pro parse do numero — o MODO nunca tem fallback.
        var codigoRaw = config["No:Codigo"] ?? config["Filial:Codigo"];
        var syncHabilitado = string.Equals(config["Sync:Habilitado"], "true", StringComparison.OrdinalIgnoreCase);
        var urlCentral = config["Sync:UrlCentral"] ?? "";

        switch (modo)
        {
            case NoModo.Hub:
                if (codigoRaw != null && (!int.TryParse(codigoRaw, out var codigoHub) || codigoHub != 0))
                    throw new InvalidOperationException(
                        "No:Modo=Hub exige No:Codigo=0 (ou ausente). O hub e' SEMPRE o no 0 — codigo diferente " +
                        "ligaria o loop de push do hub contra si mesmo.");
                if (syncHabilitado)
                    throw new InvalidOperationException(
                        "No:Modo=Hub com Sync:Habilitado=true e' contradicao: o hub nao roda o loop de transporte " +
                        "(recebe via /api/sync/enviar e serve /api/sync/receber). Desligue Sync:Habilitado no hub.");
                return (modo, 0);

            case NoModo.Edge:
                if (!int.TryParse(codigoRaw, out var codigoEdge) || codigoEdge < 1)
                    throw new InvalidOperationException(
                        "No:Modo=Edge exige No:Codigo >= 1 (inteiro UNICO por no — define a faixa de Id e a " +
                        "origem do sync). Sem default silencioso.");
                if (syncHabilitado && string.IsNullOrWhiteSpace(urlCentral))
                    throw new InvalidOperationException(
                        "No:Modo=Edge com Sync:Habilitado=true exige Sync:UrlCentral. Sem a URL o transporte " +
                        "ficaria morto em silencio enquanto o backlog cresce.");
                return (modo, codigoEdge);

            case NoModo.StandaloneCloud:
                if (!int.TryParse(codigoRaw, out var codigoStandalone) || codigoStandalone < 1)
                    throw new InvalidOperationException(
                        "No:Modo=StandaloneCloud exige No:Codigo >= 1 (e' uma LOJA, nao o hub — mantem faixa de " +
                        "Id propria pra migracao futura pra Edge via bootstrap).");
                if (syncHabilitado)
                    throw new InvalidOperationException(
                        "No:Modo=StandaloneCloud com Sync:Habilitado=true e' contradicao: standalone nao replica. " +
                        "Pra ligar replicacao, converta pra Edge (exige bootstrap — ver plano fase 5).");
                return (modo, codigoStandalone);

            default:
                throw new InvalidOperationException($"No:Modo '{modo}' sem tratamento — bug no NoDeployment.");
        }
    }

    /// <summary>
    /// Leitura LENIENTE do modo pra dependencias que aceitam config nula (harness de teste,
    /// design-time do EF). Ausente/invalido = Edge (comportamento historico: captura ligada).
    /// O fail-fast de verdade e' o Resolver() no boot do Program.
    /// </summary>
    public static NoModo LerModoLeniente(IConfiguration? config)
        => Enum.TryParse<NoModo>(config?["No:Modo"], ignoreCase: true, out var m) ? m : NoModo.Edge;
}
