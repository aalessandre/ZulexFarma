using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>Desconto especial por agrupador ou produto específico.</summary>
public class ClienteDesconto
{
    public long Id { get; set; }
    public long ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    /// <summary>Se ProdutoId != null, é desconto por produto. Senão, é por agrupador.</summary>
    public long? ProdutoId { get; set; }
    public TipoAgrupador? TipoAgrupador { get; set; }
    public long? AgrupadorId { get; set; }
    public string AgrupadorOuProdutoNome { get; set; } = string.Empty;
    public decimal DescontoMinimo { get; set; }
    public decimal DescontoMaxSemSenha { get; set; }
    public decimal DescontoMaxComSenha { get; set; }
}
