namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Perfil de preço de entrega. Agrupa um conjunto de faixas (raio × valor).
/// Exemplos: "DIURNO ÚTIL", "NOTURNO", "FIM DE SEMANA", "FERIADO".
/// Cada slot da agenda (dia × turno × feriado) aponta para um perfil.
/// </summary>
public class EntregaPerfil : BaseEntity
{
    public long FilialId { get; set; }
    public Filial? Filial { get; set; }

    /// <summary>Nome único por filial, UPPERCASE (ex: "DIURNO ÚTIL").</summary>
    public string Nome { get; set; } = string.Empty;

    public ICollection<EntregaFaixa> Faixas { get; set; } = new List<EntregaFaixa>();
}
