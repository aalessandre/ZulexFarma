namespace ZulexPharma.Application.DTOs.Usuarios;

public class UsuarioListDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public bool IsAdministrador { get; set; }
    public bool Ativo { get; set; }
    public long GrupoUsuarioId { get; set; }
    public string NomeGrupo { get; set; } = string.Empty;
    public long FilialId { get; set; }
    public string NomeFilial { get; set; } = string.Empty;
    public DateTime? UltimoAcesso { get; set; }
}

public class UsuarioFormDto
{
    public string Nome { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string? Senha { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public bool IsAdministrador { get; set; }
    public bool Ativo { get; set; } = true;
    public long GrupoUsuarioId { get; set; }
    public long FilialId { get; set; }
}
