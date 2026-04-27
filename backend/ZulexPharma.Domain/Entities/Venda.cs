using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Entities.SelfCheckout;

namespace ZulexPharma.Domain.Entities;

public class Venda : BaseEntity
{
    public long FilialId { get; set; }
    public Filial? Filial { get; set; }
    public long? CaixaId { get; set; }
    public Caixa? Caixa { get; set; }
    public long? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public long? ColaboradorId { get; set; }
    public Colaborador? Colaborador { get; set; }
    public long? TipoPagamentoId { get; set; }
    public TipoPagamento? TipoPagamento { get; set; }
    public long? ConvenioId { get; set; }

    /// <summary>Número da cesta informado pelo usuário (opcional, configurável).</summary>
    public string? NrCesta { get; set; }

    /// <summary>Origem: PreVenda, Caixa ou SelfCheckout.</summary>
    public VendaOrigem Origem { get; set; } = VendaOrigem.PreVenda;

    /// <summary>Terminal de Self-Checkout que gerou a venda. Null para vendas do caixa atendido / PreVenda.</summary>
    public long? SelfCheckoutTerminalId { get; set; }
    public SelfCheckoutTerminal? SelfCheckoutTerminal { get; set; }

    // ── Tipo de operação e documento fiscal ────────────────────
    /// <summary>Venda comercial, transferência, perda, ajuste, etc.</summary>
    public TipoOperacao TipoOperacao { get; set; } = TipoOperacao.Venda;

    /// <summary>Modelo de documento fiscal a emitir (ou já emitido).</summary>
    public ModeloDocumento ModeloDocumento { get; set; } = ModeloDocumento.SemDocumento;

    /// <summary>Status do documento fiscal. NaoEmitido = sem emissão.</summary>
    public StatusFiscal StatusFiscal { get; set; } = StatusFiscal.NaoEmitido;

    /// <summary>Natureza de operação (define CFOP/CST padrão na emissão).</summary>
    public long? NaturezaOperacaoId { get; set; }
    public NaturezaOperacao? NaturezaOperacao { get; set; }

    /// <summary>Destinatário do documento fiscal quando diferente do Cliente (ex: outra filial, empresa).</summary>
    public long? DestinatarioPessoaId { get; set; }
    public Pessoa? DestinatarioPessoa { get; set; }

    // ── Campos de movimento de estoque (preenchidos conforme TipoOperacao) ──
    /// <summary>Filial de destino (apenas Transferencia).</summary>
    public long? FilialDestinoId { get; set; }
    public Filial? FilialDestino { get; set; }

    /// <summary>Motivo da perda (apenas TipoOperacao=Perda).</summary>
    public MotivoPerda? Motivo { get; set; }

    /// <summary>Número do Boletim de Ocorrência (obrigatório para Furto/Roubo).</summary>
    public string? NumeroBoletim { get; set; }

    // ── Totais ──────────────────────────────────────────────────
    public decimal TotalBruto { get; set; }
    public decimal TotalDesconto { get; set; }
    public decimal TotalLiquido { get; set; }
    public int TotalItens { get; set; }

    /// <summary>
    /// Outras Despesas Acessórias (RN-13 da spec entregas-precificacao).
    /// Hoje preenchido só pela taxa de entrega vinda de EntregaService.CalcularAsync.
    /// Entra em vNF do NFC-e via &lt;vOutro&gt;. TotalLiquido NÃO inclui — é derivado.
    /// </summary>
    public decimal ValorOutrasDespesas { get; set; }

    // ── Datas do ciclo de vida ────────────────────────────────
    /// <summary>Data da pré-venda (quando o registro foi criado). Igual a CriadoEm.</summary>
    public DateTime DataPreVenda { get; set; } = Helpers.DataHoraHelper.Agora();

    /// <summary>Data em que a venda foi finalizada no caixa.</summary>
    public DateTime? DataFinalizacao { get; set; }

    /// <summary>Data em que o cupom fiscal (NFC-e) foi emitido.</summary>
    public DateTime? DataEmissaoCupom { get; set; }

    // ── Status ──────────────────────────────────────────────────
    public VendaStatus Status { get; set; } = VendaStatus.Aberta;
    public string? Observacao { get; set; }

    /// <summary>
    /// Marca venda com itens controlados SNGPC cujas receitas ainda não foram lançadas.
    /// Setado no modo <c>NaoLancar</c> (sempre) ou no modo <c>Misto</c> (quando o operador
    /// escolhe "Lançar Depois" com senha de supervisor).
    /// </summary>
    public bool SngpcPendente { get; set; }

    // ── Entrega (solicitação na pré-venda; Entrega efetiva criada no caixa ao finalizar) ──
    public bool EntregaSolicitada { get; set; }
    public long? EntregaEnderecoId { get; set; }
    public PessoaEndereco? EntregaEndereco { get; set; }
    public string? EntregaObservacao { get; set; }

    // ── Pagamento diferido (entregas: caixa contabiliza só na baixa) ──
    /// <summary>True quando o caixa já contabilizou (CaixaMovimentos criados). Default true em venda sem entrega.</summary>
    public bool PagamentoRecebido { get; set; } = true;
    public DateTime? DataPagamentoRecebido { get; set; }
    /// <summary>Caixa que contabilizou o recebimento (pode diferir do CaixaId original).</summary>
    public long? CaixaRecebimentoId { get; set; }
    public Caixa? CaixaRecebimento { get; set; }

    // ── Navigation ─────────────────────────────────────────────
    public ICollection<VendaItem> Itens { get; set; } = new List<VendaItem>();
    public ICollection<VendaPagamento> Pagamentos { get; set; } = new List<VendaPagamento>();
    public ICollection<VendaReceita> Receitas { get; set; } = new List<VendaReceita>();

    /// <summary>Dados do documento fiscal emitido (1:1, null se não emitido).</summary>
    public VendaFiscal? Fiscal { get; set; }
}
