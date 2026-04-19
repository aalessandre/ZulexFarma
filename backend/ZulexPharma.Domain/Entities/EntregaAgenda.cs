using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Agenda que mapeia (DiaSemana × Turno × EhFeriado) → Perfil de preço.
///
/// Regras (RN-05, RN-06):
///   - Se EhFeriado = false, DiaSemana é obrigatório (1=Domingo..7=Sábado).
///   - Se EhFeriado = true, DiaSemana é null (feriado independe do dia da semana).
///   - Unique: (FilialId, DiaSemana, Turno, EhFeriado).
///   - Cobertura obrigatória: 7 × 2 (normais) + 1 × 2 (feriado) = 16 linhas por filial.
/// </summary>
public class EntregaAgenda : BaseEntity
{
    public long FilialId { get; set; }
    public Filial? Filial { get; set; }

    /// <summary>1 = Domingo, 2 = Segunda, ... 7 = Sábado. Null se EhFeriado=true.</summary>
    public int? DiaSemana { get; set; }

    public TurnoEntrega Turno { get; set; }

    public bool EhFeriado { get; set; }

    public long PerfilId { get; set; }
    public EntregaPerfil? Perfil { get; set; }
}
