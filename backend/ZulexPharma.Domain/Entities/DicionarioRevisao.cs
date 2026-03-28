namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Definições por TABELA: escopo (global/filial), replica, instrução IA.
/// </summary>
public class DicionarioTabela
{
    public long Id { get; set; }
    public string Tabela { get; set; } = "";
    public string Escopo { get; set; } = "global"; // "global" ou "filial"
    public bool Replica { get; set; } = true;
    public string? InstrucaoIA { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

/// <summary>
/// Definições por CAMPO: revisado, obrigatório, único, instrução IA.
/// </summary>
public class DicionarioRevisao
{
    public long Id { get; set; }
    public string Tabela { get; set; } = "";
    public string Coluna { get; set; } = "";
    public bool Revisado { get; set; }
    public bool? Unico { get; set; }
    public bool? Obrigatorio { get; set; }
    public string? Observacao { get; set; }
    public string? InstrucaoIA { get; set; }
    public DateTime? RevisadoEm { get; set; }
}
