namespace ZulexPharma.Application.DTOs.SelfCheckout;

/// <summary>Forma de pagamento aceita pelo Self-Checkout (apenas PIX e cartão no MVP).</summary>
public enum FormaPagamentoKiosk
{
    Pix = 1,
    Cartao = 2
}

public class IniciarVendaKioskDto
{
    public long TerminalId { get; set; }
    public List<VendaKioskItemDto> Itens { get; set; } = new();
}

public class VendaKioskItemDto
{
    /// <summary>Código do produto no ERP origem (ex: CodigoProduto do Inovafarma como string).</summary>
    public string CodigoExterno { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}

public class IniciarVendaKioskResultDto
{
    public long VendaId { get; set; }
    public decimal TotalLiquido { get; set; }
    public int TotalItens { get; set; }
    public List<VendaKioskItemResultDto> Itens { get; set; } = new();
}

public class VendaKioskItemResultDto
{
    public long VendaItemId { get; set; }
    public string CodigoExterno { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public int Quantidade { get; set; }
    public decimal Total { get; set; }
    public bool EmPromocao { get; set; }
}

public class RegistrarPagamentoKioskDto
{
    public FormaPagamentoKiosk FormaPagamento { get; set; }
}

public class ConfirmarVendaKioskResultDto
{
    public long VendaId { get; set; }
    public bool NfceAutorizada { get; set; }
    public string? ChaveAcesso { get; set; }
    public int? NumeroNfce { get; set; }
    public int? SerieNfce { get; set; }
    public string? Mensagem { get; set; }
}
