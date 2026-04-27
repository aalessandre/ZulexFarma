using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities.SelfCheckout;

/// <summary>
/// Configuração do módulo Self-Checkout por filial. 1:1 com Filial.
/// Reusa a entidade Filial para dados fiscais (CNPJ/IE/IBGE/UF/Certificado).
/// Apenas armazena o que é específico do self-checkout: ERP origem + credenciais
/// do banco externo + filial no ERP origem + série dedicada da NFC-e.
/// </summary>
public class SelfCheckoutConfiguracao : BaseEntity
{
    public long FilialId { get; set; }
    public Filial? Filial { get; set; }

    /// <summary>ERP terceiro de onde os produtos/preços vêm.</summary>
    public ErpOrigem ErpOrigem { get; set; } = ErpOrigem.Inovafarma;

    /// <summary>Host/IP do banco do ERP externo.</summary>
    public string HostBanco { get; set; } = string.Empty;

    /// <summary>Nome do banco do ERP externo (ex: INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3).</summary>
    public string NomeBanco { get; set; } = string.Empty;

    /// <summary>Usuário do banco externo.</summary>
    public string UsuarioBanco { get; set; } = string.Empty;

    /// <summary>Senha do banco externo, criptografada via CriptografiaHelper.</summary>
    public string SenhaBancoCriptografada { get; set; } = string.Empty;

    /// <summary>
    /// Identificador da filial no ERP externo (ex: CodigoEmpresa do Inovafarma).
    /// Mantido como string para suportar diferentes formatos por ERP.
    /// </summary>
    public string FilialErpOrigem { get; set; } = string.Empty;

    /// <summary>
    /// Código da Natureza de Operação no ERP origem (ex: Fiscal_Natureza.CodigoNatureza
    /// do Inovafarma) usada para emitir NFC-e de venda a consumidor final.
    /// Determina CST/CSOSN, CFOP e CST PIS/COFINS do snapshot fiscal.
    /// Null = ainda não configurado (connector não consegue emitir NFC-e até preencher).
    /// </summary>
    public int? CodigoNaturezaOperacaoNfce { get; set; }

    /// <summary>
    /// Usuário virtual operador do self-checkout (terminal opera sem login do cliente).
    /// Cadastrado uma vez por filial, gera token longo usado pelos terminais.
    /// </summary>
    public long? UsuarioVirtualId { get; set; }
    public Usuario? UsuarioVirtual { get; set; }
}
