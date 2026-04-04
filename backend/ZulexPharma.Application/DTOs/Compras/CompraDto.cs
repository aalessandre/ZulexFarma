using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.Compras;

// ── Grid ─────────────────────────────────────────────────────────────
public class CompraListDto
{
    public long Id { get; set; }
    public string? Codigo { get; set; }
    public string NumeroNf { get; set; } = string.Empty;
    public string? SerieNf { get; set; }
    public string FornecedorNome { get; set; } = string.Empty;
    public string? FornecedorCnpj { get; set; }
    public DateTime? DataEmissao { get; set; }
    public DateTime? DataEntrada { get; set; }
    public decimal ValorNota { get; set; }
    public CompraStatus Status { get; set; }
    public int TotalItens { get; set; }
    public int ItensVinculados { get; set; }
    public int ItensPrecificados { get; set; }
    public DateTime CriadoEm { get; set; }
}

// ── Detalhe (retorno do GET /{id}) ───────────────────────────────────
public class CompraDetalheDto
{
    public long Id { get; set; }
    public string? Codigo { get; set; }
    public long FilialId { get; set; }
    public long FornecedorId { get; set; }
    public string FornecedorNome { get; set; } = string.Empty;
    public string? FornecedorCnpj { get; set; }
    public string ChaveNfe { get; set; } = string.Empty;
    public string NumeroNf { get; set; } = string.Empty;
    public string? SerieNf { get; set; }
    public string? NaturezaOperacao { get; set; }
    public DateTime? DataEmissao { get; set; }
    public DateTime? DataEntrada { get; set; }
    public decimal ValorProdutos { get; set; }
    public decimal ValorSt { get; set; }
    public decimal ValorFcpSt { get; set; }
    public decimal ValorFrete { get; set; }
    public decimal ValorSeguro { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorIpi { get; set; }
    public decimal ValorPis { get; set; }
    public decimal ValorCofins { get; set; }
    public decimal ValorOutros { get; set; }
    public decimal ValorNota { get; set; }
    public CompraStatus Status { get; set; }
    public DateTime CriadoEm { get; set; }

    public List<CompraProdutoDto> Produtos { get; set; } = new();
}

// ── Item da compra ───────────────────────────────────────────────────
public class CompraProdutoDto
{
    public long Id { get; set; }
    public int NumeroItem { get; set; }
    public long? ProdutoId { get; set; }
    public string? ProdutoNome { get; set; }
    public string? ProdutoCodigoBarras { get; set; }
    public string? CodigoProdutoFornecedor { get; set; }
    public string? CodigoBarrasXml { get; set; }
    public string? DescricaoXml { get; set; }
    public string? NcmXml { get; set; }
    public string? CestXml { get; set; }
    public string? CfopXml { get; set; }
    public string? UnidadeXml { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorFrete { get; set; }
    public decimal ValorOutros { get; set; }
    public decimal ValorItemNota { get; set; }
    public string? Lote { get; set; }
    public DateTime? DataFabricacao { get; set; }
    public DateTime? DataValidade { get; set; }
    public string? CodigoAnvisa { get; set; }
    public decimal? PrecoMaximoConsumidor { get; set; }
    public bool Vinculado { get; set; }
    public short Fracao { get; set; } = 1;
    public string? InfoAdicional { get; set; }

    public CompraFiscalDto? Fiscal { get; set; }
}

// ── Dados fiscais do item ────────────────────────────────────────────
public class CompraFiscalDto
{
    public string? OrigemMercadoria { get; set; }
    public string? CstIcms { get; set; }
    public decimal BaseIcms { get; set; }
    public decimal AliquotaIcms { get; set; }
    public decimal ValorIcms { get; set; }
    public string? ModalidadeBcSt { get; set; }
    public decimal MvaSt { get; set; }
    public decimal BaseSt { get; set; }
    public decimal AliquotaSt { get; set; }
    public decimal ValorSt { get; set; }
    public decimal BaseFcpSt { get; set; }
    public decimal AliquotaFcpSt { get; set; }
    public decimal ValorFcpSt { get; set; }
    public string? CstPis { get; set; }
    public decimal BasePis { get; set; }
    public decimal AliquotaPis { get; set; }
    public decimal ValorPis { get; set; }
    public string? CstCofins { get; set; }
    public decimal BaseCofins { get; set; }
    public decimal AliquotaCofins { get; set; }
    public decimal ValorCofins { get; set; }
    public string? CstIbsCbs { get; set; }
    public string? ClasseTributariaIbsCbs { get; set; }
    public decimal BaseIbsCbs { get; set; }
    public decimal AliquotaIbsUf { get; set; }
    public decimal ValorIbsUf { get; set; }
    public decimal AliquotaIbsMun { get; set; }
    public decimal ValorIbsMun { get; set; }
    public decimal AliquotaCbs { get; set; }
    public decimal ValorCbs { get; set; }
}

// ── DTO para atualizar fração ────────────────────────────────────────
public class AtualizarFracaoRequest
{
    public short Fracao { get; set; } = 1;
}

// ── DTO para vincular produto ────────────────────────────────────────
public class VincularProdutoDto
{
    public long CompraProdutoId { get; set; }
    public long ProdutoId { get; set; }
}
