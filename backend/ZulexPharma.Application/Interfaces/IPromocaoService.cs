using ZulexPharma.Application.DTOs.Promocoes;

namespace ZulexPharma.Application.Interfaces;

public interface IPromocaoService
{
    Task<List<PromocaoListDto>> ListarAsync();
    Task<PromocaoDetalheDto?> ObterAsync(long id);
    Task<PromocaoListDto> CriarAsync(PromocaoFormDto dto);
    Task AtualizarAsync(long id, PromocaoFormDto dto);
    Task<string> ExcluirAsync(long id);
}
