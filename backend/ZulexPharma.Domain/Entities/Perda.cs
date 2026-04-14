using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Registro de perda de estoque. Alimenta o relatório SNGPC quando o produto é controlado.
/// Ao salvar, abate o saldo do <see cref="ProdutoLote"/> e do <see cref="ProdutoDados.EstoqueAtual"/>.
/// </summary>
public class Perda : BaseEntity
{
    public long FilialId { get; set; }

    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public long ProdutoLoteId { get; set; }
    public ProdutoLote ProdutoLote { get; set; } = null!;

    public decimal Quantidade { get; set; }
    public DateTime DataPerda { get; set; }
    public MotivoPerda Motivo { get; set; }

    /// <summary>Número do Boletim de Ocorrência (obrigatório para Furto/Roubo).</summary>
    public string? NumeroBoletim { get; set; }

    public string? Observacao { get; set; }

    public long? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
