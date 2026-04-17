using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Campanha de fidelidade — unifica Pontos e Cashback via <see cref="Tipo"/>.
/// Os pontos/cashback são creditados ao cliente na finalização da venda, conforme as regras:
/// <list type="bullet">
/// <item>Cliente precisa estar informado na venda</item>
/// <item>Filial da venda precisa estar na lista (ou lista vazia = todas)</item>
/// <item>Pagamento precisa conter ao menos uma das formas permitidas (ou lista vazia = todas)</item>
/// <item>Ao menos 1 item da venda deve pertencer aos agrupadores marcados</item>
/// <item>Campanha vigente (data/hora/dia da semana)</item>
/// <item>Quando múltiplas campanhas elegíveis, vence a mais vantajosa (maior crédito gerado)</item>
/// </list>
/// </summary>
public class CampanhaFidelidade : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    public TipoFidelidade Tipo { get; set; } = TipoFidelidade.Pontos;
    public ModoContagemFidelidade ModoContagem { get; set; } = ModoContagemFidelidade.PorVenda;

    // ── Geração de crédito ──────────────────────────────────────
    /// <summary>Valor base em R$ usado para gerar o crédito (ex: R$5 gera X pts/cashback).</summary>
    public decimal ValorBase { get; set; }

    /// <summary>Pontos ganhos a cada <see cref="ValorBase"/> (quando <see cref="Tipo"/> = Pontos).</summary>
    public int PontosGanhos { get; set; }

    /// <summary>Percentual de cashback do valor elegível (quando <see cref="Tipo"/> = Cashback). Ex: 5 = 5%.</summary>
    public decimal PercentualCashback { get; set; }

    // ── Resgate (só Pontos) ─────────────────────────────────────
    public FormaRetiradaPontos FormaRetirada { get; set; } = FormaRetiradaPontos.DescontoNaVenda;

    /// <summary>
    /// Conversão pts → R$ na hora do resgate (quando <see cref="FormaRetirada"/> = DescontoNaVenda).
    /// Independente da conversão de creditação — dá margem pro lojista.
    /// Ex: 0.05 = cada ponto vale R$0,05 no desconto.
    /// </summary>
    public decimal ValorPorPonto { get; set; }

    // ── Validade e alerta ───────────────────────────────────────
    /// <summary>Dias de validade do crédito a partir da data de emissão (0 = sem validade).</summary>
    public int DiasValidadePontos { get; set; }

    /// <summary>Saldo em pontos que dispara alerta no caixa quando o cliente atinge (0 = sem alerta).</summary>
    public int LimiarAlerta { get; set; }

    // ── Vigência ────────────────────────────────────────────────
    public DateTime DataHoraInicio { get; set; }
    public DateTime? DataHoraFim { get; set; }

    /// <summary>Bitmask dos dias da semana. Dom=1, Seg=2, Ter=4, Qua=8, Qui=16, Sex=32, Sáb=64. 127=todos.</summary>
    public int DiaSemana { get; set; } = 127;
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFim { get; set; }

    // ── Navigation ──────────────────────────────────────────────
    public ICollection<CampanhaFidelidadeFilial> Filiais { get; set; } = new List<CampanhaFidelidadeFilial>();
    public ICollection<CampanhaFidelidadePagamento> Pagamentos { get; set; } = new List<CampanhaFidelidadePagamento>();
    public ICollection<CampanhaFidelidadeItem> Itens { get; set; } = new List<CampanhaFidelidadeItem>();
}
