namespace ZulexPharma.Application.DTOs.FarmaciaPopular;

/// <summary>
/// Bloco opcional enviado junto com VendaFormDto quando a aba é "Farmácia Popular".
/// Presente => o backend materializa VendaFarmaciaPopular + Itens.
/// </summary>
public class VendaFarmaciaPopularFormDto
{
    public long? PrescritorId { get; set; }
    public string CrmMedico { get; set; } = string.Empty;
    public string UfCrm { get; set; } = string.Empty;
    public DateOnly? DtEmissaoReceita { get; set; }
    public string? NuReceita { get; set; }
    public bool BolsaFamilia { get; set; }
    public List<VendaFarmaciaPopularItemFormDto> Itens { get; set; } = new();
}

public class VendaFarmaciaPopularItemFormDto
{
    /// <summary>Id do VendaItem (vem no form como referência opcional; no create, case com a ordem do item).</summary>
    public long? VendaItemId { get; set; }
    public long ProdutoId { get; set; }
    /// <summary>Informativo. O backend ignora este valor e usa Produto.CodigoBarras como fonte canônica.</summary>
    public string CodigoBarraEAN { get; set; } = string.Empty;
    public decimal QtPrescrita { get; set; }
    public decimal QtSolicitada { get; set; }
    public decimal VlPrecoVenda { get; set; }
}

// ─────────────────────────────────────────────────────────────────
// Retornos do SOAP DATASUS
// ─────────────────────────────────────────────────────────────────

public class SolicitacaoRetornoDto
{
    public bool Sucesso { get; set; }
    public string CodigoRetorno { get; set; } = string.Empty;
    public string? MensagemRetorno { get; set; }
    public string? NuAutorizacao { get; set; }
    public string? NoPaciente { get; set; }
    public string? RequestXml { get; set; }
    public string? ResponseXml { get; set; }
    public List<SolicitacaoRetornoItemDto> Itens { get; set; } = new();
}

public class SolicitacaoRetornoItemDto
{
    public string CodigoBarraEAN { get; set; } = string.Empty;
    public string? CodigoRetornoItem { get; set; }
    public string? MensagemRetornoItem { get; set; }
    public decimal? QtAutorizada { get; set; }
    public decimal? VlPrecoSubsidiadoMS { get; set; }
    public decimal? VlPrecoSubsidiadoPaciente { get; set; }
    public string? InAutorizacaoMedicamento { get; set; }
}

public class ConfirmacaoRetornoDto
{
    public bool Sucesso { get; set; }
    public string CodigoRetorno { get; set; } = string.Empty;
    public string? MensagemRetorno { get; set; }
    public string? RequestXml { get; set; }
    public string? ResponseXml { get; set; }
}
