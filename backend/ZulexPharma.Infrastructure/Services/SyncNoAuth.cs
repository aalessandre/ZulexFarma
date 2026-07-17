using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public enum HandshakeResultado
{
    Ok,
    CredencialInvalida,   // no desconhecido OU chave errada (resposta unica: nao vazar existencia do no)
    NoInativo,            // Suspenso | Desativado | RebootstrapNecessario — precisa de acao no painel
    Gemeo                 // InstanciaUid diferente do registrado -> segundo servidor com o mesmo codigo
}

/// <summary>
/// Autenticacao de NO (fase 1): credencial POR NO (chave aleatoria, hash no hub) + anti-gemeo por
/// InstanciaUid. Substitui o login SISTEMA como credencial de maquina do transporte — a senha diaria
/// derivada de segredo compartilhado dava token ADMIN a qualquer no comprometido (Codex P0.2).
/// </summary>
public static class SyncNoAuth
{
    /// <summary>Gera a chave em claro do no (64 hex, 256 bits). Exibida UMA vez no cadastro/rotacao.</summary>
    public static string GerarChave() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower();

    public static string HashChave(string chave) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chave))).ToLower();

    /// <summary>
    /// Valida o handshake de um no. Efeitos colaterais (commitados aqui): primeiro handshake CRAVA o
    /// InstanciaUid; handshakes validos atualizam VersaoApp. Comparacao de hash em tempo constante.
    /// </summary>
    public static async Task<(HandshakeResultado Resultado, SyncNo? No)> ValidarHandshakeAsync(
        AppDbContext db, int noCodigo, Guid instanciaUid, string chave, string? versaoApp, CancellationToken ct = default)
    {
        var no = await db.SyncNos.FirstOrDefaultAsync(n => n.NoCodigo == noCodigo, ct);
        if (no == null) return (HandshakeResultado.CredencialInvalida, null);

        var hashInformado = HashChave(chave ?? "");
        var chaveOk = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(hashInformado), Encoding.ASCII.GetBytes(no.ChaveHash));
        if (!chaveOk) return (HandshakeResultado.CredencialInvalida, no);

        if (no.Status is "Suspenso" or "Desativado" or "RebootstrapNecessario")
            return (HandshakeResultado.NoInativo, no);

        if (no.InstanciaUid == null)
        {
            // Primeiro handshake da instalacao: crava a identidade fisica (anti-gemeo dali em diante).
            no.InstanciaUid = instanciaUid;
            if (no.Status == "Provisionando") no.Status = "Ativo";
            Log.Information("Sync: no {No} cravou InstanciaUid {Uid} no primeiro handshake.", noCodigo, instanciaUid);
        }
        else if (no.InstanciaUid != instanciaUid)
        {
            // DOIS servidores com o mesmo NoCodigo = corrupcao silenciosa (colisao de faixa de Id +
            // anti-eco cego). Falha RUIDOSA — reinstalacao legitima passa pelo "resetar-instancia".
            Log.Error("Sync: NO GEMEO detectado! NoCodigo={No}, registrado={Reg}, tentou={Tentou}.",
                noCodigo, no.InstanciaUid, instanciaUid);
            return (HandshakeResultado.Gemeo, no);
        }

        no.VersaoApp = versaoApp;
        await db.SaveChangesAsync(ct);
        return (HandshakeResultado.Ok, no);
    }

    /// <summary>Le (ou cria na primeira vez) o InstanciaUid persistente deste deployment.</summary>
    public static async Task<Guid> ObterOuCriarInstanciaUidAsync(AppDbContext db, CancellationToken ct = default)
    {
        const string chave = "sync.instancia.uid";
        var estado = await db.SyncEstadoLocal.FirstOrDefaultAsync(e => e.Chave == chave, ct);
        if (estado != null && Guid.TryParse(estado.Valor, out var existente)) return existente;

        var novo = Guid.NewGuid();
        if (estado == null)
            db.SyncEstadoLocal.Add(new SyncEstadoLocal { Chave = chave, Valor = novo.ToString() });
        else
        {
            estado.Valor = novo.ToString();
            estado.AtualizadoEm = DataHoraHelper.Agora();
        }
        await db.SaveChangesAsync(ct);
        return novo;
    }
}
