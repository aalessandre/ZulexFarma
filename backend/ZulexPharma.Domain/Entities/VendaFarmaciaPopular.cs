using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Cabeçalho da venda via programa Farmácia Popular (DATASUS/MS).
/// 1:1 com Venda — só existe quando a venda tem itens FP autorizados.
/// Não duplica totais/cliente/caixa — reusa os da Venda.
/// </summary>
public class VendaFarmaciaPopular : BaseEntity
{
    public long VendaId { get; set; }
    public Venda? Venda { get; set; }

    /// <summary>Código único gerado pelo PDV (formato sugerido: {filialId}-{vendaId}-{timestamp}).</summary>
    public string CoSolicitacaoFarmacia { get; set; } = string.Empty;

    /// <summary>Retorno da Fase 1 (formato 998.467.862.438.252). Conecta as 3 fases.</summary>
    public string? NuAutorizacao { get; set; }

    /// <summary>Número do cupom fiscal (= VendaFiscal.Numero da NFC-e emitida).</summary>
    public string? NuCupomFiscal { get; set; }

    /// <summary>Saída do gbasmsb.exe na Fase 1 — identificador único da transação.</summary>
    public string? DnaEstacao { get; set; }

    // ── Snapshot da transação (obrigatórios no momento da Fase 1) ────
    public string CnpjEstabelecimento { get; set; } = string.Empty;
    public string CpfPaciente { get; set; } = string.Empty;
    public string? NoPaciente { get; set; }

    /// <summary>Default false; true usa FpPrecoTabelaBolsaFamilia do produto.</summary>
    public bool BolsaFamilia { get; set; }

    public string CrmMedico { get; set; } = string.Empty;
    public string UfCrm { get; set; } = string.Empty;
    public DateOnly DtEmissaoReceita { get; set; }
    public string? NuReceita { get; set; }

    /// <summary>Opcional — se conseguir match por CRM+UF na tabela Prescritor (SNGPC).</summary>
    public long? PrescritorId { get; set; }
    public Prescritor? Prescritor { get; set; }

    // ── Estado do ciclo ──────────────────────────────────────────────
    public StatusFarmaciaPopular Status { get; set; } = StatusFarmaciaPopular.Iniciada;
    public FaseFarmaciaPopular FaseAtual { get; set; } = FaseFarmaciaPopular.Solicitacao;
    public string? CodigoRetornoAtual { get; set; }
    public string? MensagemRetornoAtual { get; set; }

    /// <summary>True se estorno tentou e falhou — admin precisa tratar manualmente.</summary>
    public bool EstornoPendente { get; set; }

    // ── Auditoria de XMLs (request/response + timestamp de cada fase) ─
    public string? Fase1RequestXml { get; set; }
    public string? Fase1ResponseXml { get; set; }
    public DateTime? Fase1DataHora { get; set; }

    public string? Fase2RequestXml { get; set; }
    public string? Fase2ResponseXml { get; set; }
    public DateTime? Fase2DataHora { get; set; }

    public string? Fase3RequestXml { get; set; }
    public string? Fase3ResponseXml { get; set; }
    public DateTime? Fase3DataHora { get; set; }

    public string? EstornoRequestXml { get; set; }
    public string? EstornoResponseXml { get; set; }
    public DateTime? EstornoDataHora { get; set; }

    public ICollection<VendaFarmaciaPopularItem> Itens { get; set; } = new List<VendaFarmaciaPopularItem>();
}
