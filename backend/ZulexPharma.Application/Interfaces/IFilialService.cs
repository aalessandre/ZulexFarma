using ZulexPharma.Application.DTOs.Filiais;

namespace ZulexPharma.Application.Interfaces;

public interface IFilialService
{
    Task<List<FilialListDto>> ListarAsync();
    Task<FilialListDto> CriarAsync(FilialFormDto dto);
    Task AtualizarAsync(long id, FilialFormDto dto);
    /// <summary>Retorna "excluido" se deletou fisicamente ou "desativado" se apenas inativou.</summary>
    Task<string> ExcluirAsync(long id);
}
