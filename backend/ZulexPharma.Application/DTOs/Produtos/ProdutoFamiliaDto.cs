namespace ZulexPharma.Application.DTOs.Produtos;

public class ProdutoFamiliaListDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public bool Ativo { get; set; }
}

public class ProdutoFamiliaFormDto
{
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}
