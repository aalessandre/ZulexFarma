using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.Entregas;

public class EntregaAgendaSlotDto
{
    public long Id { get; set; }
    public int? DiaSemana { get; set; }
    public TurnoEntrega Turno { get; set; }
    public bool EhFeriado { get; set; }
    public long PerfilId { get; set; }
    public string? PerfilNome { get; set; }
}

public class EntregaAgendaSaveDto
{
    public long FilialId { get; set; }
    public List<EntregaAgendaSlotFormDto> Slots { get; set; } = new();
}

public class EntregaAgendaSlotFormDto
{
    public int? DiaSemana { get; set; }
    public TurnoEntrega Turno { get; set; }
    public bool EhFeriado { get; set; }
    public long PerfilId { get; set; }
}
