namespace ZulexPharma.Domain.Entities;

public class LogAcao : BaseEntity
{
    public DateTime RealizadoEm { get; set; } = DateTime.UtcNow;

    public long UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string Tela { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string? Entidade { get; set; }
    public string? RegistroId { get; set; }
    public string? ValoresAnteriores { get; set; }
    public string? ValoresNovos { get; set; }

    /// <summary>Indica se esta ação precisou de liberação por senha de supervisor.</summary>
    public bool LiberacaoPorSenha { get; set; } = false;

    public long? UsuarioLiberouId { get; set; }
    public Usuario? UsuarioLiberou { get; set; }
}
