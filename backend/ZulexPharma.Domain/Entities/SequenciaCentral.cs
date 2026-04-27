using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Numeração centralizada por (Filial, ModeloDocumento, Série).
/// Diferente de <see cref="SequenciaLocal"/> (por PC) — usada quando múltiplos
/// emissores compartilham a mesma série e podem emitir simultaneamente.
///
/// Atual uso: NFC-e do Self-Checkout (vários terminais, mesma série).
/// Desenhada como genérica — pode ser adotada futuramente pelo caixa atendido
/// para resolver colisão entre múltiplos caixas na mesma filial.
///
/// Atomicidade: incrementar usa lock transacional (SELECT ... FOR UPDATE no PostgreSQL).
/// </summary>
public class SequenciaCentral : BaseEntity
{
    public long FilialId { get; set; }
    public Filial? Filial { get; set; }

    public ModeloDocumento ModeloDocumento { get; set; }

    public int Serie { get; set; }

    public long ProximoNumero { get; set; } = 1;
}
