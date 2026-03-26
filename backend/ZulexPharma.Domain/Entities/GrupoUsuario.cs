namespace ZulexPharma.Domain.Entities;

public class GrupoUsuario : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<GrupoPermissao> Permissoes { get; set; } = new List<GrupoPermissao>();
    public ICollection<UsuarioFilialGrupo> UsuarioFilialGrupos { get; set; } = new List<UsuarioFilialGrupo>();
}
