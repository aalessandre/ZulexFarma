namespace ZulexPharma.Domain.Entities;

public class LogErro : BaseEntity
{
    public DateTime OcorridoEm { get; set; } = DateTime.UtcNow;
    public string? UsuarioLogin { get; set; }
    public string? Tela { get; set; }
    public string? Funcao { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? DadosAdicionais { get; set; }
}
