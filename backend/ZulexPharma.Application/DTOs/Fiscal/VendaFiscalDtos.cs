using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.Fiscal;

/// <summary>
/// DTOs para emissão fiscal unificada (NFe modelo 55 + NFC-e modelo 65).
/// Substituem NfeDto.cs. Parcelas vão em ContaReceber (não em DTO de NFe).
/// </summary>

public class VendaFiscalListDto
{
    public long Id { get; set; }              // VendaFiscal.Id
    public long VendaId { get; set; }
    public ModeloDocumento Modelo { get; set; }
    public int Numero { get; set; }
    public int Serie { get; set; }
    public string NatOp { get; set; } = string.Empty;
    public string? DestinatarioNome { get; set; }
    public string? DestinatarioCpfCnpj { get; set; }
    public DateTime DataEmissao { get; set; }
    public decimal ValorNota { get; set; }
    public StatusFiscal Status { get; set; }
    public string ChaveAcesso { get; set; } = string.Empty;
    public int TipoNf { get; set; }
    public FinalidadeDocumento Finalidade { get; set; }
    public TipoOperacao TipoOperacao { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class VendaFiscalDetalheDto : VendaFiscalListDto
{
    public long FilialId { get; set; }
    public long? NaturezaOperacaoId { get; set; }
    public string? NaturezaOperacaoDescricao { get; set; }
    public long? DestinatarioPessoaId { get; set; }
    public string? Protocolo { get; set; }
    public DateTime? DataAutorizacao { get; set; }
    public DateTime? DataSaidaEntrada { get; set; }
    public int Ambiente { get; set; }
    public int IdentificadorDestino { get; set; }
    public int CodigoStatus { get; set; }
    public string? MotivoStatus { get; set; }

    // Transporte
    public int ModFrete { get; set; }
    public long? TransportadoraPessoaId { get; set; }
    public string? TransportadoraNome { get; set; }
    public string? PlacaVeiculo { get; set; }
    public string? UfVeiculo { get; set; }
    public int? VolumeQuantidade { get; set; }
    public string? VolumeEspecie { get; set; }
    public decimal? VolumePesoLiquido { get; set; }
    public decimal? VolumePesoBruto { get; set; }

    // Totais
    public decimal ValorProdutos { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorFrete { get; set; }
    public decimal ValorSeguro { get; set; }
    public decimal ValorOutros { get; set; }
    public decimal ValorIcms { get; set; }
    public decimal ValorIcmsSt { get; set; }
    public decimal ValorIpi { get; set; }
    public decimal ValorPis { get; set; }
    public decimal ValorCofins { get; set; }
    public decimal ValorTotalTributos { get; set; }

    // Referencia
    public string? ChaveNfeReferenciada { get; set; }
    public string? Observacao { get; set; }

    // XML
    public string? XmlEnvio { get; set; }
    public string? XmlRetorno { get; set; }
    public string? XmlCancelamento { get; set; }
    public string? XmlCartaCorrecao { get; set; }

    public List<VendaItemFiscalDto> Itens { get; set; } = new();
}

public class VendaItemFiscalDto
{
    public long Id { get; set; }
    public long VendaItemId { get; set; }
    public int NumeroItem { get; set; }
    public long ProdutoId { get; set; }
    public long? ProdutoLoteId { get; set; }
    public string CodigoProduto { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = "SEM GTIN";
    public string DescricaoProduto { get; set; } = string.Empty;
    public string Ncm { get; set; } = string.Empty;
    public string? Cest { get; set; }
    public string Cfop { get; set; } = string.Empty;
    public string Unidade { get; set; } = "UN";
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorFrete { get; set; }
    public decimal ValorSeguro { get; set; }
    public decimal ValorOutros { get; set; }

    // Medicamento / Rastro
    public string? CodigoAnvisa { get; set; }
    public string? RastroLote { get; set; }
    public DateTime? RastroFabricacao { get; set; }
    public DateTime? RastroValidade { get; set; }
    public decimal? RastroQuantidade { get; set; }

    // ICMS
    public string OrigemMercadoria { get; set; } = "0";
    public string? CstIcms { get; set; }
    public string? Csosn { get; set; }
    public decimal BaseIcms { get; set; }
    public decimal AliquotaIcms { get; set; }
    public decimal ValorIcms { get; set; }
    public decimal PercentualReducaoBc { get; set; }
    public string? CodigoBeneficioFiscal { get; set; }

    // ICMS-ST
    public decimal MvaSt { get; set; }
    public decimal BaseIcmsSt { get; set; }
    public decimal AliquotaIcmsSt { get; set; }
    public decimal ValorIcmsSt { get; set; }

    // FCP
    public decimal BaseFcp { get; set; }
    public decimal AliquotaFcp { get; set; }
    public decimal ValorFcp { get; set; }

    // PIS
    public string CstPis { get; set; } = "49";
    public decimal BasePis { get; set; }
    public decimal AliquotaPis { get; set; }
    public decimal ValorPis { get; set; }

    // COFINS
    public string CstCofins { get; set; } = "49";
    public decimal BaseCofins { get; set; }
    public decimal AliquotaCofins { get; set; }
    public decimal ValorCofins { get; set; }

    // IPI
    public string? CstIpi { get; set; }
    public string? EnquadramentoIpi { get; set; }
    public decimal BaseIpi { get; set; }
    public decimal AliquotaIpi { get; set; }
    public decimal ValorIpi { get; set; }

    public decimal ValorTotalTributos { get; set; }
    public decimal? CustoUnitario { get; set; }
}

// ── Form DTOs (criar/atualizar rascunho NFe) ────────────────────────

public class VendaFiscalFormDto
{
    public long FilialId { get; set; }
    public TipoOperacao TipoOperacao { get; set; } = TipoOperacao.Venda;
    public ModeloDocumento Modelo { get; set; } = ModeloDocumento.Nfe;
    public long NaturezaOperacaoId { get; set; }
    public long? DestinatarioPessoaId { get; set; }
    public long? FilialDestinoId { get; set; }
    public DateTime? DataSaidaEntrada { get; set; }
    public string? ChaveNfeReferenciada { get; set; }
    public string? Observacao { get; set; }

    // Transporte
    public int ModFrete { get; set; } = 9;
    public long? TransportadoraPessoaId { get; set; }
    public string? PlacaVeiculo { get; set; }
    public string? UfVeiculo { get; set; }
    public int? VolumeQuantidade { get; set; }
    public string? VolumeEspecie { get; set; }
    public decimal? VolumePesoLiquido { get; set; }
    public decimal? VolumePesoBruto { get; set; }

    public List<VendaItemFiscalFormDto> Itens { get; set; } = new();
}

public class VendaItemFiscalFormDto
{
    public long? Id { get; set; }
    public long ProdutoId { get; set; }
    public long? ProdutoLoteId { get; set; }
    public string CodigoProduto { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = "SEM GTIN";
    public string DescricaoProduto { get; set; } = string.Empty;
    public string Ncm { get; set; } = string.Empty;
    public string? Cest { get; set; }
    public string Cfop { get; set; } = string.Empty;
    public string Unidade { get; set; } = "UN";
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorFrete { get; set; }
    public decimal ValorSeguro { get; set; }
    public decimal ValorOutros { get; set; }

    // Medicamento / Rastro
    public string? CodigoAnvisa { get; set; }
    public string? RastroLote { get; set; }
    public DateTime? RastroFabricacao { get; set; }
    public DateTime? RastroValidade { get; set; }
    public decimal? RastroQuantidade { get; set; }

    // ICMS
    public string OrigemMercadoria { get; set; } = "0";
    public string? CstIcms { get; set; }
    public string? Csosn { get; set; }
    public string? ModBcIcms { get; set; }
    public decimal BaseIcms { get; set; }
    public decimal AliquotaIcms { get; set; }
    public decimal ValorIcms { get; set; }
    public decimal PercentualReducaoBc { get; set; }
    public decimal ValorIcmsDesonerado { get; set; }
    public string? MotivoDesoneracaoIcms { get; set; }
    public string? CodigoBeneficioFiscal { get; set; }

    // ICMS-ST
    public string? ModBcIcmsSt { get; set; }
    public decimal MvaSt { get; set; }
    public decimal BaseIcmsSt { get; set; }
    public decimal AliquotaIcmsSt { get; set; }
    public decimal ValorIcmsSt { get; set; }

    // FCP
    public decimal BaseFcp { get; set; }
    public decimal AliquotaFcp { get; set; }
    public decimal ValorFcp { get; set; }
    public decimal BaseFcpSt { get; set; }
    public decimal AliquotaFcpSt { get; set; }
    public decimal ValorFcpSt { get; set; }

    // PIS
    public string CstPis { get; set; } = "49";
    public decimal BasePis { get; set; }
    public decimal AliquotaPis { get; set; }
    public decimal ValorPis { get; set; }

    // COFINS
    public string CstCofins { get; set; } = "49";
    public decimal BaseCofins { get; set; }
    public decimal AliquotaCofins { get; set; }
    public decimal ValorCofins { get; set; }

    // IPI
    public string? CstIpi { get; set; }
    public string? EnquadramentoIpi { get; set; }
    public decimal BaseIpi { get; set; }
    public decimal AliquotaIpi { get; set; }
    public decimal ValorIpi { get; set; }

    public decimal ValorTotalTributos { get; set; }
}

// ── Result DTOs ─────────────────────────────────────────────

public class VendaFiscalEmissaoResult
{
    public long VendaFiscalId { get; set; }
    public long VendaId { get; set; }
    public ModeloDocumento Modelo { get; set; }
    public int Numero { get; set; }
    public int Serie { get; set; }
    public string ChaveAcesso { get; set; } = string.Empty;
    public string? Protocolo { get; set; }
    public int CodigoStatus { get; set; }
    public string? MotivoStatus { get; set; }
    public bool Autorizada { get; set; }
}

public class VendaFiscalEventoResult
{
    public bool Sucesso { get; set; }
    public int CodigoStatus { get; set; }
    public string? MotivoStatus { get; set; }
    public string? Protocolo { get; set; }
    public string? XmlEvento { get; set; }
}
