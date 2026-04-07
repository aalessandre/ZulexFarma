using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.PreVendas;

public class PreVendaListDto
{
    public long Id { get; set; }
    public string? Codigo { get; set; }
    public string? ClienteNome { get; set; }
    public string? ColaboradorNome { get; set; }
    public string? TipoPagamentoNome { get; set; }
    public decimal TotalLiquido { get; set; }
    public int TotalItens { get; set; }
    public PreVendaStatus Status { get; set; }
    public string StatusDescricao { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}

public class PreVendaDetalheDto
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public long? ClienteId { get; set; }
    public string? ClienteNome { get; set; }
    public long? ColaboradorId { get; set; }
    public string? ColaboradorNome { get; set; }
    public long? TipoPagamentoId { get; set; }
    public long? ConvenioId { get; set; }
    public decimal TotalBruto { get; set; }
    public decimal TotalDesconto { get; set; }
    public decimal TotalLiquido { get; set; }
    public int TotalItens { get; set; }
    public PreVendaStatus Status { get; set; }
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; }
    public List<PreVendaItemDto> Itens { get; set; } = new();
}

public class PreVendaFormDto
{
    public long FilialId { get; set; }
    public long? ClienteId { get; set; }
    public long? ColaboradorId { get; set; }
    public long? TipoPagamentoId { get; set; }
    public long? ConvenioId { get; set; }
    public string? Observacao { get; set; }
    public List<PreVendaItemFormDto> Itens { get; set; } = new();
}

public class PreVendaItemDto
{
    public long Id { get; set; }
    public long ProdutoId { get; set; }
    public string ProdutoCodigo { get; set; } = string.Empty;
    public string ProdutoNome { get; set; } = string.Empty;
    public string? Fabricante { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PercentualDesconto { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Total { get; set; }
    public decimal EstoqueAtual { get; set; }
    public string? Grupo { get; set; }
    public string? Formula { get; set; }
}

public class PreVendaItemFormDto
{
    public long ProdutoId { get; set; }
    public string ProdutoCodigo { get; set; } = string.Empty;
    public string ProdutoNome { get; set; } = string.Empty;
    public string? Fabricante { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal Quantidade { get; set; } = 1;
    public decimal PercentualDesconto { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Total { get; set; }
}
