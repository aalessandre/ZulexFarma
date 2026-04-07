using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class ContaPagar : BaseEntity
{
    public string Descricao { get; set; } = string.Empty;
    public long? PessoaId { get; set; }
    public Pessoa? Pessoa { get; set; }
    public long? PlanoContaId { get; set; }
    public PlanoConta? PlanoConta { get; set; }
    public long FilialId { get; set; }
    public Filial? Filial { get; set; }
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
    public StatusConta Status { get; set; } = StatusConta.Aberto;
    public Guid? RecorrenciaGrupo { get; set; }
    public string? RecorrenciaParcela { get; set; }
}
