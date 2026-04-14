namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Lote de um produto em uma filial. Guarda o saldo atual e metadados do lote (validade, fabricação, etc).
/// Usado tanto para controle SNGPC (psicotrópicos, antimicrobianos) quanto para rastreio operacional
/// de grupos marcados com <c>ControlarLotesVencimento=true</c>.
///
/// Um produto pode ter múltiplos lotes por filial — cada entrada (compra) pode criar um novo lote
/// ou incrementar um existente (quando número de lote, validade e fabricação coincidem).
/// </summary>
public class ProdutoLote : BaseEntity
{
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public long FilialId { get; set; }

    /// <summary>Número do lote (nLote do XML). "S/L" para lotes fictícios gerados retroativamente.</summary>
    public string NumeroLote { get; set; } = string.Empty;

    public DateTime? DataFabricacao { get; set; }
    public DateTime? DataValidade { get; set; }

    /// <summary>Saldo atual do lote. Denormalizado para performance; sincronizado via MovimentoLote.</summary>
    public decimal SaldoAtual { get; set; }

    /// <summary>Snapshot do Registro MS no momento da entrada do lote.</summary>
    public string? RegistroMs { get; set; }

    /// <summary>Fornecedor que originou o lote (primeira entrada).</summary>
    public long? FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }

    /// <summary>Compra que originou o lote (referência da primeira entrada).</summary>
    public long? CompraId { get; set; }
    public Compra? Compra { get; set; }

    /// <summary>
    /// True quando o lote foi auto-gerado pelo sistema como ponto de partida retroativo
    /// (ex: ao ativar o controle de lotes em um grupo que já tinha estoque).
    /// Lotes fictícios devem ser destacados na UI e idealmente zerados após balanço físico.
    /// </summary>
    public bool EhLoteFicticio { get; set; }

    public string? Observacao { get; set; }

    public DateTime PrimeiraEntradaEm { get; set; }
    public DateTime? UltimaMovimentacaoEm { get; set; }

    public ICollection<MovimentoLote> Movimentos { get; set; } = new List<MovimentoLote>();
}
