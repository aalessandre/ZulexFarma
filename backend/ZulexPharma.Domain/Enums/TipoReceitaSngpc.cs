namespace ZulexPharma.Domain.Enums;

/// <summary>
/// Tipo de receituário para SNGPC.
/// </summary>
public enum TipoReceitaSngpc
{
    /// <summary>Notificação A (amarela) — Entorpecentes (A1, A2, A3). Validade 30 dias, numeração Anvisa.</summary>
    NotificacaoA = 1,

    /// <summary>Notificação B1 (azul) — Psicotrópicos. Validade 30 dias.</summary>
    NotificacaoB1 = 2,

    /// <summary>Notificação B2 (azul) — Psicotrópicos anorexígenos. Validade 30 dias.</summary>
    NotificacaoB2 = 3,

    /// <summary>Receita de Controle Especial C1 (branca 2 vias). Validade 30 dias.</summary>
    ReceitaC1 = 4,

    /// <summary>Notificação C2 (branca especial) — Retinóides sistêmicos. Validade 30 dias.</summary>
    NotificacaoC2 = 5,

    /// <summary>Notificação C4 (branca especial) — Imunossupressores. Validade 30 dias.</summary>
    NotificacaoC4 = 6,

    /// <summary>Notificação C5 (branca especial) — Anabolizantes. Validade 30 dias.</summary>
    NotificacaoC5 = 7,

    /// <summary>Receita de Antimicrobiano (branca 2 vias). Validade 10 dias (RDC 20/2011).</summary>
    Antimicrobiano = 8
}
