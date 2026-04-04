using System.Text.Json.Serialization;
using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>Cabeçalho da nota fiscal de entrada (compra).</summary>
public class Compra : BaseEntity
{
    public long FilialId { get; set; }

    public long FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;

    // ── Identificação NF ────────────────────────────────────────
    public string ChaveNfe { get; set; } = string.Empty;
    public string NumeroNf { get; set; } = string.Empty;
    public string? SerieNf { get; set; }
    public string? NaturezaOperacao { get; set; }
    public DateTime? DataEmissao { get; set; }
    public DateTime? DataEntrada { get; set; }

    // ── Totais ──────────────────────────────────────────────────
    public decimal ValorProdutos { get; set; }
    public decimal ValorSt { get; set; }
    public decimal ValorFcpSt { get; set; }
    public decimal ValorFrete { get; set; }
    public decimal ValorSeguro { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorIpi { get; set; }
    public decimal ValorPis { get; set; }
    public decimal ValorCofins { get; set; }
    public decimal ValorOutros { get; set; }
    public decimal ValorNota { get; set; }

    // ── Status ──────────────────────────────────────────────────
    public CompraStatus Status { get; set; } = CompraStatus.PreEntrada;

    // ── Finalização ─────────────────────────────────────────────
    public bool DuplicatasEntregues { get; set; }
    public bool NotaPaga { get; set; }
    public DateTime? DataFinalizacao { get; set; }

    // ── XML ─────────────────────────────────────────────────────
    /// <summary>XML completo da NF-e. Excluído do sync para economizar tráfego.</summary>
    [JsonIgnore]
    public string? XmlConteudo { get; set; }

    // ── Navigation ──────────────────────────────────────────────
    public ICollection<CompraProduto> Produtos { get; set; } = new List<CompraProduto>();
}
