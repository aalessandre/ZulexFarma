using ZulexPharma.Application.DTOs.Entregas;

namespace ZulexPharma.Application.Interfaces;

public interface IEntregaFaixaService
{
    Task<List<EntregaFaixaDto>> ListarAsync(long filialId);
    Task<EntregaFaixaDto> CriarAsync(EntregaFaixaFormDto dto);
    Task AtualizarAsync(long id, EntregaFaixaFormDto dto);
    Task ExcluirAsync(long id);
}
