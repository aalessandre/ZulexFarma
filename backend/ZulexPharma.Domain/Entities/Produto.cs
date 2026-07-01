namespace ZulexPharma.Domain.Entities;

public class Produto : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public int QtdeEmbalagem { get; set; } = 1;
    public decimal? PrecoFp { get; set; }
    /// <summary>Preço tabela FP reduzido pra beneficiários do Bolsa Família (total da embalagem).</summary>
    public decimal? PrecoFpBolsaFamilia { get; set; }
    /// <summary>Ativa o fluxo Farmácia Popular pra esse produto (gate do MVP).</summary>
    public bool ParticipaFarmaciaPopular { get; set; }
    public string Lista { get; set; } = "Indefinida";
    public short Fracao { get; set; } = 1;
    public bool Eliminado { get; set; }
    public bool PermitirConferenciaDigitando { get; set; }

    /// <summary>Quando true, o produto é um "modelo" e é vendido pelas variações
    /// (grade tamanho/cor/etc). Ver docs/specs/multiramo-grade.md (Passo 2).</summary>
    public bool ControlaGrade { get; set; }

    /// <summary>
    /// Classe terapêutica para fins de controle SNGPC.
    /// Valores: null (nenhum) | "Psicotrópicos" | "Antimicrobiano".
    /// Produtos com esse campo preenchido são considerados controlados e entram no relatório mensal SNGPC.
    /// </summary>
    public string? ClasseTerapeutica { get; set; }

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
    public ICollection<ProdutoFiscal> Fiscais { get; set; } = new List<ProdutoFiscal>();
    public ICollection<ProdutoDados> Dados { get; set; } = new List<ProdutoDados>();

    // ── Grade de variações (Passo 2) ──────────────────────────────
    public ICollection<ProdutoAtributo> Atributos { get; set; } = new List<ProdutoAtributo>();
    public ICollection<ProdutoVariacao> Variacoes { get; set; } = new List<ProdutoVariacao>();
}
