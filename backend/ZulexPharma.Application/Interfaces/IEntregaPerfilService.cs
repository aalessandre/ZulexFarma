using ZulexPharma.Application.DTOs.Entregas;

namespace ZulexPharma.Application.Interfaces;

public interface IEntregaPerfilService
{
    Task<List<EntregaPerfilDto>> ListarAsync(long filialId);
    Task<EntregaPerfilDto?> ObterAsync(long id);
    Task<EntregaPerfilDto> CriarAsync(EntregaPerfilFormDto dto);
    Task AtualizarAsync(long id, EntregaPerfilFormDto dto);
    Task<string> ExcluirAsync(long id);
}
