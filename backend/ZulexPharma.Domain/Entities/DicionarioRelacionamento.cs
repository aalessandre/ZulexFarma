namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Definições por RELACIONAMENTO (FK): comportamento de cascade no delete/update.
/// </summary>
public class DicionarioRelacionamento
{
    public long Id { get; set; }

    /// <summary>Tabela que contém a FK (ex: "Usuarios")</summary>
    public string Tabela { get; set; } = "";

    /// <summary>Coluna FK (ex: "ColaboradorId")</summary>
    public string ColunaFk { get; set; } = "";

    /// <summary>Tabela referenciada (ex: "Colaboradores")</summary>
    public string TabelaAlvo { get; set; } = "";

    /// <summary>Comportamento ao deletar: cascade, restrict, setNull, noAction</summary>
    public string OnDelete { get; set; } = "restrict";

    /// <summary>Comportamento ao atualizar: cascade, restrict, setNull, noAction</summary>
    public string OnUpdate { get; set; } = "noAction";

    public bool Revisado { get; set; }
    public DateTime? RevisadoEm { get; set; }
}
