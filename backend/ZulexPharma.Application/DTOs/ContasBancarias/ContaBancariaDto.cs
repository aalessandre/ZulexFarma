using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.ContasBancarias;

public class ContaBancariaListDto
{
    public long Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public TipoConta TipoConta { get; set; }
    public string TipoContaDescricao { get; set; } = string.Empty;
    public string? Banco { get; set; }
    public string? Agencia { get; set; }
    public string? AgenciaDigito { get; set; }
    public string? NumeroConta { get; set; }
    public string? ContaDigito { get; set; }
    public string? ChavePix { get; set; }
    public decimal SaldoInicial { get; set; }
    public DateTime? DataSaldoInicial { get; set; }
    public long? PlanoContaId { get; set; }
    public string? PlanoContaDescricao { get; set; }
    public long? FilialId { get; set; }
    public string? FilialNome { get; set; }
    public string? Observacao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class ContaBancariaFormDto
{
    public string Descricao { get; set; } = string.Empty;
    public TipoConta TipoConta { get; set; }
    public string? Banco { get; set; }
    public string? Agencia { get; set; }
    public string? AgenciaDigito { get; set; }
    public string? NumeroConta { get; set; }
    public string? ContaDigito { get; set; }
    public string? ChavePix { get; set; }
    public decimal SaldoInicial { get; set; }
    public DateTime? DataSaldoInicial { get; set; }
    public long? PlanoContaId { get; set; }
    public long? FilialId { get; set; }
    public string? Observacao { get; set; }
    public bool Ativo { get; set; } = true;
}
