namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Fabricante de produtos. Entidade simples com apenas Nome.
/// </summary>
public class Fabricante : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public decimal DescontoMinimo { get; set; } = 0;
    public decimal DescontoMaximo { get; set; } = 0;
    public decimal DescontoMaximoComSenha { get; set; } = 0;
}
