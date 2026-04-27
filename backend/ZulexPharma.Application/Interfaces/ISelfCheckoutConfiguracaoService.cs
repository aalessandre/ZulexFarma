using ZulexPharma.Application.DTOs.SelfCheckout;

namespace ZulexPharma.Application.Interfaces;

public interface ISelfCheckoutConfiguracaoService
{
    Task<SelfCheckoutConfiguracaoDto?> ObterPorFilialAsync(long filialId, CancellationToken ct = default);

    /// <summary>Cria ou atualiza a configuração da filial. Senha vazia em edição mantém a anterior.</summary>
    Task<SelfCheckoutConfiguracaoDto> SalvarAsync(long filialId, SelfCheckoutConfiguracaoFormDto form, CancellationToken ct = default);

    Task<List<SelfCheckoutTerminalDto>> ListarTerminaisAsync(long filialId, CancellationToken ct = default);

    Task<SelfCheckoutTerminalDto> CriarTerminalAsync(long filialId, SelfCheckoutTerminalFormDto form, CancellationToken ct = default);

    Task<SelfCheckoutTerminalDto> AtualizarTerminalAsync(long terminalId, SelfCheckoutTerminalFormDto form, CancellationToken ct = default);

    Task RemoverTerminalAsync(long terminalId, CancellationToken ct = default);
}
