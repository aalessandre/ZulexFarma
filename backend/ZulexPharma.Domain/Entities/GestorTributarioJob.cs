using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Registro de um trabalho em lote do Gestor Tributário (revisão de base, sincronização).
/// Persistente — sobrevive a refresh de browser e permite acompanhamento de progresso.
/// </summary>
public class GestorTributarioJob : BaseEntity
{
    public TipoJobGestorTributario Tipo { get; set; }
    public StatusJobGestorTributario Status { get; set; } = StatusJobGestorTributario.Pendente;

    public string Provider { get; set; } = "avant";

    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }

    public int TotalItens { get; set; }
    public int ItensProcessados { get; set; }
    public int ItensAtualizados { get; set; }
    public int ItensNaoEncontrados { get; set; }
    public int ItensComErro { get; set; }
    public int RequisicoesUsadas { get; set; }

    /// <summary>JSON dos filtros usados para disparar o job (grupo, fabricante, etc).</summary>
    public string? FiltroJson { get; set; }

    public string? MensagemErro { get; set; }

    public long? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
