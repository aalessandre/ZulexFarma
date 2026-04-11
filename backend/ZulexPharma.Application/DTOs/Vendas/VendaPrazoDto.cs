namespace ZulexPharma.Application.DTOs.Vendas;

/// <summary>Resultado da validação de venda a prazo.</summary>
public class VendaPrazoValidacaoDto
{
    public bool ClienteBloqueado { get; set; }
    public bool ConvenioBloqueado { get; set; }
    public string? MensagemBloqueio { get; set; }

    public decimal LimiteCredito { get; set; }
    public decimal SaldoUtilizado { get; set; }
    public decimal SaldoDisponivel { get; set; }
    public bool ExcedeLimite { get; set; }

    public bool PermiteParcelada { get; set; }
    public int MaxParcelas { get; set; }
    public bool BloquearDescontoParcelada { get; set; }

    public bool ExigeSenha { get; set; }

    public bool TipoPagamentoBloqueado { get; set; }
    public string? MensagemTipoBloqueado { get; set; }
}

/// <summary>Body para POST api/vendas/{id}/finalizar.</summary>
public class FinalizarVendaDto
{
    public string? SenhaCliente { get; set; }
    public string? TokenLiberacaoCredito { get; set; }
    public int NumeroParcelas { get; set; } = 1;
}

/// <summary>Request para POST api/vendas/validar-prazo.</summary>
public class ValidarPrazoRequestDto
{
    public long ClienteId { get; set; }
    public long? ConvenioId { get; set; }
    public long TipoPagamentoId { get; set; }
    public decimal ValorVenda { get; set; }
}
