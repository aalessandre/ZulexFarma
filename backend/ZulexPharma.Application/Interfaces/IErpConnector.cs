using ZulexPharma.Application.DTOs.SelfCheckout;

namespace ZulexPharma.Application.Interfaces;

/// <summary>
/// Conector com o ERP origem usado pelo módulo Self-Checkout.
/// Cada ERP suportado (Inovafarma, etc.) implementa esta interface.
/// Instância é criada via <see cref="IErpConnectorFactory"/> com as credenciais
/// da filial atual.
/// </summary>
public interface IErpConnector : IAsyncDisposable
{
    Task<ResultadoTesteConexaoDto> TestarConexaoAsync(CancellationToken ct = default);

    /// <summary>
    /// Busca produto pelo EAN (denormalizado em <c>Produto.CodigoBarra</c> ou
    /// na tabela <c>Produto_CodigoBarra</c>). Retorna null se não localizado.
    /// Já preenche <see cref="ProdutoSelfCheckoutDto.PrecoFinal"/> aplicando RN-19.
    /// </summary>
    Task<ProdutoSelfCheckoutDto?> BuscarProdutoPorEanAsync(string ean, CancellationToken ct = default);

    /// <summary>
    /// Busca produto pelo código externo (CodigoProduto do Inovafarma). Usado na finalização
    /// da venda quando o kiosk já bipou e tem o código retornado pela busca EAN/nome,
    /// e precisa revalidar preço/estoque atual no servidor antes de gravar.
    /// </summary>
    Task<ProdutoSelfCheckoutDto?> BuscarProdutoPorCodigoAsync(string codigoExterno, CancellationToken ct = default);

    /// <summary>
    /// Busca por nome (LIKE %termo%). Retorna até <paramref name="top"/> resultados,
    /// cada um com preço final calculado conforme RN-19.
    /// </summary>
    Task<List<ProdutoSelfCheckoutDto>> BuscarProdutosPorNomeAsync(string termo, int top = 20, CancellationToken ct = default);

    /// <summary>
    /// Resolve o snapshot fiscal completo do produto para emissão de NFC-e.
    /// Combina a hierarquia de alíquotas (Produto_Fiscal_UF → Produto_NCM_UF →
    /// Fiscal_Aliquota) com a Natureza de Operação configurada (Fiscal_Natureza
    /// + Fiscal_Natureza_CSTCSOSN_ICMS + Fiscal_PISCofins). Cenário escolhido:
    /// Tributado PF Dentro do Estado (consumidor final na própria UF, padrão NFC-e).
    /// CSOSN é usado para Simples Nacional (regime 1 ou 2); CST para Regime Normal
    /// (regime 3 Lucro Presumido / 4 Lucro Real). PIS/COFINS Cumulativo para regime 3,
    /// Não-Cumulativo para regime 4, zerado para Simples.
    /// </summary>
    /// <param name="codigoExterno">Código do produto no ERP origem.</param>
    /// <param name="uf">UF da filial emitente.</param>
    /// <param name="codigoRegime">Regime tributário (1=Simples, 2=Simples Excesso, 3=Lucro Presumido, 4=Lucro Real).</param>
    Task<ProdutoFiscalSnapshotDto?> ObterFiscalAsync(string codigoExterno, string uf, int codigoRegime, CancellationToken ct = default);

    /// <summary>
    /// Lista as naturezas de operação de saída cadastradas no ERP origem.
    /// Usado para popular o dropdown de configuração no accordion Self-Checkout.
    /// </summary>
    Task<List<NaturezaOperacaoErpDto>> ListarNaturezasOperacaoSaidaAsync(CancellationToken ct = default);
}
