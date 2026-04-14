namespace ZulexPharma.Application.DTOs.Compras;

/// <summary>
/// Item mostrado na tela "Conferir Lotes" de uma compra.
/// Agrupa os dados do produto + lotes que vieram do XML (ou vazio se não tinha rastro).
/// </summary>
public class ConferenciaLotesItemDto
{
    public long CompraProdutoId { get; set; }
    public long? ProdutoId { get; set; }
    public string Codigo { get; set; } = string.Empty;          // CodigoProdutoFornecedor do XML
    public string Descricao { get; set; } = string.Empty;       // DescricaoXml ou Produto.Nome
    public string? Fabricante { get; set; }
    public decimal Quantidade { get; set; }
    public string? UnidadeXml { get; set; }

    /// <summary>True se o produto é controlado SNGPC (Psicotrópicos/Antimicrobiano).</summary>
    public bool IsSngpc { get; set; }

    /// <summary>True se o produto exige rastreio de lote (SNGPC OU grupo com ControlarLotesVencimento).</summary>
    public bool IsRastreavel { get; set; }

    /// <summary>Registro MS atual do Produto (pode ser editado e gera snapshot por lote).</summary>
    public string? RegistroMs { get; set; }

    /// <summary>Lista de lotes deste item (podem ser 0, 1 ou N).</summary>
    public List<ConferenciaLoteLinhaDto> Lotes { get; set; } = new();
}

/// <summary>Um lote (linha editável) dentro de um item da conferência.</summary>
public class ConferenciaLoteLinhaDto
{
    public long? Id { get; set; }                      // null = lote novo criado na conferência
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime? DataFabricacao { get; set; }
    public DateTime? DataValidade { get; set; }
    public decimal Quantidade { get; set; }
    public string? RegistroMs { get; set; }

    // Snapshots originais (read-only no frontend — só pra auditoria visual)
    public string? NumeroLoteOriginal { get; set; }
    public DateTime? DataFabricacaoOriginal { get; set; }
    public DateTime? DataValidadeOriginal { get; set; }
    public string? RegistroMsOriginal { get; set; }

    public bool EditadoPeloUsuario { get; set; }
    public DateTime? EditadoEm { get; set; }
    public string? EditadoPorUsuarioNome { get; set; }
}

/// <summary>Retorno da tela de conferência — toda a compra com cabeçalho + itens.</summary>
public class ConferenciaLotesDto
{
    public long CompraId { get; set; }
    public string NumeroNf { get; set; } = string.Empty;
    public string FornecedorNome { get; set; } = string.Empty;
    public DateTime? DataEmissao { get; set; }
    public bool LotesConferidos { get; set; }
    public DateTime? LotesConferidosEm { get; set; }
    public string? LotesConferidosPorUsuarioNome { get; set; }
    public bool? SngpcOptOut { get; set; }
    public List<ConferenciaLotesItemDto> Itens { get; set; } = new();
}

/// <summary>
/// Body enviado pelo frontend ao salvar a conferência.
/// Contém a lista editada de lotes por item e o opt-out global de SNGPC (para modo Misto).
/// </summary>
public class SalvarConferenciaLotesDto
{
    public bool? SngpcOptOut { get; set; }
    public List<SalvarConferenciaLotesItemDto> Itens { get; set; } = new();
}

public class SalvarConferenciaLotesItemDto
{
    public long CompraProdutoId { get; set; }
    public string? RegistroMs { get; set; }
    public List<SalvarConferenciaLoteLinhaDto> Lotes { get; set; } = new();
}

public class SalvarConferenciaLoteLinhaDto
{
    public long? Id { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime? DataFabricacao { get; set; }
    public DateTime? DataValidade { get; set; }
    public decimal Quantidade { get; set; }
    public string? RegistroMs { get; set; }
}
