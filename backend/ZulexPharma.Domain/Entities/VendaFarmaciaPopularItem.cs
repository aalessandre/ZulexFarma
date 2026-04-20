namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Dados específicos do programa FP por item da venda.
/// 1:1 com VendaItem — só existe pros itens autorizados via DATASUS.
/// Preço, quantidade e produto ficam em VendaItem (não duplicados aqui).
/// </summary>
public class VendaFarmaciaPopularItem : BaseEntity
{
    public long VendaFarmaciaPopularId { get; set; }
    public VendaFarmaciaPopular? VendaFarmaciaPopular { get; set; }

    public long VendaItemId { get; set; }
    public VendaItem? VendaItem { get; set; }

    /// <summary>Snapshot do EAN enviado na Fase 1 (em caso do EAN do cadastro mudar depois).</summary>
    public string CodigoBarraEAN { get; set; } = string.Empty;

    /// <summary>Posologia diária (qtPrescrita no DATASUS). Insulina/líquidos usam 1.</summary>
    public decimal QtPrescrita { get; set; }

    /// <summary>Quantidade solicitada na Fase 1 (em unidades-base: comprimidos/ml).</summary>
    public decimal QtSolicitada { get; set; }

    /// <summary>Quantidade aprovada pelo DATASUS (retorno Fase 1; pode ser menor que QtSolicitada).</summary>
    public decimal? QtAutorizada { get; set; }

    /// <summary>Quantidade efetivamente dispensada (enviada na Fase 3). Normalmente = QtAutorizada.</summary>
    public decimal? QtDispensada { get; set; }

    /// <summary>Quantidade confirmada pelo DATASUS em estorno.</summary>
    public decimal? QtEstornada { get; set; }

    /// <summary>Preço enviado ao DATASUS (FpPrecoTabela ou FpPrecoTabelaBolsaFamilia do produto).</summary>
    public decimal VlPrecoVenda { get; set; }

    /// <summary>Parte subsidiada pelo governo (retorno DATASUS).</summary>
    public decimal? VlPrecoSubsidiadoMS { get; set; }

    /// <summary>Parte paga pelo paciente (retorno DATASUS).</summary>
    public decimal? VlPrecoSubsidiadoPaciente { get; set; }

    /// <summary>Código do retorno por item (ex: 00SM = autorizado).</summary>
    public string? CodigoRetornoItem { get; set; }
    public string? MensagemRetornoItem { get; set; }

    /// <summary>Valor bruto do campo inAutorizacaoMedicamento do DATASUS.</summary>
    public string? InAutorizacaoMedicamento { get; set; }
}
