namespace ZulexPharma.Domain.Entities.SelfCheckout;

/// <summary>
/// Terminal físico do Self-Checkout. Uma filial pode ter N terminais.
/// Identifica de onde veio cada venda (Venda.SelfCheckoutTerminalId).
/// </summary>
public class SelfCheckoutTerminal : BaseEntity
{
    public long FilialId { get; set; }
    public Filial? Filial { get; set; }

    /// <summary>Número do terminal dentro da filial (1, 2, 3...). Único por filial.</summary>
    public int Numero { get; set; }

    /// <summary>Apelido amigável para exibição (ex: "Caixa Express 1").</summary>
    public string? Apelido { get; set; }

    /// <summary>Última atividade registrada (heartbeat ou venda). Para detectar terminal offline.</summary>
    public DateTime? UltimaAtividade { get; set; }
}
