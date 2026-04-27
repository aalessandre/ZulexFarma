using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities.SelfCheckout;

/// <summary>
/// Chamado disparado pelo cliente no terminal Self-Checkout.
/// Tela admin mostra os abertos em tempo real para o atendente atender.
/// </summary>
public class SelfCheckoutChamadoAtendente : BaseEntity
{
    public long TerminalId { get; set; }
    public SelfCheckoutTerminal? Terminal { get; set; }

    public MotivoChamadoAtendente Motivo { get; set; }

    /// <summary>Mensagem opcional com mais contexto (ex: EAN não localizado).</summary>
    public string? Mensagem { get; set; }

    public DateTime? AtendidoEm { get; set; }

    public long? AtendidoPorColaboradorId { get; set; }
    public Colaborador? AtendidoPorColaborador { get; set; }
}
