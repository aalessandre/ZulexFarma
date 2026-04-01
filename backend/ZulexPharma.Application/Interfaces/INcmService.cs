using ZulexPharma.Application.DTOs.Ncm;

namespace ZulexPharma.Application.Interfaces;

public interface INcmService
{
    Task<List<NcmListDto>> ListarAsync(string? busca = null);
    Task<NcmDetalheDto> ObterAsync(long id);
    Task<NcmListDto> CriarAsync(NcmFormDto dto);
    Task AtualizarAsync(long id, NcmFormDto dto);
    Task<string> ExcluirAsync(long id);
    Task<object> ImportarCsvAsync(string caminhoArquivo);
}
