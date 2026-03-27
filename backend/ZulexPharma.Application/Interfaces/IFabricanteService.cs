using ZulexPharma.Application.DTOs.Fabricantes;

namespace ZulexPharma.Application.Interfaces;

public interface IFabricanteService
{
    Task<List<FabricanteListDto>> ListarAsync();
    Task<FabricanteListDto> CriarAsync(FabricanteFormDto dto);
    Task AtualizarAsync(long id, FabricanteFormDto dto);
    Task<string> ExcluirAsync(long id);
}
