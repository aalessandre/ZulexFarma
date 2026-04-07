using ZulexPharma.Application.DTOs.PreVendas;

namespace ZulexPharma.Application.Interfaces;

public interface IPreVendaService
{
    Task<List<PreVendaListDto>> ListarAsync(long? filialId = null);
    Task<PreVendaDetalheDto?> ObterAsync(long id);
    Task<PreVendaDetalheDto> CriarAsync(PreVendaFormDto dto);
    Task AtualizarAsync(long id, PreVendaFormDto dto);
    Task FinalizarAsync(long id);
    Task CancelarAsync(long id);
}
