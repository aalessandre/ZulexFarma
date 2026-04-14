using ZulexPharma.Application.DTOs.Compras;

namespace ZulexPharma.Application.Interfaces;

public interface ICompraService
{
    Task<List<CompraListDto>> ListarAsync(long? filialId = null, string? status = null, DateTime? dataInicio = null, DateTime? dataFim = null, string? filtroData = null);
    Task<CompraDetalheDto> ObterAsync(long id);
    Task<CompraDetalheDto> ImportarXmlAsync(string xmlConteudo, long filialId);
    Task<CompraProdutoDto> VincularProdutoAsync(VincularProdutoDto dto);
    Task<CompraProdutoDto> DesvincularProdutoAsync(long compraProdutoId);
    Task<CompraProdutoDto> AtualizarFracaoAsync(long compraProdutoId, short fracao);
    Task<BiparResult> BiparAsync(BiparRequest request);
    Task<CompraProdutoDto> AtualizarQtdeConfAsync(long compraProdutoId, decimal qtdeConferida);
    Task<CompraDetalheDto> ReVincularAsync(long compraId);
    Task<PrecificacaoResult> GerarPrecificacaoAsync(PrecificacaoRequest request);
    Task<int> AplicarPrecificacaoAsync(AplicarPrecificacaoRequest request);
    Task<int> SalvarSugestoesAsync(SalvarSugestaoRequest request);
    Task<DadosFinalizacaoDto> ObterDadosFinalizacaoAsync(long compraId);
    Task<FinalizarCompraResult> FinalizarAsync(FinalizarCompraRequest request);
    Task<string> ExcluirAsync(long id);

    // ── Conferência de Lotes ──────────────────────────────────────
    Task<ConferenciaLotesDto> ObterConferenciaLotesAsync(long compraId);
    Task SalvarConferenciaLotesAsync(long compraId, long usuarioId, SalvarConferenciaLotesDto dto);
}
