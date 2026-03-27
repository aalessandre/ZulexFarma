namespace ZulexPharma.Application.DTOs.Substancias;

public class SubstanciaListDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Dcb { get; set; } = string.Empty;
    public string Cas { get; set; } = string.Empty;
    public bool ControleEspecialSngpc { get; set; }
    public string? ClasseTerapeutica { get; set; }
    public DateTime CriadoEm { get; set; }
    public bool Ativo { get; set; }
}

public class SubstanciaFormDto
{
    public string Nome { get; set; } = string.Empty;
    public string Dcb { get; set; } = string.Empty;
    public string Cas { get; set; } = string.Empty;
    public bool ControleEspecialSngpc { get; set; } = false;
    public string? ClasseTerapeutica { get; set; }
    public bool Ativo { get; set; } = true;
}
