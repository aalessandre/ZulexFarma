namespace ZulexPharma.Application.DTOs.Substancias;

public class SubstanciaListDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Dcb { get; set; } = string.Empty;
    public string Cas { get; set; } = string.Empty;
    public bool ControleEspecialSngpc { get; set; }
    public string? ClasseTerapeutica { get; set; }
    public string? ListaPortaria344 { get; set; }
    public int? TipoReceita { get; set; }
    public int? ValidadeReceitaDias { get; set; }
    public bool Adendo { get; set; }
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
    public string? ListaPortaria344 { get; set; }
    public int? TipoReceita { get; set; }
    public int? ValidadeReceitaDias { get; set; }
    public bool Adendo { get; set; } = false;
    public bool Ativo { get; set; } = true;
}

public class SubstanciaImportResultDto
{
    public int TotalLinhas { get; set; }
    public int Importadas { get; set; }
    public int PuladasSemDcbCas { get; set; }
    public int PuladasDuplicadas { get; set; }
    public int RemovidasAntes { get; set; }
    public List<string> Avisos { get; set; } = new();
}
