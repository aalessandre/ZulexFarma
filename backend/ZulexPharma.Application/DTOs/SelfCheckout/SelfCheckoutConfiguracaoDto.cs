using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.SelfCheckout;

/// <summary>Leitura da configuração de Self-Checkout por filial. Senha NUNCA retorna em texto puro.</summary>
public class SelfCheckoutConfiguracaoDto
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public ErpOrigem ErpOrigem { get; set; }
    public string HostBanco { get; set; } = string.Empty;
    public string NomeBanco { get; set; } = string.Empty;
    public string UsuarioBanco { get; set; } = string.Empty;
    public string FilialErpOrigem { get; set; } = string.Empty;
    public int? CodigoNaturezaOperacaoNfce { get; set; }
    public long? UsuarioVirtualId { get; set; }
    public bool Ativo { get; set; }
    public bool TemSenhaCadastrada { get; set; }
}
