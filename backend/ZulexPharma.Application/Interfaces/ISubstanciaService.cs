using ZulexPharma.Application.DTOs.Substancias;

namespace ZulexPharma.Application.Interfaces;

public interface ISubstanciaService
{
    Task<List<SubstanciaListDto>> ListarAsync();
    Task<SubstanciaListDto> CriarAsync(SubstanciaFormDto dto);
    Task AtualizarAsync(long id, SubstanciaFormDto dto);
    Task<string> ExcluirAsync(long id);
}
