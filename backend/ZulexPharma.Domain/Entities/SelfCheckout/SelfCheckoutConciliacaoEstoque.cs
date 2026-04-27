namespace ZulexPharma.Domain.Entities.SelfCheckout;

/// <summary>
/// Fila de baixa de estoque pendente no ERP origem. 1:1 com VendaItem
/// de venda do self-checkout (Venda.Origem = SelfCheckout).
///
/// Fase 1: registros são gerados e ficam pendentes (ProcessadoEm = null).
/// Conciliação manual via relatório.
///
/// Fase 3 (futuro): processador automático escreve no Inovafarma e seta ProcessadoEm.
/// </summary>
public class SelfCheckoutConciliacaoEstoque : BaseEntity
{
    public long VendaItemId { get; set; }
    public VendaItem? VendaItem { get; set; }

    /// <summary>Código do produto no ERP externo (ex: CodigoProduto do Inovafarma).</summary>
    public string CodigoProdutoExterno { get; set; } = string.Empty;

    /// <summary>EAN bipado (cópia para auditoria, mesmo se mudar no ERP origem).</summary>
    public string? CodigoBarrasExterno { get; set; }

    public int Quantidade { get; set; }

    /// <summary>Null = pendente. Preenchido quando a baixa for efetivada no ERP origem.</summary>
    public DateTime? ProcessadoEm { get; set; }

    /// <summary>Mensagem de erro do último processamento (se falhou).</summary>
    public string? UltimoErro { get; set; }
}
