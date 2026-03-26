using ZulexPharma.Application.DTOs.Grupos;

namespace ZulexPharma.Application.Interfaces;

public interface IGrupoService
{
    Task<List<GrupoListDto>> ListarAsync();
    Task<GrupoListDto> CriarAsync(GrupoFormDto dto);
    Task AtualizarAsync(long id, GrupoFormDto dto);
    Task ExcluirAsync(long id);
    Task<List<PermissaoDto>> ListarPermissoesAsync(long grupoId);
    Task SalvarPermissoesAsync(long grupoId, SalvarPermissoesDto dto);
}
