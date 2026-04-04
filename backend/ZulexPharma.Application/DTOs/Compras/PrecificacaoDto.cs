namespace ZulexPharma.Application.DTOs.Compras;

public class PrecificacaoRequest
{
    public long FilialId { get; set; }
    public List<long> CompraIds { get; set; } = new();
}

public class PrecificacaoItem
{
    public long ProdutoId { get; set; }
    public long ProdutoDadosId { get; set; }
    public long CompraProdutoId { get; set; }
    public string ProdutoNome { get; set; } = "";
    public string? Ean { get; set; }
    public string? FabricanteNome { get; set; }

    // Custo Compra
    public decimal CustoCompraAnterior { get; set; }
    public decimal CustoCompraAtual { get; set; }
    public decimal VarCustoCompraPercent { get; set; }

    // Custo Médio
    public decimal CustoMedioAnterior { get; set; }
    public decimal CustoMedioAtual { get; set; }
    public decimal VarCustoMedioPercent { get; set; }

    // Preço Venda
    public decimal PrecoVendaAtual { get; set; }
    public decimal SugestaoVendaCustoCompra { get; set; }
    public decimal SugestaoVendaCustoMedio { get; set; }
    public decimal NovoPrecoVenda { get; set; }

    // PMC
    public decimal PmcNota { get; set; }
    public decimal PmcAbcFarma { get; set; }

    // Config
    public string FormacaoPreco { get; set; } = "MARKUP";
    public decimal Markup { get; set; }
    public decimal ProjecaoLucro { get; set; }
    public decimal Quantidade { get; set; }
}

public class PrecificacaoResult
{
    public int TotalProdutos { get; set; }
    public List<PrecificacaoItem> Itens { get; set; } = new();
}

public class AplicarPrecificacaoRequest
{
    public long FilialId { get; set; }
    public string? NomeUsuario { get; set; }
    public List<AplicarPrecificacaoItem> Itens { get; set; } = new();
}

public class AplicarPrecificacaoItem
{
    public long ProdutoDadosId { get; set; }
    public long ProdutoId { get; set; }
    public long CompraProdutoId { get; set; }
    public decimal NovoPrecoVenda { get; set; }
    public decimal NovoMarkup { get; set; }
    public decimal NovaProjecaoLucro { get; set; }
    public decimal NovoCustoCompra { get; set; }
    public decimal NovoCustoMedio { get; set; }
    public decimal NovoPmc { get; set; }
}
