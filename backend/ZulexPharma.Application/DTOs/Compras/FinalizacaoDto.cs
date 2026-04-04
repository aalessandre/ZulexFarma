namespace ZulexPharma.Application.DTOs.Compras;

public class FinalizarCompraRequest
{
    public long CompraId { get; set; }
    public bool DuplicatasEntregues { get; set; }
    public bool NotaPaga { get; set; }
    public string? NomeUsuario { get; set; }
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
    public decimal Valor { get; set; }
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
}
