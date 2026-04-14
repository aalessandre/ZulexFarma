namespace ZulexPharma.Application.DTOs.Caixa;

/// <summary>Totalizador por forma de pagamento para a tela de Conferência de Caixa.</summary>
public class ConferenciaFormaPagamentoDto
{
    public long? TipoPagamentoId { get; set; }
    public string TipoPagamentoNome { get; set; } = string.Empty;
    public int? Modalidade { get; set; }
    public decimal ValorDeclarado { get; set; }
    public decimal ValorSistema { get; set; }
    public decimal Diferenca => ValorSistema - ValorDeclarado;
    public int QtdeMovimentos { get; set; }
    public int QtdeConferidos { get; set; }
    public List<CaixaMovimentoListDto> Movimentos { get; set; } = new();
}

/// <summary>Resposta do endpoint GET /api/caixas/{id}/conferencia.</summary>
public class ConferenciaCaixaDto
{
    public long CaixaId { get; set; }
    public string? Codigo { get; set; }
    public string? ModeloFechamento { get; set; }
    public DateTime DataAbertura { get; set; }
    public DateTime? DataFechamento { get; set; }
    public DateTime? DataConferencia { get; set; }
    public string ColaboradorNome { get; set; } = string.Empty;
    public decimal ValorAbertura { get; set; }
    public int Status { get; set; }
    public string StatusDescricao { get; set; } = string.Empty;
    public List<ConferenciaFormaPagamentoDto> FormasPagamento { get; set; } = new();
}

public class ConferirCaixaFormDto
{
    public List<long> MovimentoIdsConferidos { get; set; } = new();
    public decimal? ValorDinheiroContado { get; set; }
    public string? Observacao { get; set; }
}
