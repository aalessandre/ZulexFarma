namespace ZulexPharma.Domain.Entities;

public abstract class BaseEntity
{
    public long Id { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
    public bool Ativo { get; set; } = true;

    /// <summary>Versão incrementada a cada alteração. Usado para sync.</summary>
    public long VersaoSync { get; set; } = 0;

    /// <summary>Id da filial onde o registro foi criado/alterado. Null = global.</summary>
    public long? FilialOrigemId { get; set; }
}
