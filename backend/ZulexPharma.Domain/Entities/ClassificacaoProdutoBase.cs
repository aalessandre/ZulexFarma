namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Base para classificações de produto (Grupo Principal, Grupo, Sub Grupo, Seção).
/// Todos compartilham os mesmos campos.
/// </summary>
public abstract class ClassificacaoProdutoBase : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public decimal ComissaoPercentual { get; set; } = 0;
    public decimal DescontoMinimo { get; set; } = 0;
    public decimal DescontoMaximo { get; set; } = 0;
    public decimal DescontoMaximoComSenha { get; set; } = 0;
    public decimal ProjecaoLucro { get; set; } = 30;
    public decimal MarkupPadrao { get; set; } = 50;

    /// <summary>Vazio, "MARKUP" ou "PROJECAO"</summary>
    public string? Priorizar { get; set; }

    public bool ControlarLotesVencimento { get; set; } = false;
    public bool InformarPrescritorVenda { get; set; } = false;
    public bool ImprimirEtiqueta { get; set; } = false;
    public bool PermitirDescontoPrazo { get; set; } = false;
    public bool PermitirPromocao { get; set; } = false;
    public bool PermitirDescontosProgressivos { get; set; } = false;
}
