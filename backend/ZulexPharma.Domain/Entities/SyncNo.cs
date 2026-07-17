namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Registro EXPLICITO de nos da replicacao (fase 1 do plano). Vive no HUB e e' INFRA (nao herda
/// BaseEntity -> nao replica). E' a fonte de: credencial por no (ChaveHash), deteccao de no gemeo
/// (InstanciaUid), escopo do PULL (SyncNoFilial) e — na fase 5 — retencao por ACK (UltimoAckSeq).
/// Sem ele, o conjunto de nos era descoberto por OBSERVACAO (a raiz da retencao revertida).
/// </summary>
public class SyncNo
{
    /// <summary>Codigo do no (mesmo espaco do No:Codigo/faixa de Id). PK, atribuido no cadastro, NUNCA reutilizado.</summary>
    public int NoCodigo { get; set; }

    /// <summary>Rotulo amigavel ("Loja Centro").</summary>
    public string? Nome { get; set; }

    /// <summary>
    /// Identidade da INSTALACAO fisica — anti-gemeo. Null ate' o 1o handshake (o primeiro crava);
    /// handshake com uid DIFERENTE = segundo servidor com o mesmo codigo -> 409, nao sobe.
    /// Reinstalacao legitima: admin usa "resetar-instancia" no painel.
    /// </summary>
    public Guid? InstanciaUid { get; set; }

    /// <summary>Provisionando | Ativo | Suspenso | RebootstrapNecessario | Desativado.</summary>
    public string Status { get; set; } = "Provisionando";

    /// <summary>SHA256 (hex minusculo) da chave do no. A chave em claro so' aparece 1x, no cadastro/rotacao.</summary>
    public string ChaveHash { get; set; } = "";

    /// <summary>Maior SeqEntrega confirmada pelo no (fase 2 preenche; fase 5 usa pra retencao).</summary>
    public long UltimoAckSeq { get; set; }

    public DateTime? UltimoPushEm { get; set; }
    public DateTime? UltimoPullEm { get; set; }
    public string? VersaoApp { get; set; }
    public DateTime CriadoEm { get; set; } = Helpers.DataHoraHelper.Agora();

    public ICollection<SyncNoFilial> Filiais { get; set; } = new List<SyncNoFilial>();
}

/// <summary>
/// Escopo AUTORIZADO do pull por no: quais filiais-donas este no pode receber (alem do GLOBAL).
/// Fase 1: o /receber passa a derivar o escopo DAQUI (server-side) e ignora o ?filiais= do cliente.
/// Sem FK pra Filiais de proposito (a filial pode ainda nao existir no hub durante o provisionamento).
/// </summary>
public class SyncNoFilial
{
    public int NoCodigo { get; set; }
    public long FilialId { get; set; }
}
