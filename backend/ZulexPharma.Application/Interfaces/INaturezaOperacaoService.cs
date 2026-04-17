using ZulexPharma.Application.DTOs.Fiscal;

namespace ZulexPharma.Application.Interfaces;

public interface INaturezaOperacaoService
{
    Task<List<NaturezaOperacaoListDto>> ListarAsync();
    Task<NaturezaOperacaoDetalheDto> ObterAsync(long id);
    Task<NaturezaOperacaoListDto> CriarAsync(NaturezaOperacaoFormDto dto);
    Task AtualizarAsync(long id, NaturezaOperacaoFormDto dto);
    Task<string> ExcluirAsync(long id);
}
