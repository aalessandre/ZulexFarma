using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.Interfaces;

/// <summary>
/// Numeração centralizada por (Filial, Modelo, Série) com lock transacional —
/// usada quando múltiplos emissores compartilham a mesma série e podem emitir
/// simultaneamente (caso do Self-Checkout: N terminais, mesma série).
/// </summary>
public interface ISequenciaCentralService
{
    /// <summary>
    /// Reserva atomicamente o próximo número da sequência (FilialId, Modelo, Série).
    /// Cria a sequência se ainda não existir, iniciando em <paramref name="numeroPartida"/>
    /// (ou 1 quando null). Esse parâmetro reflete o "Nº atual" que o admin
    /// configura no accordion Fiscal — só é considerado na criação da linha.
    /// Após inicializada, a sequência incrementa sozinha.
    /// Lock transacional impede colisão entre chamadas concorrentes.
    /// </summary>
    Task<long> ProximoNumeroAsync(long filialId, ModeloDocumento modelo, int serie, long? numeroPartida = null, CancellationToken ct = default);
}
