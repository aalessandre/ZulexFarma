using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.ContasReceber;

public class ContaReceberListDto
{
    public long Id { get; set; }
    public string? Codigo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? ClienteNome { get; set; }
    public string? TipoPagamentoNome { get; set; }
    public string? Modalidade { get; set; }
    public decimal Valor { get; set; }
    public decimal ValorLiquido { get; set; }
    public decimal Tarifa { get; set; }
    public decimal ValorTarifa { get; set; }
    public int NumParcela { get; set; }
    public int TotalParcelas { get; set; }
    public DateTime DataEmissao { get; set; }
    public DateTime DataVencimento { get; set; }
    public DateTime? DataRecebimento { get; set; }
    public decimal ValorRecebido { get; set; }
    public StatusContaReceber Status { get; set; }
    public string StatusDescricao { get; set; } = string.Empty;
    public string? NSU { get; set; }
    public string? TxId { get; set; }
    public string? BandeiraNome { get; set; }
    public string? AdquirenteNome { get; set; }
    public bool Vencido { get; set; }
}
