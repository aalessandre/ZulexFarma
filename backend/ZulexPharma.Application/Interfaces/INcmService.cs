using ZulexPharma.Application.DTOs.Ncm;

namespace ZulexPharma.Application.Interfaces;

public interface INcmService
{
    Task<List<NcmListDto>> ListarAsync();
    Task<NcmDetalheDto> ObterAsync(long id);
    Task<NcmListDto> CriarAsync(NcmFormDto dto);
    Task AtualizarAsync(long id, NcmFormDto dto);
    Task<string> ExcluirAsync(long id);
}
