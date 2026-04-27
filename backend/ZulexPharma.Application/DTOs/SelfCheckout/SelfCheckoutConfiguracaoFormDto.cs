using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.SelfCheckout;

/// <summary>Form para criar/editar a configuração da filial.</summary>
public class SelfCheckoutConfiguracaoFormDto
{
    public ErpOrigem ErpOrigem { get; set; } = ErpOrigem.Inovafarma;
    public string HostBanco { get; set; } = string.Empty;
    public string NomeBanco { get; set; } = string.Empty;
    public string UsuarioBanco { get; set; } = string.Empty;

    /// <summary>
    /// Senha em texto puro vinda do form. Em criação: obrigatório.
    /// Em edição: vazio = mantém a senha existente; preenchido = substitui (criptografada antes de gravar).
    /// </summary>
    public string? SenhaBanco { get; set; }

    public string FilialErpOrigem { get; set; } = string.Empty;
    public int? CodigoNaturezaOperacaoNfce { get; set; }
    public long? UsuarioVirtualId { get; set; }
    public bool Ativo { get; set; } = true;
}
