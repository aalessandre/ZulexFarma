namespace ZulexPharma.Domain.Enums;

public enum TipoEntregaEvento
{
    /// <summary>Mudança de status da entrega.</summary>
    StatusChange = 1,
    /// <summary>Ping de localização GPS (Fase 2 — rastreio em tempo real).</summary>
    Localizacao = 2,
    /// <summary>Observação manual registrada pelo operador.</summary>
    Observacao = 3
}
