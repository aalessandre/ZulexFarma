using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class GrupoPermissao : BaseEntity
{
    public long GrupoUsuarioId { get; set; }
    public GrupoUsuario GrupoUsuario { get; set; } = null!;

    public BlocoMenu Bloco { get; set; }
    public string CodigoTela { get; set; } = string.Empty;
    public string NomeTela { get; set; } = string.Empty;

    public bool PodeIncluir { get; set; } = false;
    public bool PodeAlterar { get; set; } = false;
    public bool PodeExcluir { get; set; } = false;
    public bool PodeConsultar { get; set; } = true;
    public string? PermissoesAdicionais { get; set; }
}
