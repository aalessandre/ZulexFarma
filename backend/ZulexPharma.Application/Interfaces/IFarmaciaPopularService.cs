using ZulexPharma.Application.DTOs.FarmaciaPopular;

namespace ZulexPharma.Application.Interfaces;

/// <summary>
/// Orquestra as fases do Farmácia Popular. Invocado pelo fluxo de finalização
/// da venda (VendaService.FinalizarAsync), não por endpoint REST (MVP).
/// Cada método persiste XMLs, atualiza Status/FaseAtual e retorna o resultado.
/// </summary>
public interface IFarmaciaPopularService
{
    /// <summary>
    /// Fase 1 — invoca gbasmsb + SOAP.executarSolicitacao. Atualiza VendaFarmaciaPopular com retorno.
    /// Se dnaEstacaoOverride for fornecido (uso diagnóstico), pula gbasmsb e envia o DNA passado.
    /// </summary>
    Task<SolicitacaoRetornoDto> SolicitarAsync(long vendaId, string? dnaEstacaoOverride = null, CancellationToken ct = default);

    /// <summary>Fase 2 — SOAP.confirmarSolicitacao (chamado após NFC-e autorizada).</summary>
    Task<ConfirmacaoRetornoDto> ConfirmarAsync(long vendaId, string nuCupomFiscal, CancellationToken ct = default);

    /// <summary>Fase 3 — SOAP.receberMedicamento. Marca venda como Efetivada.</summary>
    Task<ConfirmacaoRetornoDto> ReceberAsync(long vendaId, CancellationToken ct = default);

    /// <summary>Fase E — SOAP.estornar. Chamado quando venda FP efetivada é cancelada.</summary>
    Task<ConfirmacaoRetornoDto> EstornarAsync(long vendaId, string motivo, CancellationToken ct = default);
}
