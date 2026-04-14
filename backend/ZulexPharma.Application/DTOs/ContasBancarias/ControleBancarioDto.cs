namespace ZulexPharma.Application.DTOs.ContasBancarias;

/// <summary>Item do extrato da conta bancária.</summary>
public class MovimentoContaBancariaListDto
{
    public long Id { get; set; }
    public DateTime DataMovimento { get; set; }
    public int Tipo { get; set; }              // 1=Entrada, 2=Saida
    public string TipoDescricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public long? CaixaId { get; set; }
    public string? CaixaCodigo { get; set; }
    public long? CaixaMovimentoId { get; set; }
    public string? CaixaMovimentoCodigo { get; set; }
    public int? CaixaMovimentoTipo { get; set; }    // TipoMovimentoCaixa (se veio do caixa)
    public string? UsuarioNome { get; set; }
    public bool Manual { get; set; }                 // true = lançamento manual
}

/// <summary>Saldo atual e totalizadores do período.</summary>
public class ContaBancariaSaldoDto
{
    public long ContaBancariaId { get; set; }
    public string ContaBancariaNome { get; set; } = string.Empty;
    public int TipoConta { get; set; }
    public bool EhCofre { get; set; }
    public decimal SaldoInicial { get; set; }
    public DateTime? DataSaldoInicial { get; set; }
    // Saldo absoluto (inicial + todas movimentações até agora)
    public decimal SaldoAtual { get; set; }
    // Totalizadores do período filtrado
    public decimal TotalEntradasPeriodo { get; set; }
    public decimal TotalSaidasPeriodo { get; set; }
    public decimal SaldoPeriodo => TotalEntradasPeriodo - TotalSaidasPeriodo;
}

public class AjusteManualFormDto
{
    public int Tipo { get; set; }                // 1=Entrada, 2=Saida
    public decimal Valor { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Observacao { get; set; }
}
