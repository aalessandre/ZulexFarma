namespace ZulexPharma.Domain.Entities;

/// <summary>Registro de uma execução de atualização de preços (para reversão).</summary>
public class AtualizacaoPreco : BaseEntity
{
    public long FilialId { get; set; }

    /// <summary>"ABCFARMA" ou "MANUAL"</summary>
    public string Tipo { get; set; } = "ABCFARMA";

    public DateTime DataExecucao { get; set; } = DateTime.UtcNow;
    public long? UsuarioId { get; set; }
    public string? NomeUsuario { get; set; }

    /// <summary>Filtros aplicados (JSON): grupos, modo, flags</summary>
    public string? FiltroJson { get; set; }

    public int TotalProdutos { get; set; }
    public int TotalAlterados { get; set; }

    /// <summary>"APLICADA" ou "REVERTIDA"</summary>
    public string Status { get; set; } = "APLICADA";

    public ICollection<AtualizacaoPrecoItem> Itens { get; set; } = new List<AtualizacaoPrecoItem>();
}
