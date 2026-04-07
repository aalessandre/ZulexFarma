using ZulexPharma.Application.DTOs.TiposPagamento;

namespace ZulexPharma.Application.Interfaces;

public interface ITipoPagamentoService
{
    Task<List<TipoPagamentoListDto>> ListarAsync();
    Task<TipoPagamentoListDto> CriarAsync(TipoPagamentoFormDto dto);
    Task AtualizarAsync(long id, TipoPagamentoFormDto dto);
    Task<string> ExcluirAsync(long id);
}
