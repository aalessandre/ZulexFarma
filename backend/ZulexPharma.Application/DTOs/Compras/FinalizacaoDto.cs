namespace ZulexPharma.Application.DTOs.Compras;

public class FinalizarCompraRequest
{
    public long CompraId { get; set; }
    public bool DuplicatasEntregues { get; set; }
    public bool NotaPaga { get; set; }
    public string? NomeUsuario { get; set; }
    public List<DuplicataEditDto> Duplicatas { get; set; } = new();
    public List<LoteEditDto> Lotes { get; set; } = new();
    /// <summary>
    /// Desmembramento por item de grade: distribui a quantidade da nota entre os SKUs.
    /// Itens sem desmembramento entram no estoque simples (linha-base).
    /// </summary>
    public List<DesmembramentoItemDto> Desmembramentos { get; set; } = new();
}

public class DesmembramentoItemDto
{
    public long CompraProdutoId { get; set; }
    public List<DesmembramentoSkuDto> Skus { get; set; } = new();
}

public class DesmembramentoSkuDto
{
    public long VariacaoId { get; set; }
    public decimal Quantidade { get; set; }
}

public class DuplicataEditDto
{
    public string? Numero { get; set; }
    public string? Vencimento { get; set; }
    public decimal Descontos { get; set; }
    public decimal Encargos { get; set; }
    public decimal Valor { get; set; }
}

public class LoteEditDto
{
    public long CompraProdutoId { get; set; }
    public string? Lote { get; set; }
    public string? DataFabricacao { get; set; }
    public string? DataValidade { get; set; }
}

public class FinalizarCompraResult
{
    public int ProdutosAtualizados { get; set; }
    public int PrecosAplicados { get; set; }
    public decimal EstoqueAdicionado { get; set; }
}

public class DuplicataDto
{
    public string? Numero { get; set; }
    public string? Vencimento { get; set; }
    public decimal Descontos { get; set; }
    public decimal Encargos { get; set; }
    public decimal Valor { get; set; }
}

public class LoteItemDto
{
    public long CompraProdutoId { get; set; }
    public long? ProdutoId { get; set; }
    public string? ProdutoNome { get; set; }
    public string? CodigoBarras { get; set; }
    public string? Fabricante { get; set; }
    public decimal Quantidade { get; set; }
    /// <summary>Quantidade em UNIDADES de estoque (Quantidade * Fracao) — base do desmembramento.</summary>
    public decimal QtdeEstoque { get; set; }
    public decimal ValorTotal { get; set; }
    public string? Lote { get; set; }
    public string? DataFabricacao { get; set; }
    public string? DataValidade { get; set; }
    public string? CodigoAnvisa { get; set; }
    /// <summary>Produto de grade — permite desmembrar a quantidade da nota entre os SKUs na finalização.</summary>
    public bool ControlaGrade { get; set; }
    public List<VariacaoSimplesDto> Variacoes { get; set; } = new();
}

public class VariacaoSimplesDto
{
    public long Id { get; set; }
    public string Descricao { get; set; } = "";
}

public class DadosFinalizacaoDto
{
    public long CompraId { get; set; }
    public string NumeroNf { get; set; } = "";
    public string? SerieNf { get; set; }
    public string FornecedorNome { get; set; } = "";
    public decimal ValorNota { get; set; }
    public int TotalItens { get; set; }
    public int ItensVinculados { get; set; }
    public int ItensPrecificados { get; set; }
    public int ItensConferidos { get; set; }
    public bool TemPrecosPendentes { get; set; }
    public List<DuplicataDto> Duplicatas { get; set; } = new();
    public List<LoteItemDto> Lotes { get; set; } = new();
}
