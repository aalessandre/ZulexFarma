using ZulexPharma.Application.DTOs.HierarquiaDescontos;

namespace ZulexPharma.Application.Interfaces;

public interface IHierarquiaDescontoService
{
    Task<List<HierarquiaDescontoListDto>> ListarAsync();
    Task<HierarquiaDescontoDetalheDto?> ObterAsync(long id);
    Task<HierarquiaDescontoListDto> CriarAsync(HierarquiaDescontoFormDto dto);
    Task AtualizarAsync(long id, HierarquiaDescontoFormDto dto);
    Task<string> ExcluirAsync(long id);
}
