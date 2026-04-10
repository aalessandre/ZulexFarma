using ZulexPharma.Application.DTOs.Adquirentes;

namespace ZulexPharma.Application.Interfaces;

public interface IAdquirenteService
{
    Task<List<AdquirenteListDto>> ListarAsync();
    Task<AdquirenteDetalheDto?> ObterAsync(long id);
    Task<AdquirenteDetalheDto> CriarAsync(AdquirenteFormDto dto);
    Task AtualizarAsync(long id, AdquirenteFormDto dto);
    Task<string> ExcluirAsync(long id);
}
