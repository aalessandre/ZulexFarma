using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.SelfCheckout;

/// <summary>
/// Parâmetros de conexão com o ERP origem.
/// Usado no fluxo de teste de conexão (sem precisar persistir antes).
/// </summary>
public class ConfiguracaoConexaoErpDto
{
    public ErpOrigem ErpOrigem { get; set; } = ErpOrigem.Inovafarma;

    public string HostBanco { get; set; } = string.Empty;
    public string NomeBanco { get; set; } = string.Empty;
    public string UsuarioBanco { get; set; } = string.Empty;

    /// <summary>Senha em texto puro (vinda do form). NÃO persistir sem criptografar.</summary>
    public string SenhaBanco { get; set; } = string.Empty;

    /// <summary>Identificador da filial no ERP externo (CodigoEmpresa do Inovafarma).</summary>
    public string FilialErpOrigem { get; set; } = string.Empty;
}
