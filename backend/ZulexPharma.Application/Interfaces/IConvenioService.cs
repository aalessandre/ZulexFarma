using ZulexPharma.Application.DTOs.Convenios;

namespace ZulexPharma.Application.Interfaces;

public interface IConvenioService
{
    Task<List<ConvenioListDto>> ListarAsync();
    Task<ConvenioDetalheDto?> ObterAsync(long id);
    Task<ConvenioListDto> CriarAsync(ConvenioFormDto dto);
    Task AtualizarAsync(long id, ConvenioFormDto dto);
    Task<string> ExcluirAsync(long id);
}
