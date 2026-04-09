namespace ZulexPharma.Domain.Entities;

public abstract class BaseEntity
{
    public long Id { get; set; }
    public string? Codigo { get; set; }
    public DateTime CriadoEm { get; set; } = Helpers.DataHoraHelper.Agora();
    public DateTime? AtualizadoEm { get; set; }
    public bool Ativo { get; set; } = true;

    /// <summary>Id da filial onde o registro foi criado.</summary>
    public long? FilialOrigemId { get; set; }

    /// <summary>GUID auxiliar para reconciliação no sync. Não é PK, sem FK.</summary>
    public Guid SyncGuid { get; set; } = Guid.NewGuid();
}
