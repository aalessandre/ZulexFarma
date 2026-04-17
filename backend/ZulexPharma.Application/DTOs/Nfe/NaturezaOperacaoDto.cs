namespace ZulexPharma.Application.DTOs.Nfe;

public class NaturezaOperacaoListDto
{
    public long Id { get; set; }
    public string? Codigo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int TipoNf { get; set; }
    public int FinalidadeNfe { get; set; }
    public bool MovimentaEstoque { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class NaturezaOperacaoDetalheDto : NaturezaOperacaoListDto
{
    public int IdentificadorDestino { get; set; }
    public bool RelacionarDocumentoFiscal { get; set; }
    public bool UtilizarPrecoCusto { get; set; }
    public bool ReajustarCustoMedio { get; set; }
    public bool GeraFinanceiro { get; set; }
    public int? TipoMovimentoEstoque { get; set; }
    public string? CstPisPadrao { get; set; }
    public string? CstCofinsPadrao { get; set; }
    public string? CstIpiPadrao { get; set; }
    public string? EnquadramentoIpiPadrao { get; set; }
    public int IndicadorPresenca { get; set; }
    public int IndicadorFinalidade { get; set; }
    public string? Observacao { get; set; }
    public List<NaturezaOperacaoRegraDto> Regras { get; set; } = new();
}

public class NaturezaOperacaoFormDto
{
    public string Descricao { get; set; } = string.Empty;
    public int TipoNf { get; set; } = 1;
    public int FinalidadeNfe { get; set; } = 1;
    public int IdentificadorDestino { get; set; } = 1;
    public bool RelacionarDocumentoFiscal { get; set; }
    public bool UtilizarPrecoCusto { get; set; }
    public bool ReajustarCustoMedio { get; set; }
    public bool GeraFinanceiro { get; set; }
    public bool MovimentaEstoque { get; set; }
    public int? TipoMovimentoEstoque { get; set; }
    public string? CstPisPadrao { get; set; }
    public string? CstCofinsPadrao { get; set; }
    public string? CstIpiPadrao { get; set; }
    public string? EnquadramentoIpiPadrao { get; set; }
    public int IndicadorPresenca { get; set; } = 0;
    public int IndicadorFinalidade { get; set; } = 0;
    public string? Observacao { get; set; }
    public bool Ativo { get; set; } = true;
    public List<NaturezaOperacaoRegraDto> Regras { get; set; } = new();
}

public class NaturezaOperacaoRegraDto
{
    public long? Id { get; set; }
    public int CenarioTributario { get; set; }
    public string? CfopInterno { get; set; }
    public string? CfopInterestadual { get; set; }
    public string? CstIcmsInterno { get; set; }
    public string? CstIcmsInterestadual { get; set; }
    public string? CodigoBeneficioInterno { get; set; }
    public string? CodigoBeneficioInterestadual { get; set; }
}
