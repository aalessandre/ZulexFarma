using ZulexPharma.Application.DTOs.Fidelidade;

namespace ZulexPharma.Application.Interfaces;

public interface IPremioFidelidadeService
{
    Task<List<PremioFidelidadeListDto>> ListarAsync();
    Task<PremioFidelidadeListDto?> ObterAsync(long id);
    Task<PremioFidelidadeListDto> CriarAsync(PremioFidelidadeFormDto dto);
    Task AtualizarAsync(long id, PremioFidelidadeFormDto dto);
    Task<string> ExcluirAsync(long id);
}
