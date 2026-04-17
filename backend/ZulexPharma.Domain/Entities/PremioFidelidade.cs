namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Catálogo de prêmios resgatáveis via pontos.
/// Um cliente com saldo suficiente pode trocar pontos por um prêmio (ex: "DVD 500 pts").
/// Fase futura: expor o catálogo num portal web pro cliente consultar.
/// </summary>
public class PremioFidelidade : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    /// <summary>Categoria livre (ex: "Eletrônicos", "Perfumaria"). Opcional — texto plano por enquanto.</summary>
    public string? Categoria { get; set; }

    public int PontosNecessarios { get; set; }

    /// <summary>URL/caminho da imagem pro portal web (opcional).</summary>
    public string? ImagemUrl { get; set; }

    /// <summary>Estoque do prêmio (null = sem controle de estoque).</summary>
    public int? Estoque { get; set; }
}
