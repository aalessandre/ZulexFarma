namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Natureza de operação fiscal. Define o comportamento padrão de uma NF-e modelo 55.
/// A parametrização de CFOP/CST/Benefício é feita por cenário tributário via NaturezaOperacaoRegra.
/// </summary>
public class NaturezaOperacao : BaseEntity
{
    public string Descricao { get; set; } = string.Empty;

    /// <summary>0 = Entrada, 1 = Saída.</summary>
    public int TipoNf { get; set; } = 1;

    /// <summary>1=Normal, 2=Complementar, 3=Ajuste, 4=Devolução.</summary>
    public int FinalidadeNfe { get; set; } = 1;

    /// <summary>idDest: 1=Interna, 2=Interestadual, 3=Exterior.</summary>
    public int IdentificadorDestino { get; set; } = 1;

    // ── Flags de comportamento ──────────────────────────────────────
    /// <summary>Se true, a NF-e referencia outro documento fiscal (refNFe).</summary>
    public bool RelacionarDocumentoFiscal { get; set; }

    /// <summary>Se true, usa preço de custo no valor do item (ao invés de preço de venda).</summary>
    public bool UtilizarPrecoCusto { get; set; }

    /// <summary>Se true, reajusta o custo médio dos produtos na autorização.</summary>
    public bool ReajustarCustoMedio { get; set; }

    /// <summary>Se true, gera lançamento financeiro (Conta a Pagar/Receber).</summary>
    public bool GeraFinanceiro { get; set; }

    /// <summary>Se true, movimenta estoque na autorização.</summary>
    public bool MovimentaEstoque { get; set; }

    /// <summary>+1=Entrada no estoque, -1=Saída do estoque. Null se não movimenta.</summary>
    public int? TipoMovimentoEstoque { get; set; }

    // ── CSTs padrão PIS/COFINS/IPI (não variam por cenário) ─────────
    public string? CstPisPadrao { get; set; }
    public string? CstCofinsPadrao { get; set; }
    public string? CstIpiPadrao { get; set; }
    public string? EnquadramentoIpiPadrao { get; set; }

    /// <summary>indPres: 0=NaoSeAplica, 1=Presencial, 9=Outros.</summary>
    public int IndicadorPresenca { get; set; } = 0;

    /// <summary>indFinal: 0=Normal (revenda), 1=Consumidor final.</summary>
    public int IndicadorFinalidade { get; set; } = 0;

    /// <summary>Texto padrão para infAdic/infCpl.</summary>
    public string? Observacao { get; set; }

    // ── Regras por cenário tributário ────────────────────────────────
    public ICollection<NaturezaOperacaoRegra> Regras { get; set; } = new List<NaturezaOperacaoRegra>();
}
