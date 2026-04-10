using ZulexPharma.Application.DTOs.Vendas;

namespace ZulexPharma.Application.Interfaces;

public interface IVendaService
{
    Task<List<VendaListDto>> ListarAsync(long? filialId = null, string? status = null);
    Task<VendaDetalheDto?> ObterAsync(long id);
    Task<VendaDetalheDto> CriarAsync(VendaFormDto dto);
    Task AtualizarAsync(long id, VendaFormDto dto);
    Task FinalizarAsync(long id);
    Task CancelarAsync(long id);
}
