using ZulexPharma.Application.DTOs.HierarquiaComissoes;

namespace ZulexPharma.Application.Interfaces;

public interface IHierarquiaComissaoService
{
    Task<List<HierarquiaComissaoListDto>> ListarAsync();
    Task<HierarquiaComissaoDetalheDto?> ObterAsync(long id);
    Task<HierarquiaComissaoListDto> CriarAsync(HierarquiaComissaoFormDto dto);
    Task AtualizarAsync(long id, HierarquiaComissaoFormDto dto);
    Task<string> ExcluirAsync(long id);
}
