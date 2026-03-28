namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Armazena revisões e definições customizadas dos campos do banco.
/// Usado pela tela Dicionário de Dados para marcar campos como revisados.
/// </summary>
public class DicionarioRevisao
{
    public long Id { get; set; }
    public string Tabela { get; set; } = "";
    public string Coluna { get; set; } = "";
    public bool Revisado { get; set; }
    public bool? Unico { get; set; }
    public bool? Obrigatorio { get; set; }
    public bool? Replica { get; set; }
    public string? Observacao { get; set; }
    public DateTime? RevisadoEm { get; set; }
}
