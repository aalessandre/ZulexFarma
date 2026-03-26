namespace ZulexPharma.Application.DTOs.Grupos;

public class GrupoListDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int TotalUsuarios { get; set; }
    public DateTime CriadoEm { get; set; }
    public bool Ativo { get; set; }
}

public class GrupoFormDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;
}

public class PermissaoDto
{
    public long? Id { get; set; }
    public int Bloco { get; set; }
    public string CodigoTela { get; set; } = string.Empty;
    public string NomeTela { get; set; } = string.Empty;
    public bool PodeConsultar { get; set; }
    public bool PodeIncluir { get; set; }
    public bool PodeAlterar { get; set; }
    public bool PodeExcluir { get; set; }
}

public class SalvarPermissoesDto
{
    public List<PermissaoDto> Permissoes { get; set; } = new();
}
