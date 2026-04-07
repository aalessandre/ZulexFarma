using ZulexPharma.Application.DTOs.PlanosContas;

namespace ZulexPharma.Application.Interfaces;

public interface IPlanoContaService
{
    Task<List<PlanoContaListDto>> ListarAsync();
    Task<PlanoContaListDto> CriarAsync(PlanoContaFormDto dto);
    Task AtualizarAsync(long id, PlanoContaFormDto dto);
    Task<string> ExcluirAsync(long id);
}
