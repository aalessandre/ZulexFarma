using ZulexPharma.Application.DTOs.Grade;

namespace ZulexPharma.Application.Interfaces;

public interface IAtributoVariacaoService
{
    Task<List<AtributoVariacaoDto>> ListarAsync();
    Task<AtributoVariacaoDto?> ObterAsync(long id);
    Task<AtributoVariacaoDto> CriarAsync(AtributoVariacaoFormDto dto);
    Task AtualizarAsync(long id, AtributoVariacaoFormDto dto);
    Task<string> ExcluirAsync(long id);
}
