namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Configurações gerais do sistema em formato chave/valor.
/// Herda de BaseEntity para participar do sync entre filiais.
/// </summary>
public class Configuracao : BaseEntity
{
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string? Descricao { get; set; }
}
