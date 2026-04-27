using ZulexPharma.Application.DTOs.SelfCheckout;

namespace ZulexPharma.Application.Interfaces;

/// <summary>
/// Cria instâncias de <see cref="IErpConnector"/> conforme a configuração persistida
/// da filial (descriptografa senha e seleciona o conector certo conforme ErpOrigem).
/// </summary>
public interface IErpConnectorFactory
{
    /// <summary>Instancia o conector usando a configuração já persistida da filial.
    /// Retorna null se não houver configuração ativa.</summary>
    Task<IErpConnector?> CriarParaFilialAsync(long filialId, CancellationToken ct = default);

    /// <summary>Instancia o conector com parâmetros ad-hoc (sem persistir).
    /// Usado pelo fluxo de teste de conexão antes de salvar a configuração.</summary>
    IErpConnector CriarTransiente(ConfiguracaoConexaoErpDto config);
}
