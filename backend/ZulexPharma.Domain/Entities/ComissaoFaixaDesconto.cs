namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Faixa de comissão por desconto aplicável a classificações de produto
/// (Grupo Principal, Grupo, SubGrupo, Seção).
/// Quanto maior o desconto concedido, menor a comissão do vendedor.
/// </summary>
public class ComissaoFaixaDesconto : BaseEntity
{
    /// <summary>Tipo da entidade dona: "GrupoPrincipal", "GrupoProduto", "SubGrupo", "Secao"</summary>
    public string TipoEntidade { get; set; } = string.Empty;

    /// <summary>Id da classificação dona.</summary>
    public long EntidadeId { get; set; }

    /// <summary>Percentual de desconto inicial da faixa (ex: 0.00, 10.01).</summary>
    public decimal DescontoInicial { get; set; }

    /// <summary>Percentual de desconto final da faixa (ex: 10.00, 20.00).</summary>
    public decimal DescontoFinal { get; set; }

    /// <summary>Percentual de comissão aplicável nesta faixa.</summary>
    public decimal ComissaoPercentual { get; set; }

    /// <summary>Ordem da faixa (0, 1, 2...).</summary>
    public int Ordem { get; set; }
}
