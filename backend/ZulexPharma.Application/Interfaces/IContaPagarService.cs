using ZulexPharma.Application.DTOs.ContasPagar;

namespace ZulexPharma.Application.Interfaces;

public interface IContaPagarService
{
    Task<List<ContaPagarListDto>> ListarAsync();
    Task<ContaPagarListDto> CriarAsync(ContaPagarFormDto dto);
    Task<List<ContaPagarListDto>> CriarRecorrenteAsync(ContaPagarRecorrenteDto dto);
    Task AtualizarAsync(long id, ContaPagarFormDto dto);
    Task<string> ExcluirAsync(long id);
}
