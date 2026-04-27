namespace ZulexPharma.Application.DTOs.SelfCheckout;

public class ResultadoTesteConexaoDto
{
    public bool Ok { get; set; }
    public string Mensagem { get; set; } = string.Empty;

    /// <summary>Total de produtos visíveis na filial configurada (smoke check).</summary>
    public int? TotalProdutos { get; set; }
}
