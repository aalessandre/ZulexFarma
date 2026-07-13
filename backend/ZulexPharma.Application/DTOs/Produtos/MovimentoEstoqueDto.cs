namespace ZulexPharma.Application.DTOs.Produtos;

public class MovimentoEstoqueDto
{
    public long Id { get; set; }
    public DateTime Data { get; set; }
    /// <summary>Rotulo do tipo (Compra, Venda, Perda, Ajuste...).</summary>
    public string Tipo { get; set; } = "";
    /// <summary>Entrada ou Saida (derivado do sinal do delta).</summary>
    public string Sentido { get; set; } = "";
    /// <summary>Magnitude (sempre positiva) do movimento.</summary>
    public decimal Quantidade { get; set; }
    public decimal SaldoApos { get; set; }
    public string? Documento { get; set; }
    public string? PessoaNome { get; set; }
    public string? UsuarioNome { get; set; }
    /// <summary>Descricao do SKU (grade), quando o movimento e' de variacao.</summary>
    public string? Variacao { get; set; }
    public string? Observacao { get; set; }
    public long FilialId { get; set; }
}
