namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Define qual perfil (grupo) um usuário tem em cada filial.
/// Permite múltiplos grupos por filial.
/// Ausência de registro para uma filial = SEM ACESSO.
/// </summary>
public class UsuarioFilialGrupo : BaseEntity
{
    public long UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public long FilialId { get; set; }
    public Filial Filial { get; set; } = null!;

    public long GrupoUsuarioId { get; set; }
    public GrupoUsuario GrupoUsuario { get; set; } = null!;
}
