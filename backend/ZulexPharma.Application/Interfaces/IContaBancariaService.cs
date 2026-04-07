using ZulexPharma.Application.DTOs.ContasBancarias;

namespace ZulexPharma.Application.Interfaces;

public interface IContaBancariaService
{
    Task<List<ContaBancariaListDto>> ListarAsync();
    Task<ContaBancariaListDto> CriarAsync(ContaBancariaFormDto dto);
    Task AtualizarAsync(long id, ContaBancariaFormDto dto);
    Task<string> ExcluirAsync(long id);
}
