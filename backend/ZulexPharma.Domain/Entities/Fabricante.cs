namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Fabricante de produtos. Entidade simples com apenas Nome.
/// </summary>
public class Fabricante : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
}
