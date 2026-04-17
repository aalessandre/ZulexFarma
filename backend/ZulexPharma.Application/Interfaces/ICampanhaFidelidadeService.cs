using ZulexPharma.Application.DTOs.Fidelidade;
using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.Interfaces;

public interface ICampanhaFidelidadeService
{
    Task<List<CampanhaFidelidadeListDto>> ListarAsync(TipoFidelidade? tipo = null);
    Task<CampanhaFidelidadeDetalheDto?> ObterAsync(long id);
    Task<CampanhaFidelidadeListDto> CriarAsync(CampanhaFidelidadeFormDto dto);
    Task AtualizarAsync(long id, CampanhaFidelidadeFormDto dto);
    Task<string> ExcluirAsync(long id);
}
