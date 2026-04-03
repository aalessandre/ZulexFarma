namespace ZulexPharma.Application.DTOs.Fiscal;

// ── Upload da base ABCFarma ─────────────────────────────────────────
public class UploadAbcFarmaRequest
{
    public string ConteudoJson { get; set; } = string.Empty;
}

public class UploadAbcFarmaResult
{
    public int TotalRegistros { get; set; }
    public int Inseridos { get; set; }
    public int Atualizados { get; set; }
}

// ── Processar atualização ───────────────────────────────────────────
public class ProcessarAtualizacaoRequest
{
    public long FilialId { get; set; }

    /// <summary>"AUMENTAR", "REDUZIR" ou "AMBOS"</summary>
    public string Modo { get; set; } = "AMBOS";

    /// <summary>IDs dos grupos principais (vazio = todos)</summary>
    public List<long> GruposPrincipaisIds { get; set; } = new();

    public bool ReajustarPromocoes { get; set; }
    public bool ReajustarOfertas { get; set; }

    /// <summary>"AUTOMATICO" ou "LISTA"</summary>
    public string Acao { get; set; } = "LISTA";

    public string? NomeUsuario { get; set; }

    /// <summary>IDs específicos para aplicar (do preview com checkboxes)</summary>
    public List<long>? ProdutoDadosIds { get; set; }
}

// ── Resultado/Lista de preview ──────────────────────────────────────
public class AtualizacaoPrecoPreviewItem
{
    public long ProdutoId { get; set; }
    public long ProdutoDadosId { get; set; }
    public string ProdutoNome { get; set; } = "";
    public string? Ean { get; set; }
    public string? GrupoPrincipalNome { get; set; }
    public decimal ValorVendaAtual { get; set; }
    public decimal ValorVendaNovo { get; set; }
    public decimal PmcAtual { get; set; }
    public decimal PmcNovo { get; set; }
    public decimal VariacaoPercent { get; set; }
}

public class ProcessarAtualizacaoResult
{
    public long? AtualizacaoPrecoId { get; set; }
    public int TotalProdutos { get; set; }
    public int TotalAlterados { get; set; }
    public List<AtualizacaoPrecoPreviewItem> Itens { get; set; } = new();
}

// ── Histórico ───────────────────────────────────────────────────────
public class AtualizacaoPrecoListDto
{
    public long Id { get; set; }
    public string Tipo { get; set; } = "";
    public DateTime DataExecucao { get; set; }
    public string? NomeUsuario { get; set; }
    public int TotalProdutos { get; set; }
    public int TotalAlterados { get; set; }
    public string Status { get; set; } = "";
}

// ── Info da base ────────────────────────────────────────────────────
public class AbcFarmaBaseInfo
{
    public int TotalRegistros { get; set; }
    public DateTime? UltimaAtualizacao { get; set; }
}
