namespace ZulexPharma.Application.DTOs.Produtos;

// ── Lista ───────────────────────────────────────────────────────────
public class ProdutoListDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = "";
    public string? CodigoBarras { get; set; }
    public string? FabricanteNome { get; set; }
    public string? GrupoPrincipalNome { get; set; }
    public bool Ativo { get; set; }
    public bool Eliminado { get; set; }
    public DateTime CriadoEm { get; set; }
}

// ── Detalhe (GET por id) ────────────────────────────────────────────
public class ProdutoDetalheDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = "";
    public string? CodigoBarras { get; set; }
    public int QtdeEmbalagem { get; set; }
    public decimal? PrecoFp { get; set; }
    public string Lista { get; set; } = "Indefinida";
    public short Fracao { get; set; }
    public bool Ativo { get; set; }
    public bool Eliminado { get; set; }
    public DateTime CriadoEm { get; set; }

    // FKs
    public long? FabricanteId { get; set; }
    public long? GrupoPrincipalId { get; set; }
    public long? GrupoProdutoId { get; set; }
    public long? SubGrupoId { get; set; }
    public long? NcmId { get; set; }

    // Nomes para exibição
    public string? FabricanteNome { get; set; }
    public string? GrupoPrincipalNome { get; set; }
    public string? GrupoProdutoNome { get; set; }
    public string? SubGrupoNome { get; set; }
    public string? NcmCodigo { get; set; }

    // Sub-tabelas
    public List<ProdutoBarrasDto> Barras { get; set; } = new();
    public List<ProdutoMsDto> RegistrosMs { get; set; } = new();
    public List<ProdutoSubstanciaDto> Substancias { get; set; } = new();
    public List<ProdutoFornecedorDto> Fornecedores { get; set; } = new();
    public ProdutoFiscalDto? Fiscal { get; set; }
    public List<ProdutoDadosDto> Dados { get; set; } = new();
}

// ── Form (POST/PUT) ────────────────────────────────────────────────
public class ProdutoFormDto
{
    public string Nome { get; set; } = "";
    public string? CodigoBarras { get; set; }
    public int QtdeEmbalagem { get; set; } = 1;
    public decimal? PrecoFp { get; set; }
    public string Lista { get; set; } = "Indefinida";
    public short Fracao { get; set; } = 1;
    public bool Ativo { get; set; } = true;
    public bool Eliminado { get; set; }

    public long? FabricanteId { get; set; }
    public long? GrupoPrincipalId { get; set; }
    public long? GrupoProdutoId { get; set; }
    public long? SubGrupoId { get; set; }
    public long? NcmId { get; set; }

    public List<ProdutoBarrasDto> Barras { get; set; } = new();
    public List<ProdutoMsDto> RegistrosMs { get; set; } = new();
    public List<ProdutoSubstanciaDto> Substancias { get; set; } = new();
    public List<ProdutoFornecedorDto> Fornecedores { get; set; } = new();
    public ProdutoFiscalDto? Fiscal { get; set; }
    public List<ProdutoDadosDto> Dados { get; set; } = new();
}

// ── Sub-tabelas ─────────────────────────────────────────────────────

public class ProdutoBarrasDto
{
    public long? Id { get; set; }
    public string Barras { get; set; } = "";
}

public class ProdutoMsDto
{
    public long? Id { get; set; }
    public string NumeroMs { get; set; } = "";
}

public class ProdutoSubstanciaDto
{
    public long? Id { get; set; }
    public long SubstanciaId { get; set; }
    public string? SubstanciaNome { get; set; }
}

public class ProdutoFornecedorDto
{
    public long? Id { get; set; }
    public long FornecedorId { get; set; }
    public string? FornecedorNome { get; set; }
    public string? CodigoProdutoFornecedor { get; set; }
    public string? NomeProduto { get; set; }
    public short Fracao { get; set; } = 1;
}

public class ProdutoFiscalDto
{
    public long? Id { get; set; }
    public long? NcmId { get; set; }
    public string? NcmCodigo { get; set; }
    public string? Cest { get; set; }
    public string? OrigemMercadoria { get; set; }
    public string? CstIcms { get; set; }
    public string? Csosn { get; set; }
    public decimal AliquotaIcms { get; set; }
    public string? CstPis { get; set; }
    public decimal AliquotaPis { get; set; }
    public string? CstCofins { get; set; }
    public decimal AliquotaCofins { get; set; }
    public string? CstIpi { get; set; }
    public decimal AliquotaIpi { get; set; }
}

public class ProdutoDadosDto
{
    public long? Id { get; set; }
    public long FilialId { get; set; }
    public string? FilialNome { get; set; }

    // Estoque
    public decimal EstoqueAtual { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public decimal EstoqueMaximo { get; set; }
    public decimal Demanda { get; set; }
    public string? CurvaAbc { get; set; }

    // Preços — Última Compra
    public decimal UltimaCompraUnitario { get; set; }
    public decimal UltimaCompraSt { get; set; }
    public decimal UltimaCompraOutros { get; set; }
    public decimal UltimaCompraIpi { get; set; }
    public decimal UltimaCompraFpc { get; set; }
    public decimal UltimaCompraBoleto { get; set; }
    public decimal UltimaCompraDifal { get; set; }

    // Preços — Valores
    public decimal CustoMedio { get; set; }
    public decimal ProjecaoLucro { get; set; }
    public decimal Markup { get; set; }
    public decimal ValorVenda { get; set; }

    // Promoção
    public decimal ValorPromocao { get; set; }
    public DateTime? PromocaoInicio { get; set; }
    public DateTime? PromocaoFim { get; set; }

    // Descontos
    public decimal DescontoMinimo { get; set; }
    public decimal DescontoMaxSemSenha { get; set; }
    public decimal DescontoMaxComSenha { get; set; }

    // Geral
    public decimal Comissao { get; set; }
    public decimal ValorIncentivo { get; set; }
    public long? ProdutoLocalId { get; set; }
    public long? SecaoId { get; set; }
    public long? ProdutoFamiliaId { get; set; }
    public string? NomeEtiqueta { get; set; }
    public string? Mensagem { get; set; }

    // Flags
    public bool BloquearDesconto { get; set; }
    public bool BloquearPromocao { get; set; }
    public bool NaoAtualizarAbcfarma { get; set; }
    public bool NaoAtualizarGestorTributario { get; set; }
    public bool BloquearCompras { get; set; }
    public bool ProdutoFormula { get; set; }
    public bool BloquearComissao { get; set; }
    public bool BloquearCoberturaOferta { get; set; }
    public bool UsoContinuo { get; set; }
    public bool AvisoFracao { get; set; }

    // Estatísticas
    public DateTime? UltimaCompraEm { get; set; }
    public DateTime? UltimaVendaEm { get; set; }
}

// ── ProdutoLocal (cadastral) ────────────────────────────────────────
public class ProdutoLocalListDto
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public string Nome { get; set; } = "";
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class ProdutoLocalFormDto
{
    public long FilialId { get; set; }
    public string Nome { get; set; } = "";
    public bool Ativo { get; set; } = true;
}
