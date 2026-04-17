namespace ZulexPharma.Application.DTOs.Fidelidade;

public class PremioFidelidadeListDto
{
    public long Id { get; set; }
    public string? Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Categoria { get; set; }
    public int PontosNecessarios { get; set; }
    public string? ImagemUrl { get; set; }
    public int? Estoque { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class PremioFidelidadeFormDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Categoria { get; set; }
    public int PontosNecessarios { get; set; }
    public string? ImagemUrl { get; set; }
    public int? Estoque { get; set; }
    public bool Ativo { get; set; } = true;
}
