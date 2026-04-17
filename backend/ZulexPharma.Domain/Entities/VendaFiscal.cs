using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Dados do documento fiscal (NFe 55 ou NFC-e 65) emitido para uma Venda.
/// Relação 1:1 com Venda — só existe quando houve emissão.
/// Substitui as entidades Nfe/Nfce.
/// </summary>
public class VendaFiscal : BaseEntity
{
    public long VendaId { get; set; }
    public Venda Venda { get; set; } = null!;

    public ModeloDocumento Modelo { get; set; } = ModeloDocumento.Nfce;
    public FinalidadeDocumento Finalidade { get; set; } = FinalidadeDocumento.Normal;

    public int Numero { get; set; }
    public int Serie { get; set; }
    public string ChaveAcesso { get; set; } = string.Empty;
    public string? Protocolo { get; set; }
    public DateTime DataEmissao { get; set; }
    public DateTime? DataAutorizacao { get; set; }
    public DateTime? DataSaidaEntrada { get; set; }

    /// <summary>1=Produção, 2=Homologação.</summary>
    public int Ambiente { get; set; } = 2;

    /// <summary>0=Entrada, 1=Saída.</summary>
    public int TipoNf { get; set; } = 1;

    /// <summary>idDest: 1=Interna, 2=Interestadual, 3=Exterior.</summary>
    public int IdentificadorDestino { get; set; } = 1;

    public int CodigoStatus { get; set; }
    public string? MotivoStatus { get; set; }

    /// <summary>natOp do XML (ex: "VENDA", "DEVOLUCAO DE COMPRA", "TRANSFERENCIA").</summary>
    public string NatOp { get; set; } = string.Empty;

    public long? NaturezaOperacaoId { get; set; }
    public NaturezaOperacao? NaturezaOperacao { get; set; }

    // ── Transporte (apenas NFe com frete) ───────────────────────
    /// <summary>0=Emitente, 1=Destinatário, 2=Terceiros, 9=SemFrete.</summary>
    public int ModFrete { get; set; } = 9;
    public long? TransportadoraPessoaId { get; set; }
    public Pessoa? TransportadoraPessoa { get; set; }
    public string? PlacaVeiculo { get; set; }
    public string? UfVeiculo { get; set; }
    public int? VolumeQuantidade { get; set; }
    public string? VolumeEspecie { get; set; }
    public decimal? VolumePesoLiquido { get; set; }
    public decimal? VolumePesoBruto { get; set; }

    // ── Totais fiscais ──────────────────────────────────────────
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
    public decimal ValorNota { get; set; }
    public decimal ValorTotalTributos { get; set; }

    // ── XML ──────────────────────────────────────────────────────
    public string? XmlEnvio { get; set; }
    public string? XmlRetorno { get; set; }
    public string? XmlCancelamento { get; set; }
    public string? XmlCartaCorrecao { get; set; }

    /// <summary>Chave NFe referenciada (devolução/complemento).</summary>
    public string? ChaveNfeReferenciada { get; set; }
    public string? Observacao { get; set; }
}
