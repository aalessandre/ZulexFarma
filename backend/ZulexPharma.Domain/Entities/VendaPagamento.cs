namespace ZulexPharma.Domain.Entities;

public class VendaPagamento
{
    public long Id { get; set; }
    public long VendaId { get; set; }
    public Venda Venda { get; set; } = null!;
    public long TipoPagamentoId { get; set; }
    public TipoPagamento TipoPagamento { get; set; } = null!;
    public decimal Valor { get; set; }
    public decimal Troco { get; set; }

    /// <summary>Para onde foi o troco: "Dinheiro", "PIX", etc.</summary>
    public string? TrocoPara { get; set; }

    // ── Dados do cartão (para NFC-e) ──────────────────────────
    /// <summary>Bandeira do cartão: 01=Visa, 02=Mastercard, 03=Amex, 04=Elo, 99=Outros</summary>
    public string? CartaoBandeira { get; set; }
    /// <summary>Código de autorização (NSU ou cAut)</summary>
    public string? CartaoAutorizacao { get; set; }
    /// <summary>CNPJ da credenciadora/adquirente</summary>
    public string? CartaoCnpjCredenciadora { get; set; }
    /// <summary>Tipo: 1=Débito, 2=Crédito</summary>
    public int? CartaoTipo { get; set; }
}
