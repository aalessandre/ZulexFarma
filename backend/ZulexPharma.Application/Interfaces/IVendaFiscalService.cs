using ZulexPharma.Application.DTOs.Fiscal;

namespace ZulexPharma.Application.Interfaces;

/// <summary>
/// Emissão fiscal unificada: NFe modelo 55 e NFC-e modelo 65.
/// Substitui INfeService + NfceService. Opera sobre Venda + VendaFiscal + VendaItemFiscal.
/// </summary>
public interface IVendaFiscalService
{
    // ── Listagem / leitura ──────────────────────────────────────
    Task<List<VendaFiscalListDto>> ListarAsync(long? filialId = null);
    Task<VendaFiscalDetalheDto> ObterAsync(long vendaId);

    // ── NFe modelo 55 (fluxo: cria rascunho → emite) ────────────
    Task<VendaFiscalListDto> CriarRascunhoNfeAsync(VendaFiscalFormDto dto);
    Task AtualizarRascunhoNfeAsync(long vendaId, VendaFiscalFormDto dto);
    Task<string> ExcluirRascunhoNfeAsync(long vendaId);
    Task<VendaFiscalEmissaoResult> EmitirNfeAsync(long vendaId);

    // ── NFC-e modelo 65 (fluxo: emite direto a partir de venda finalizada) ──
    /// <summary>
    /// Emite NFC-e a partir de venda finalizada. Série e número de partida vêm
    /// da Configuracao (<c>fiscal.nfce.serie</c> e <c>fiscal.nfce.numero.atual</c>).
    /// O número é reservado via <see cref="ISequenciaCentralService"/> com lock
    /// transacional — protege contra colisão entre N caixas + N terminais
    /// self-checkout emitindo simultaneamente sob a mesma série.
    /// </summary>
    Task<VendaFiscalEmissaoResult> EmitirNfceAsync(long vendaId);

    // ── Eventos (cancelamento, CC-e, inutilização) ──────────────
    Task<VendaFiscalEventoResult> CancelarAsync(long vendaId, string justificativa);
    Task<VendaFiscalEventoResult> CartaCorrecaoAsync(long vendaId, string textoCorrecao);
    Task<VendaFiscalEventoResult> InutilizarAsync(long filialId, int serie, int numInicial, int numFinal, string justificativa);
}
