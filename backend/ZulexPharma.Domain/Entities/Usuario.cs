namespace ZulexPharma.Domain.Entities;

public class Usuario : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public bool IsAdministrador { get; set; } = false;
    public DateTime? UltimoAcesso { get; set; }

    public long GrupoUsuarioId { get; set; }
    public GrupoUsuario GrupoUsuario { get; set; } = null!;

    public long FilialId { get; set; }
    public Filial Filial { get; set; } = null!;

    /// <summary>Tempo máximo de sessão em minutos. 0 = herda de Configurações.</summary>
    public int SessaoMaximaMinutos { get; set; } = 0;

    /// <summary>Tempo de inatividade em minutos. 0 = herda de Configurações.</summary>
    public int InatividadeMinutos { get; set; } = 0;

    public long? ColaboradorId { get; set; }
    public Colaborador? Colaborador { get; set; }

    public ICollection<LogAcao> LogsAcoes { get; set; } = new List<LogAcao>();
    public ICollection<UsuarioFilialGrupo> FilialGrupos { get; set; } = new List<UsuarioFilialGrupo>();
}
