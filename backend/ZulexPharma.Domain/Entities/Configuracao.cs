namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Configurações gerais do sistema em formato chave/valor.
/// </summary>
public class Configuracao
{
    public long Id { get; set; }
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string? Descricao { get; set; }
}
