using ZulexPharma.Application.DTOs.SelfCheckout;

namespace ZulexPharma.Application.Interfaces;

/// <summary>
/// Orquestra o ciclo de venda do Self-Checkout: criar venda + itens + snapshot fiscal,
/// registrar pagamento, confirmar (atendente) e disparar emissão da NFC-e.
/// Cria fila de conciliação de estoque (RN-22 — produto totalmente externo).
/// </summary>
public interface ISelfCheckoutVendaService
{
    /// <summary>
    /// Cria a venda kiosk (Origem=SelfCheckout) com itens + snapshot fiscal pré-resolvido
    /// via <see cref="IErpConnector.ObterFiscalAsync"/>. A venda fica em <c>Status=Aberta</c>
    /// e <c>PagamentoRecebido=false</c>.
    /// </summary>
    Task<IniciarVendaKioskResultDto> IniciarAsync(long filialId, IniciarVendaKioskDto input, CancellationToken ct = default);

    /// <summary>
    /// Cliente kiosk informa a forma escolhida. Cria <c>VendaPagamento</c> com o tipo
    /// vinculado pela modalidade (PIX → VendaPix, Cartao → VendaCartao). Venda continua
    /// pendente (PagamentoRecebido=false) até o atendente confirmar.
    /// </summary>
    Task RegistrarPagamentoAsync(long vendaId, RegistrarPagamentoKioskDto input, CancellationToken ct = default);

    /// <summary>
    /// Atendente confirma o recebimento. Marca PagamentoRecebido=true, dispara
    /// <see cref="IVendaFiscalService.EmitirNfceAsync"/>, finaliza venda (Status=Finalizada)
    /// e cria registros em <c>SelfCheckoutConciliacoesEstoque</c> para baixa futura no ERP origem.
    /// Em caso de NFC-e rejeitada, retorna o erro mas mantém a venda pendente.
    /// </summary>
    Task<ConfirmarVendaKioskResultDto> ConfirmarPagamentoAsync(long vendaId, CancellationToken ct = default);

    /// <summary>Cancela a venda (cliente desiste antes da NFC-e ou atendente recusa).</summary>
    Task CancelarAsync(long vendaId, string? motivo, CancellationToken ct = default);

    /// <summary>Status consumível pelo terminal kiosk (polling).</summary>
    Task<StatusVendaKioskDto?> ObterStatusKioskAsync(long vendaId, CancellationToken ct = default);

    /// <summary>
    /// Lista vendas kiosk aguardando confirmação manual de pagamento na filial.
    /// Status = Aberta + PagamentoRecebido = false + tem VendaPagamento informado.
    /// </summary>
    Task<List<PagamentoPendenteDto>> ListarPagamentosPendentesAsync(long filialId, CancellationToken ct = default);
}
