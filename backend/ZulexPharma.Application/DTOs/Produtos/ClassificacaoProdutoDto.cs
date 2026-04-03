namespace ZulexPharma.Application.DTOs.Produtos;

public class ClassificacaoListDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal ComissaoPercentual { get; set; }
    public decimal MarkupPadrao { get; set; }
    public decimal ProjecaoLucro { get; set; }
    public DateTime CriadoEm { get; set; }
    public bool Ativo { get; set; }
}

public class ClassificacaoFormDto
{
    public string Nome { get; set; } = string.Empty;
    public decimal ComissaoPercentual { get; set; } = 0;
    public decimal DescontoMinimo { get; set; } = 0;
    public decimal DescontoMaximo { get; set; } = 0;
    public decimal DescontoMaximoComSenha { get; set; } = 0;
    public decimal ProjecaoLucro { get; set; } = 30;
    public decimal MarkupPadrao { get; set; } = 50;
    public string FormacaoPreco { get; set; } = "MARKUP";
    public string BaseCalculo { get; set; } = "CUSTO_COMPRA";
    public bool ControlarLotesVencimento { get; set; } = false;
    public bool InformarPrescritorVenda { get; set; } = false;
    public bool ImprimirEtiqueta { get; set; } = false;
    public bool PermitirDescontoPrazo { get; set; } = false;
    public bool PermitirPromocao { get; set; } = false;
    public bool PermitirDescontosProgressivos { get; set; } = false;
    public bool AtualizarAbcFarma { get; set; } = true;
    public bool Ativo { get; set; } = true;
}

public class ClassificacaoDetalheDto : ClassificacaoFormDto
{
    public long Id { get; set; }
    public DateTime CriadoEm { get; set; }
}
