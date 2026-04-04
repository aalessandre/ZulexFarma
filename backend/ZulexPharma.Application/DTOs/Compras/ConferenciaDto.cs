namespace ZulexPharma.Application.DTOs.Compras;

public class BiparRequest
{
    public string Barras { get; set; } = "";
    public decimal Quantidade { get; set; } = 1;
    public List<long> CompraIds { get; set; } = new();
}

public class BiparResult
{
    public bool Encontrado { get; set; }
    public bool PertenceNota { get; set; }
    public string? Mensagem { get; set; }
    public long? CompraProdutoId { get; set; }
    public long? ProdutoId { get; set; }
    public string? ProdutoNome { get; set; }
    public decimal QtdeConferida { get; set; }
    public decimal QtdeTotal { get; set; }
}

public class AtualizarQtdeConfRequest
{
    public decimal QtdeConferida { get; set; }
}
