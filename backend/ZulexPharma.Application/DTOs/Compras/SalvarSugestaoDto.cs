namespace ZulexPharma.Application.DTOs.Compras;

public class SalvarSugestaoRequest
{
    public List<SalvarSugestaoItem> Itens { get; set; } = new();
}

public class SalvarSugestaoItem
{
    public long CompraProdutoId { get; set; }
    public decimal SugestaoVenda { get; set; }
    public decimal SugestaoMarkup { get; set; }
    public decimal SugestaoProjecao { get; set; }
    public decimal SugestaoCustoMedio { get; set; }
}
