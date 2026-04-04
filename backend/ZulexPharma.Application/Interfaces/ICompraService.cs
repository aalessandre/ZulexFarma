using ZulexPharma.Application.DTOs.Compras;

namespace ZulexPharma.Application.Interfaces;

public interface ICompraService
{
    Task<List<CompraListDto>> ListarAsync();
    Task<CompraDetalheDto> ObterAsync(long id);
    Task<CompraDetalheDto> ImportarXmlAsync(string xmlConteudo, long filialId);
    Task<CompraProdutoDto> VincularProdutoAsync(VincularProdutoDto dto);
    Task<CompraProdutoDto> DesvincularProdutoAsync(long compraProdutoId);
    Task<CompraProdutoDto> AtualizarFracaoAsync(long compraProdutoId, short fracao);
    Task<CompraDetalheDto> ReVincularAsync(long compraId);
    Task<PrecificacaoResult> GerarPrecificacaoAsync(PrecificacaoRequest request);
    Task<int> AplicarPrecificacaoAsync(AplicarPrecificacaoRequest request);
    Task<int> SalvarSugestoesAsync(SalvarSugestaoRequest request);
    Task<string> ExcluirAsync(long id);
}
