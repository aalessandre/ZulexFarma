using ZulexPharma.Application.DTOs.Usuarios;

namespace ZulexPharma.Application.Interfaces;

public interface IUsuarioService
{
    Task<List<UsuarioListDto>> ListarAsync();
    Task<UsuarioListDto> CriarAsync(UsuarioFormDto dto);
    Task AtualizarAsync(long id, UsuarioFormDto dto);
    Task ExcluirAsync(long id);
}
