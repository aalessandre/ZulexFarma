namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Lotes de um item da nota fiscal de compra. Um item (<c>CompraProduto</c>) pode ter
/// múltiplos rastros (<c>&lt;rastro&gt;</c>) no XML da NFe — cada um vira uma linha aqui.
///
/// Mantém também snapshot dos valores originais do XML para auditoria, permitindo ao usuário
/// editar (lote, validade, fabricação, registro MS) na tela "Conferir Lotes" sem perder o original.
/// </summary>
public class CompraProdutoLote : BaseEntity
{
    public long CompraProdutoId { get; set; }
    public CompraProduto CompraProduto { get; set; } = null!;

    // ── Valores atuais (podem ter sido editados pelo usuário na conferência) ──
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime? DataFabricacao { get; set; }
    public DateTime? DataValidade { get; set; }
    public decimal Quantidade { get; set; }

    /// <summary>Snapshot do Registro MS no momento da compra (pode ser editado na conferência).</summary>
    public string? RegistroMs { get; set; }

    // ── Snapshot dos valores ORIGINAIS do XML (auditoria) ──
    public string? NumeroLoteOriginal { get; set; }
    public DateTime? DataFabricacaoOriginal { get; set; }
    public DateTime? DataValidadeOriginal { get; set; }
    public string? RegistroMsOriginal { get; set; }

    /// <summary>True se o usuário editou qualquer valor na tela "Conferir Lotes".</summary>
    public bool EditadoPeloUsuario { get; set; }
    public DateTime? EditadoEm { get; set; }
    public long? EditadoPorUsuarioId { get; set; }
    public Usuario? EditadoPorUsuario { get; set; }
}
