namespace ZulexPharma.Application.DTOs.Grade;

// ── Resposta ──────────────────────────────────────────────────────
public class VariacaoValorDto
{
    public long AtributoVariacaoId { get; set; }
    public long ValorAtributoId { get; set; }
    public string? AtributoNome { get; set; }
    public string? ValorTexto { get; set; }
}

public class VariacaoDto
{
    public long? Id { get; set; }
    public string? CodigoBarras { get; set; }
    public decimal? PrecoProprio { get; set; }
    /// <summary>Estoque/preço da variação na filial atual (de ProdutoDados).</summary>
    public decimal Estoque { get; set; }
    public decimal Preco { get; set; }
    public List<VariacaoValorDto> Valores { get; set; } = new();
}

public class ProdutoGradeDto
{
    public long ProdutoId { get; set; }
    public bool ControlaGrade { get; set; }
    /// <summary>Eixos que o produto usa (ordem = ordem de exibição na matriz).</summary>
    public List<long> AtributoIds { get; set; } = new();
    public List<VariacaoDto> Variacoes { get; set; } = new();
}

// ── Requisição (salvar) ───────────────────────────────────────────
public class VariacaoValorRefDto
{
    public long AtributoVariacaoId { get; set; }
    public long ValorAtributoId { get; set; }
}

public class SalvarVariacaoDto
{
    public long? Id { get; set; }
    public string? CodigoBarras { get; set; }
    public decimal? PrecoProprio { get; set; }
    public decimal Estoque { get; set; }
    public decimal Preco { get; set; }
    public List<VariacaoValorRefDto> Valores { get; set; } = new();
}

public class SalvarGradeDto
{
    public bool ControlaGrade { get; set; }
    public List<long> AtributoIds { get; set; } = new();
    public List<SalvarVariacaoDto> Variacoes { get; set; } = new();
}
