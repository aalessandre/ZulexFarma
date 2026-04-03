namespace ZulexPharma.Domain.Entities;

/// <summary>Localização física do produto por filial (ex: Prateleira Azul, Gôndola 5).</summary>
public class ProdutoLocal : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
}
