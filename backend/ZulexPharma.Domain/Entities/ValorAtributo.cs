namespace ZulexPharma.Domain.Entities;

/// <summary>Valor possível de um atributo (ex.: P/M/G pra Tamanho; Preto/Azul pra Cor).</summary>
public class ValorAtributo : BaseEntity
{
    public long AtributoVariacaoId { get; set; }
    public AtributoVariacao AtributoVariacao { get; set; } = null!;

    public string Valor { get; set; } = string.Empty;
    /// <summary>Cor em hex (#RRGGBB) — opcional, só faz sentido pro atributo "Cor".</summary>
    public string? Hex { get; set; }
    public int Ordem { get; set; }
}
