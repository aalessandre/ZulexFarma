using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.Promocoes;

public class PromocaoListDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoPromocao Tipo { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public DateTime? DataHoraFim { get; set; }
    public int DiaSemana { get; set; }
    public int TotalProdutos { get; set; }
    public bool LancarPorQuantidade { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class PromocaoDetalheDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoPromocao Tipo { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public DateTime? DataHoraFim { get; set; }
    public int DiaSemana { get; set; }
    public bool PermitirMudarPreco { get; set; }
    public bool GerarComissao { get; set; }
    public bool ExclusivaConvenio { get; set; }
    public decimal ReducaoVendaPrazo { get; set; }
    public int? QtdeMaxPorVenda { get; set; }
    public bool LancarPorQuantidade { get; set; }
    public DateTime? DataInicioContagem { get; set; }
    public bool Intersabores { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public List<long> FilialIds { get; set; } = new();
    public List<long> PagamentoIds { get; set; } = new();
    public List<long> ConvenioIds { get; set; } = new();
    public List<PromocaoProdutoDto> Produtos { get; set; } = new();
    public List<PromocaoFaixaDto> Faixas { get; set; } = new();
}

public class PromocaoFaixaDto
{
    public int Quantidade { get; set; }
    public decimal PercentualDesconto { get; set; }
}

public class PromocaoFormDto
{
    public string Nome { get; set; } = string.Empty;
    public TipoPromocao Tipo { get; set; } = TipoPromocao.Fixa;
    public DateTime DataHoraInicio { get; set; }
    public DateTime? DataHoraFim { get; set; }
    public int DiaSemana { get; set; } = 127;
    public bool PermitirMudarPreco { get; set; }
    public bool GerarComissao { get; set; }
    public bool ExclusivaConvenio { get; set; }
    public decimal ReducaoVendaPrazo { get; set; }
    public int? QtdeMaxPorVenda { get; set; }
    public bool LancarPorQuantidade { get; set; }
    public DateTime? DataInicioContagem { get; set; }
    public bool Intersabores { get; set; }
    public bool Ativo { get; set; } = true;
    public List<long> FilialIds { get; set; } = new();
    public List<long> PagamentoIds { get; set; } = new();
    public List<long> ConvenioIds { get; set; } = new();
    public List<PromocaoProdutoFormDto> Produtos { get; set; } = new();
    public List<PromocaoFaixaDto> Faixas { get; set; } = new();
}

public class PromocaoProdutoDto
{
    public long Id { get; set; }
    public long ProdutoId { get; set; }
    public string? ProdutoCodigo { get; set; }
    public string ProdutoNome { get; set; } = string.Empty;
    public string? Fabricante { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal CustoMedio { get; set; }
    public decimal EstoqueAtual { get; set; }
    public string? Curva { get; set; }
    public decimal PercentualPromocao { get; set; }
    public decimal ValorPromocao { get; set; }
    public decimal PercentualLucro { get; set; }
    public int? QtdeLimite { get; set; }
    public int QtdeVendida { get; set; }
    public decimal? PercentualAposLimite { get; set; }
    public decimal? ValorAposLimite { get; set; }
}

public class PromocaoProdutoFormDto
{
    public long ProdutoId { get; set; }
    public decimal PercentualPromocao { get; set; }
    public decimal ValorPromocao { get; set; }
    public decimal PercentualLucro { get; set; }
    public int? QtdeLimite { get; set; }
    public decimal? PercentualAposLimite { get; set; }
    public decimal? ValorAposLimite { get; set; }
}
