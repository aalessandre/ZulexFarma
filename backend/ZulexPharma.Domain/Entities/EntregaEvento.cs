using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Histórico de uma Entrega. Cada transição de status vira uma linha aqui.
/// Pings de GPS (Fase 2) também são persistidos como Tipo=Localizacao.
/// </summary>
public class EntregaEvento : BaseEntity
{
    public long EntregaId { get; set; }
    public Entrega Entrega { get; set; } = null!;

    public TipoEntregaEvento Tipo { get; set; }

    /// <summary>Status preenchido quando Tipo=StatusChange.</summary>
    public StatusEntrega? Status { get; set; }

    /// <summary>Coordenadas preenchidas quando Tipo=Localizacao.</summary>
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    /// <summary>Texto livre (observação ou detalhe do evento).</summary>
    public string? Texto { get; set; }

    public long? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
