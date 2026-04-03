using ZulexPharma.Application.DTOs.Fiscal;

namespace ZulexPharma.Application.Interfaces;

public interface IIcmsUfService
{
    Task<List<IcmsUfListDto>> ListarAsync();
    Task<IcmsUfListDto> ObterAsync(long id);
    Task<IcmsUfListDto> CriarAsync(IcmsUfFormDto dto);
    Task AtualizarAsync(long id, IcmsUfFormDto dto);
    Task<string> ExcluirAsync(long id);
}
