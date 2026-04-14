namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Dados fiscais/tributários do produto por filial.
/// Os campos extras vêm do retorno do Gestor Tributário (Avant/figurafiscal/Imendes) e ficam
/// disponíveis para uso em NFC-e, cálculo de ST e demais cenários fiscais.
/// </summary>
public class ProdutoFiscal : BaseEntity
{
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public long FilialId { get; set; }

    public long? NcmId { get; set; }
    public Ncm? Ncm { get; set; }

    public string? Cfop { get; set; }
    public string? Cest { get; set; }
    public string? OrigemMercadoria { get; set; }

    // ── ICMS (saída) ────────────────────────────────────────────
    public string? CstIcms { get; set; }
    public string? Csosn { get; set; }
    public decimal AliquotaIcms { get; set; }
    /// <summary>Percentual do Fundo de Combate à Pobreza (FCP).</summary>
    public decimal AliquotaFcp { get; set; }
    /// <summary>Modalidade da base de cálculo do ICMS (0-3).</summary>
    public string? ModBc { get; set; }
    /// <summary>Percentual de redução da base de cálculo ICMS.</summary>
    public decimal PercentualReducaoBc { get; set; }
    /// <summary>Código de benefício fiscal (cBenef).</summary>
    public string? CodigoBeneficio { get; set; }
    /// <summary>Dispositivo legal que embasa a tributação ICMS (informativo).</summary>
    public string? DispositivoLegalIcms { get; set; }

    // ── Substituição Tributária (ICMS-ST entrada) ───────────────
    /// <summary>Indica se o produto está sujeito a substituição tributária (derivado do grupoTribPDV.ST da Avant).</summary>
    public bool TemSubstituicaoTributaria { get; set; }
    public decimal MvaOriginal { get; set; }
    public decimal MvaAjustado4 { get; set; }
    public decimal MvaAjustado7 { get; set; }
    public decimal MvaAjustado12 { get; set; }
    public decimal AliquotaIcmsSt { get; set; }
    public decimal AliquotaFcpSt { get; set; }
    /// <summary>Alíquota ICMS de entrada específica para o UF da filial (o "ICMS Interno").</summary>
    public decimal AliquotaIcmsInternoEntrada { get; set; }

    // ── PIS ─────────────────────────────────────────────────────
    public string? CstPis { get; set; }
    public decimal AliquotaPis { get; set; }
    /// <summary>CST PIS para operações de entrada (compras). Ex: "70" — aquisição sem direito a crédito.</summary>
    public string? CstPisEntrada { get; set; }
    /// <summary>Código da natureza da receita para PIS/COFINS (ex: "01.01.00" ou "201 - Produtos farmacêuticos").</summary>
    public string? NaturezaReceita { get; set; }

    // ── COFINS ──────────────────────────────────────────────────
    public string? CstCofins { get; set; }
    public decimal AliquotaCofins { get; set; }
    /// <summary>CST COFINS para operações de entrada.</summary>
    public string? CstCofinsEntrada { get; set; }

    // ── IPI ─────────────────────────────────────────────────────
    public string? CstIpi { get; set; }
    public decimal AliquotaIpi { get; set; }
    /// <summary>Código de enquadramento do IPI (cEnq).</summary>
    public string? EnquadramentoIpi { get; set; }
    /// <summary>CST IPI para operações de entrada.</summary>
    public string? CstIpiEntrada { get; set; }
    public decimal AliquotaIpiEntrada { get; set; }
    /// <summary>% IPI destacado quando comprado diretamente de indústria.</summary>
    public decimal AliquotaIpiIndustria { get; set; }

    // ── Reforma Tributária (vigência 2026+) ─────────────────────
    /// <summary>CST do Imposto Seletivo (IS).</summary>
    public string? CstIs { get; set; }
    public string? ClassTribIs { get; set; }
    public decimal AliquotaIs { get; set; }
    /// <summary>CST IBS/CBS unificado.</summary>
    public string? CstIbsCbs { get; set; }
    public string? ClassTribIbsCbs { get; set; }
    /// <summary>Alíquota efetiva IBS UF (Imposto sobre Bens e Serviços estadual).</summary>
    public decimal AliquotaIbsUf { get; set; }
    /// <summary>Alíquota efetiva IBS Municipal.</summary>
    public decimal AliquotaIbsMun { get; set; }
    /// <summary>Alíquota efetiva CBS (Contribuição sobre Bens e Serviços federal).</summary>
    public decimal AliquotaCbs { get; set; }

    // ── Origem dos dados ────────────────────────────────────────
    /// <summary>Última atualização via Gestor Tributário (UTC).</summary>
    public DateTime? AtualizadoGestorTributarioEm { get; set; }
    /// <summary>Nome do provedor que atualizou (ex: "avant").</summary>
    public string? AtualizadoGestorTributarioProvider { get; set; }
}
