using ZulexPharma.Application.DTOs.Clientes;

namespace ZulexPharma.Application.Interfaces;

public interface IClienteService
{
    Task<List<ClienteListDto>> ListarAsync();
    Task<ClienteDetalheDto?> ObterAsync(long id);
    Task<ClienteListDto> CriarAsync(ClienteFormDto dto);
    Task AtualizarAsync(long id, ClienteFormDto dto);
    Task<string> ExcluirAsync(long id);
}
