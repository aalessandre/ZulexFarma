namespace ZulexPharma.Domain.Entities;

public class Produto : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public int QtdeEmbalagem { get; set; } = 1;
    public decimal? PrecoFp { get; set; }
    public string Lista { get; set; } = "Indefinida";
    public short Fracao { get; set; } = 1;
    public bool Eliminado { get; set; }

    // FKs
    public long? FabricanteId { get; set; }
    public long? GrupoPrincipalId { get; set; }
    public long? GrupoProdutoId { get; set; }
    public long? SubGrupoId { get; set; }
    public long? NcmId { get; set; }

    // Navigation
    public Fabricante? Fabricante { get; set; }
    public GrupoPrincipal? GrupoPrincipal { get; set; }
    public GrupoProduto? GrupoProduto { get; set; }
    public SubGrupo? SubGrupo { get; set; }
    public Ncm? Ncm { get; set; }

    public ICollection<ProdutoBarras> Barras { get; set; } = new List<ProdutoBarras>();
    public ICollection<ProdutoMs> RegistrosMs { get; set; } = new List<ProdutoMs>();
    public ICollection<ProdutoSubstancia> Substancias { get; set; } = new List<ProdutoSubstancia>();
    public ICollection<ProdutoFornecedor> Fornecedores { get; set; } = new List<ProdutoFornecedor>();
    public ProdutoFiscal? Fiscal { get; set; }
    public ICollection<ProdutoDados> Dados { get; set; } = new List<ProdutoDados>();
}
