using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.Vendas;

public class VendaListDto
{
    public long Id { get; set; }
    public string? Codigo { get; set; }
    public string? NrCesta { get; set; }
    public string? ClienteNome { get; set; }
    public string? ColaboradorNome { get; set; }
    public string? TipoPagamentoNome { get; set; }
    public decimal TotalLiquido { get; set; }
    public int TotalItens { get; set; }
    public VendaStatus Status { get; set; }
    public string StatusDescricao { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}

public class VendaDetalheDto
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public long? ClienteId { get; set; }
    public string? ClienteNome { get; set; }
    public long? ColaboradorId { get; set; }
    public string? ColaboradorNome { get; set; }
    public long? TipoPagamentoId { get; set; }
    public long? ConvenioId { get; set; }
    public string? NrCesta { get; set; }
    public decimal TotalBruto { get; set; }
    public decimal TotalDesconto { get; set; }
    public decimal TotalLiquido { get; set; }
    public int TotalItens { get; set; }
    public VendaStatus Status { get; set; }
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; }
    public List<VendaItemDto> Itens { get; set; } = new();
}

public class VendaFormDto
{
    public long FilialId { get; set; }
    public long? CaixaId { get; set; }
    public long? ClienteId { get; set; }
    public long? ColaboradorId { get; set; }
    public long? TipoPagamentoId { get; set; }
    public long? ConvenioId { get; set; }
    public string? NrCesta { get; set; }
    public int? Origem { get; set; }
    public string? Observacao { get; set; }
    public List<VendaItemFormDto> Itens { get; set; } = new();
}

public class VendaItemDto
{
    public long Id { get; set; }
    public long ProdutoId { get; set; }
    public string ProdutoCodigo { get; set; } = string.Empty;
    public string ProdutoNome { get; set; } = string.Empty;
    public string? Fabricante { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PercentualDesconto { get; set; }
    public decimal PercentualPromocao { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Total { get; set; }
    public decimal EstoqueAtual { get; set; }
    public string? Grupo { get; set; }
    public string? Formula { get; set; }
    public List<VendaItemDescontoDto> Descontos { get; set; } = new();
}

public class VendaItemFormDto
{
    public long ProdutoId { get; set; }
    public string ProdutoCodigo { get; set; } = string.Empty;
    public string ProdutoNome { get; set; } = string.Empty;
    public string? Fabricante { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal Quantidade { get; set; } = 1;
    public decimal PercentualDesconto { get; set; }
    public decimal PercentualPromocao { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Total { get; set; }
    public List<VendaItemDescontoFormDto> Descontos { get; set; } = new();
}

public class VendaItemDescontoDto
{
    public long Id { get; set; }
    public int Tipo { get; set; }
    public decimal Percentual { get; set; }
    public string Origem { get; set; } = string.Empty;
    public string Regra { get; set; } = string.Empty;
    public long? OrigemId { get; set; }
    public long? LiberadoPorId { get; set; }
}

public class VendaItemDescontoFormDto
{
    public int Tipo { get; set; }
    public decimal Percentual { get; set; }
    public string Origem { get; set; } = string.Empty;
    public string Regra { get; set; } = string.Empty;
    public long? OrigemId { get; set; }
    public long? LiberadoPorId { get; set; }
}
