using ZulexPharma.Application.DTOs.Entregas;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.Interfaces;

public interface IEntregaAgendaService
{
    Task<List<EntregaAgendaSlotDto>> ListarAsync(long filialId);
    Task SalvarAsync(EntregaAgendaSaveDto dto);

    /// <summary>Resolve o perfil aplicável para a filial na data/hora informada.</summary>
    /// <param name="ehFeriado">Indica se a data é feriado (calculado externamente via IFeriadoService).</param>
    Task<EntregaPerfil?> ResolverPerfilAsync(long filialId, DateTime dataHora, bool ehFeriado);
}
