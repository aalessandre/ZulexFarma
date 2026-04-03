namespace ZulexPharma.Domain.Entities;

/// <summary>Dados do produto por filial: estoque, preços, promoção, descontos, geral.</summary>
public class ProdutoDados : BaseEntity
{
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public long FilialId { get; set; }

    // ── Estoque ─────────────────────────────────────────────────
    public decimal EstoqueAtual { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public decimal EstoqueMaximo { get; set; }
    public decimal Demanda { get; set; }
    public string? CurvaAbc { get; set; }
    public decimal EstoqueDeposito { get; set; }

    // ── Preços — Última Compra ──────────────────────────────────
    public decimal UltimaCompraUnitario { get; set; }
    public decimal UltimaCompraSt { get; set; }
    public decimal UltimaCompraOutros { get; set; }
    public decimal UltimaCompraIpi { get; set; }
    public decimal UltimaCompraFpc { get; set; }
    public decimal UltimaCompraBoleto { get; set; }
    public decimal UltimaCompraDifal { get; set; }
    public decimal UltimaCompraFrete { get; set; }

    // ── Preços — Valores ────────────────────────────────────────
    public decimal CustoMedio { get; set; }
    public decimal ProjecaoLucro { get; set; }
    public decimal Markup { get; set; }
    public decimal ValorVenda { get; set; }
    public decimal Pmc { get; set; }

    // ── Promoção ────────────────────────────────────────────────
    public decimal ValorPromocao { get; set; }
    public decimal ValorPromocaoPrazo { get; set; }
    public DateTime? PromocaoInicio { get; set; }
    public DateTime? PromocaoFim { get; set; }

    // ── Descontos ───────────────────────────────────────────────
    public decimal DescontoMinimo { get; set; }
    public decimal DescontoMaxSemSenha { get; set; }
    public decimal DescontoMaxComSenha { get; set; }

    // ── Geral ───────────────────────────────────────────────────
    public decimal Comissao { get; set; }
    public decimal ValorIncentivo { get; set; }

    public long? ProdutoLocalId { get; set; }
    public ProdutoLocal? ProdutoLocal { get; set; }
    public long? SecaoId { get; set; }
    public Secao? Secao { get; set; }
    public long? ProdutoFamiliaId { get; set; }
    public ProdutoFamilia? ProdutoFamilia { get; set; }

    public string? NomeEtiqueta { get; set; }
    public string? Mensagem { get; set; }

    // Flags
    public bool BloquearDesconto { get; set; }
    public bool BloquearPromocao { get; set; }
    public bool NaoAtualizarAbcfarma { get; set; }
    public bool NaoAtualizarGestorTributario { get; set; }
    public bool BloquearCompras { get; set; }
    public bool ProdutoFormula { get; set; }
    public bool BloquearComissao { get; set; }
    public bool BloquearCoberturaOferta { get; set; }
    public bool UsoContinuo { get; set; }
    public bool AvisoFracao { get; set; }

    // ── Formação de preço ─────────────────────────────────────────
    /// <summary>Formação de preço: "MARKUP" ou "PROJECAO". Herda do GrupoPrincipal se vazio.</summary>
    public string? FormacaoPreco { get; set; }

    /// <summary>Base de cálculo: "CUSTO_COMPRA" ou "CUSTO_MEDIO". Herda do GrupoPrincipal se vazio.</summary>
    public string? BaseCalculo { get; set; }

    // ── Lote / Validade ───────────────────────────────────────────
    public string? Lote { get; set; }
    public DateTime? DataValidade { get; set; }

    // ── Estatísticas ────────────────────────────────────────────
    public DateTime? UltimaCompraEm { get; set; }
    public DateTime? UltimaVendaEm { get; set; }
}
