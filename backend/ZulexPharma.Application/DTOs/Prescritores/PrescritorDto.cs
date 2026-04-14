namespace ZulexPharma.Application.DTOs.Prescritores;

public class PrescritorListDto
{
    public long Id { get; set; }
    public string? Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string TipoConselho { get; set; } = "CRM";
    public string NumeroConselho { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Especialidade { get; set; }
    public string? Telefone { get; set; }
    public DateTime CriadoEm { get; set; }
    public bool Ativo { get; set; }
}

public class PrescritorFormDto
{
    public string Nome { get; set; } = string.Empty;
    public string TipoConselho { get; set; } = "CRM";
    public string NumeroConselho { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Especialidade { get; set; }
    public string? Telefone { get; set; }
    public bool Ativo { get; set; } = true;
}
