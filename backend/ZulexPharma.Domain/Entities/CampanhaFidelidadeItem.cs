namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Agrupador vinculado a uma campanha de fidelidade.
/// Exatamente um dos FKs deve estar preenchido — indica o nível ao qual a campanha se aplica.
/// Se <see cref="Incluir"/> = true, os produtos que se encaixam entram na campanha;
/// se false, são explicitamente excluídos (mesmo que outro nível os incluísse).
/// </summary>
public class CampanhaFidelidadeItem : BaseEntity
{
    public long CampanhaFidelidadeId { get; set; }
    public CampanhaFidelidade CampanhaFidelidade { get; set; } = null!;

    public long? GrupoPrincipalId { get; set; }
    public GrupoPrincipal? GrupoPrincipal { get; set; }

    public long? GrupoProdutoId { get; set; }
    public GrupoProduto? GrupoProduto { get; set; }

    public long? SubGrupoId { get; set; }
    public SubGrupo? SubGrupo { get; set; }

    public long? SecaoId { get; set; }
    public Secao? Secao { get; set; }

    public long? ProdutoFamiliaId { get; set; }
    public ProdutoFamilia? ProdutoFamilia { get; set; }

    public long? FabricanteId { get; set; }
    public Fabricante? Fabricante { get; set; }

    public long? ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    /// <summary>True = produtos do agrupador entram na campanha. False = excluem.</summary>
    public bool Incluir { get; set; } = true;

    // ── Campos específicos de Cashback (por item) ────────────────
    /// <summary>Preço de venda de referência do produto/agrupador (preenchido do cadastro, apenas visual).</summary>
    public decimal? ValorVendaReferencia { get; set; }

    /// <summary>Percentual de cashback para este item.</summary>
    public decimal? PercentualCashbackItem { get; set; }

    /// <summary>Valor fixo de cashback para este item (usado na venda).</summary>
    public decimal? ValorCashbackItem { get; set; }
}
