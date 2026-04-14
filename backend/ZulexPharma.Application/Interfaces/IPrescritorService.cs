using ZulexPharma.Application.DTOs.Prescritores;

namespace ZulexPharma.Application.Interfaces;

public interface IPrescritorService
{
    Task<List<PrescritorListDto>> ListarAsync();
    Task<PrescritorListDto> CriarAsync(PrescritorFormDto dto);
    Task AtualizarAsync(long id, PrescritorFormDto dto);
    Task<string> ExcluirAsync(long id);
    Task<PrescritorListDto?> ObterAsync(long id);
}
