using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.ContasPagar;

public class ContaPagarListDto
{
    public long Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public long? PessoaId { get; set; }
    public string? PessoaNome { get; set; }
    public long? PlanoContaId { get; set; }
    public string? PlanoContaDescricao { get; set; }
    public long FilialId { get; set; }
    public string? FilialNome { get; set; }
    public long? CompraId { get; set; }
    public decimal Valor { get; set; }
    public decimal Desconto { get; set; }
    public decimal Juros { get; set; }
    public decimal Multa { get; set; }
    public decimal ValorFinal { get; set; }
    public DateTime DataEmissao { get; set; }
    public DateTime DataVencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
    public string? NrDocumento { get; set; }
    public string? NrNotaFiscal { get; set; }
    public string? Observacao { get; set; }
    public StatusConta Status { get; set; }
    public string StatusDescricao { get; set; } = string.Empty;
    public bool Vencido { get; set; }
    public Guid? RecorrenciaGrupo { get; set; }
    public string? RecorrenciaParcela { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class ContaPagarFormDto
{
    public string Descricao { get; set; } = string.Empty;
    public long? PessoaId { get; set; }
    public long? PlanoContaId { get; set; }
    public long FilialId { get; set; }
    public long? CompraId { get; set; }
    public decimal Valor { get; set; }
    public decimal Desconto { get; set; }
    public decimal Juros { get; set; }
    public decimal Multa { get; set; }
    public DateTime DataEmissao { get; set; }
    public DateTime DataVencimento { get; set; }
    public string? NrDocumento { get; set; }
    public string? NrNotaFiscal { get; set; }
    public string? Observacao { get; set; }
    public StatusConta Status { get; set; } = StatusConta.Aberto;
    public DateTime? DataPagamento { get; set; }
    public bool Ativo { get; set; } = true;
}

public class ContaPagarRecorrenteDto
{
    public ContaPagarFormDto Modelo { get; set; } = new();
    public int QuantidadeMeses { get; set; }
    public int DiaVencimento { get; set; }
}
