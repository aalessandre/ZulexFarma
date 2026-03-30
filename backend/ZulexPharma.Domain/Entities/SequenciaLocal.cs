namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Controla o próximo sequencial do Codigo visível por tabela.
/// Cada PC tem o seu. Não replica.
/// </summary>
public class SequenciaLocal
{
    public long Id { get; set; }
    public string Tabela { get; set; } = "";
    public long Ultimo { get; set; }
}
