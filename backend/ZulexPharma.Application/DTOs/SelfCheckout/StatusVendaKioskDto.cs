namespace ZulexPharma.Application.DTOs.SelfCheckout;

/// <summary>Estado consumível da venda kiosk para polling do terminal.</summary>
public enum StatusVendaKiosk
{
    AguardandoFormaPagamento = 1,
    AguardandoAtendente = 2,
    NfceAutorizada = 3,
    Cancelada = 4,
    Erro = 5
}

public class StatusVendaKioskDto
{
    public long VendaId { get; set; }
    public StatusVendaKiosk Status { get; set; }
    public string? ChaveAcesso { get; set; }
    public int? NumeroNfce { get; set; }
    public int? SerieNfce { get; set; }
    public string? Mensagem { get; set; }
}

/// <summary>Item da lista de pagamentos pendentes na tela admin.</summary>
public class PagamentoPendenteDto
{
    public long VendaId { get; set; }
    public long TerminalId { get; set; }
    public int TerminalNumero { get; set; }
    public string? TerminalApelido { get; set; }
    public decimal TotalLiquido { get; set; }
    public int TotalItens { get; set; }
    public string FormaPagamento { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}
