namespace ZulexPharma.API.Middleware;

/// <summary>
/// FASE 1b (achado ALTO da revisao adversarial): o token de MAQUINA (claim syncNode) e' assinado com
/// a mesma chave dos tokens humanos, entao passaria em QUALQUER endpoint [Authorize] puro do app
/// (ex.: /api/vendas, /api/clientes/pesquisar) — um no comprometido leria PII do hub inteiro.
/// Este gate confina o principal de no ao data plane do sync. Allowlist explicita e curta de
/// proposito: rota nova do sync que o no precise usar tem que ser adicionada AQUI conscientemente.
/// </summary>
public static class SyncNodeGate
{
    private static readonly PathString[] _permitidos =
    {
        new("/api/sync/handshake"),
        new("/api/sync/enviar"),
        new("/api/sync/receber"),
    };

    public static bool PermitidoParaNo(PathString path) =>
        _permitidos.Any(p => path.StartsWithSegments(p));

    public static bool EhTokenDeNo(System.Security.Claims.ClaimsPrincipal? user) =>
        user?.HasClaim("syncNode", "true") == true;
}
