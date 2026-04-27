using Dapper;
using Microsoft.Data.SqlClient;
using Serilog;
using ZulexPharma.Application.DTOs.SelfCheckout;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.Infrastructure.Services.SelfCheckout;

/// <summary>
/// Conector com o banco SQL Server do Inovafarma. Implementa busca por EAN/nome
/// e cálculo de preço final aplicando RN-19/RN-21 da spec.
/// </summary>
public class InovafarmaConnector : IErpConnector
{
    private readonly string _connectionString;
    private readonly short _codigoEmpresa;
    private readonly int? _codigoNaturezaOperacaoNfce;
    private SqlConnection? _connection;

    public InovafarmaConnector(string connectionString, short codigoEmpresa, int? codigoNaturezaOperacaoNfce = null)
    {
        _connectionString = connectionString;
        _codigoEmpresa = codigoEmpresa;
        _codigoNaturezaOperacaoNfce = codigoNaturezaOperacaoNfce;
    }

    private async Task<SqlConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection == null)
        {
            _connection = new SqlConnection(_connectionString);
            await _connection.OpenAsync(ct);
        }
        return _connection;
    }

    public async Task<ResultadoTesteConexaoDto> TestarConexaoAsync(CancellationToken ct = default)
    {
        try
        {
            var conn = await GetConnectionAsync(ct);
            var total = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(*) FROM Produto_Estoque WHERE CodigoEmpresa = @emp AND Ativo = 1",
                new { emp = _codigoEmpresa }, cancellationToken: ct));

            return new ResultadoTesteConexaoDto
            {
                Ok = true,
                Mensagem = $"Conexão OK. Filial {_codigoEmpresa} encontrada.",
                TotalProdutos = total
            };
        }
        catch (SqlException ex)
        {
            Log.Warning(ex, "Falha ao conectar no Inovafarma (host/credencial/banco).");
            return new ResultadoTesteConexaoDto { Ok = false, Mensagem = $"Erro SQL: {ex.Message}" };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Erro inesperado ao testar conexão Inovafarma.");
            return new ResultadoTesteConexaoDto { Ok = false, Mensagem = $"Erro: {ex.Message}" };
        }
    }

    public async Task<ProdutoSelfCheckoutDto?> BuscarProdutoPorEanAsync(string ean, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ean)) return null;
        var conn = await GetConnectionAsync(ct);

        // Localiza produto pelo EAN principal (Produto.CodigoBarra) OU adicional (Produto_CodigoBarra).
        var produto = await conn.QueryFirstOrDefaultAsync<ProdutoBasico>(new CommandDefinition(@"
            SELECT TOP 1
                p.CodigoProduto AS CodigoExterno,
                p.CodigoBarra   AS CodigoBarras,
                p.NomeProduto   AS Nome,
                p.NCM           AS Ncm,
                p.UnidadeVenda  AS Unidade
            FROM Produto p
            WHERE p.Eliminado = 0
              AND (p.CodigoBarra = @ean
                   OR EXISTS (SELECT 1 FROM Produto_CodigoBarra pcb
                              WHERE pcb.CodigoProduto = p.CodigoProduto AND pcb.CodigoBarra = @ean))",
            new { ean }, cancellationToken: ct));

        if (produto == null) return null;

        return await CompletarComPrecoAsync(produto, ct);
    }

    public async Task<ProdutoSelfCheckoutDto?> BuscarProdutoPorCodigoAsync(string codigoExterno, CancellationToken ct = default)
    {
        if (!int.TryParse(codigoExterno, out var codigoProduto)) return null;
        var conn = await GetConnectionAsync(ct);

        var produto = await conn.QueryFirstOrDefaultAsync<ProdutoBasico>(new CommandDefinition(@"
            SELECT TOP 1
                p.CodigoProduto AS CodigoExterno,
                p.CodigoBarra   AS CodigoBarras,
                p.NomeProduto   AS Nome,
                p.NCM           AS Ncm,
                p.UnidadeVenda  AS Unidade
            FROM Produto p
            WHERE p.CodigoProduto = @produto
              AND p.Eliminado = 0",
            new { produto = codigoProduto }, cancellationToken: ct));

        if (produto == null) return null;
        return await CompletarComPrecoAsync(produto, ct);
    }

    public async Task<List<ProdutoSelfCheckoutDto>> BuscarProdutosPorNomeAsync(string termo, int top = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(termo) || top <= 0) return new List<ProdutoSelfCheckoutDto>();

        var conn = await GetConnectionAsync(ct);
        var like = $"%{termo.Trim()}%";

        // Junta produto + estoque numa só query, já filtrando por filial e produtos ativos.
        // Promoções aplicáveis são calculadas em segunda passada (mais legível e
        // permite reuso do mesmo método CompletarComPrecoAsync).
        var produtos = (await conn.QueryAsync<ProdutoBasico>(new CommandDefinition(@"
            SELECT TOP (@top)
                p.CodigoProduto AS CodigoExterno,
                p.CodigoBarra   AS CodigoBarras,
                p.NomeProduto   AS Nome,
                p.NCM           AS Ncm,
                p.UnidadeVenda  AS Unidade
            FROM Produto p
            INNER JOIN Produto_Estoque pe
                ON pe.CodigoProduto = p.CodigoProduto
               AND pe.CodigoEmpresa = @emp
            WHERE p.Eliminado = 0
              AND pe.Ativo = 1
              AND p.NomeProduto LIKE @like
            ORDER BY p.NomeProduto",
            new { top, emp = _codigoEmpresa, like }, cancellationToken: ct)))
            .ToList();

        var resultado = new List<ProdutoSelfCheckoutDto>(produtos.Count);
        foreach (var p in produtos)
        {
            var dto = await CompletarComPrecoAsync(p, ct);
            if (dto != null) resultado.Add(dto);
        }
        return resultado;
    }

    /// <summary>
    /// Completa o produto com preço cheio + preço final (RN-19) e estoque atual.
    /// Implementa a CTE da Seção 11 da spec self-checkout.
    /// Flags de plano de pagamento são ignoradas no MVP (RN-21).
    /// </summary>
    private async Task<ProdutoSelfCheckoutDto?> CompletarComPrecoAsync(ProdutoBasico p, CancellationToken ct)
    {
        var conn = await GetConnectionAsync(ct);

        var preco = await conn.QueryFirstOrDefaultAsync<PrecoEstoque>(new CommandDefinition(@"
            ;WITH PrecoCheio AS (
                SELECT pe.PrecoVenda AS Preco
                FROM Produto_Estoque pe
                WHERE pe.CodigoProduto = @produto
                  AND pe.CodigoEmpresa = @emp
            ),
            PromoSimples AS (
                SELECT pe.PrecoPromocao AS Preco
                FROM Produto_Estoque pe
                WHERE pe.CodigoProduto = @produto
                  AND pe.CodigoEmpresa = @emp
                  AND pe.PrecoPromocao > 0
                  AND (pe.DataPromocaoInicio IS NULL OR pe.DataPromocaoInicio <= @hoje)
                  AND (pe.DataPromocaoFim    IS NULL OR pe.DataPromocaoFim    >= @hoje)
            ),
            PromoElaborada AS (
                SELECT pp.PrecoPromocao AS Preco
                FROM Promocao_Produto pp
                INNER JOIN Promocao p
                    ON p.CodigoPromocao = pp.CodigoPromocao
                INNER JOIN Promocao_Empresa pe
                    ON pe.CodigoPromocao = p.CodigoPromocao
                   AND pe.CodigoEmpresa  = @emp
                WHERE pp.CodigoProduto = @produto
                  AND p.Ativo = 1
                  AND p.DataInicio <= @hoje
                  AND (p.DataFim IS NULL OR p.DataFim >= @hoje)
                  AND p.ExclusivoConvenio  = 0
                  AND p.ExclusivoEcommerce = 0
                  AND (
                        NOT EXISTS (SELECT 1 FROM Promocao_DiaSemana ds
                                    WHERE ds.CodigoPromocao = p.CodigoPromocao)
                     OR EXISTS    (SELECT 1 FROM Promocao_DiaSemana ds
                                    WHERE ds.CodigoPromocao = p.CodigoPromocao
                                      AND ds.Dia = @dia)
                      )
            )
            SELECT
                (SELECT TOP 1 Preco FROM PrecoCheio)                  AS PrecoCheio,
                (SELECT MIN(Preco) FROM (
                    SELECT Preco FROM PrecoCheio
                    UNION ALL SELECT Preco FROM PromoSimples
                    UNION ALL SELECT Preco FROM PromoElaborada
                ) X)                                                   AS PrecoFinal,
                (SELECT TOP 1 Estoque FROM Produto_Estoque
                 WHERE CodigoProduto = @produto AND CodigoEmpresa = @emp) AS EstoqueAtual",
            new
            {
                produto = int.Parse(p.CodigoExterno),
                emp = _codigoEmpresa,
                hoje = DateTime.Now,
                dia = (byte)DateTime.Now.DayOfWeek + 1   // SQL: 1=Dom..7=Sáb
            }, cancellationToken: ct));

        if (preco == null || preco.PrecoCheio == null)
            return null; // produto não cadastrado nessa filial

        return new ProdutoSelfCheckoutDto
        {
            CodigoExterno = p.CodigoExterno,
            CodigoBarras = p.CodigoBarras,
            Nome = p.Nome,
            Ncm = p.Ncm,
            Unidade = p.Unidade,
            PrecoCheio = preco.PrecoCheio.Value,
            PrecoFinal = preco.PrecoFinal ?? preco.PrecoCheio.Value,
            EstoqueAtual = preco.EstoqueAtual
        };
    }

    public async Task<ProdutoFiscalSnapshotDto?> ObterFiscalAsync(string codigoExterno, string uf, int codigoRegime, CancellationToken ct = default)
    {
        if (!int.TryParse(codigoExterno, out var codigoProduto)) return null;

        var conn = await GetConnectionAsync(ct);

        // ── 1) Produto + hierarquia de alíquotas ICMS (Produto_Fiscal_UF > Produto_NCM_UF) + dados do NCM ──
        var bruto = await conn.QueryFirstOrDefaultAsync<FiscalRaw>(new CommandDefinition(@"
            SELECT
                p.NCM                       AS Ncm,
                p.CEST                      AS Cest,
                p.Origem                    AS Origem,
                p.PisCofinsCST              AS PisCofinsCstProduto,
                p.UnidadeVenda              AS Unidade,

                pfu.CodigoTributo           AS PfuCodigoTributo,
                pfu.ICMS                    AS PfuIcms,
                pfu.FCP                     AS PfuFcp,
                pfu.ReducaoICMS             AS PfuReducao,
                pfu.IcmsImportado           AS PfuIcmsImportado,
                pfu.CodigoBeneficio         AS PfuCodigoBeneficio,

                pnu.CodigoTributo           AS PnuCodigoTributo,
                pnu.ICMS                    AS PnuIcms,
                pnu.ReducaoICMS             AS PnuReducao,
                pnu.IcmsImportado           AS PnuIcmsImportado,

                pn.PisCofinsCST             AS PisCofinsCstNcm,
                pn.Pis                      AS AliquotaPis,
                pn.Cofins                   AS AliquotaCofins,
                pn.CEST                     AS CestNcm
            FROM Produto p
            LEFT JOIN Produto_Fiscal_UF pfu
                ON pfu.CodigoProduto = p.CodigoProduto
               AND pfu.UF = @uf
               AND pfu.CodigoRegime = @regime
            LEFT JOIN Produto_NCM_UF pnu
                ON pnu.NCM = p.NCM
               AND pnu.UF  = @uf
            LEFT JOIN Produto_NCM pn
                ON pn.NCM = p.NCM
            WHERE p.CodigoProduto = @produto
              AND p.Eliminado = 0",
            new { produto = codigoProduto, uf, regime = codigoRegime }, cancellationToken: ct));

        if (bruto == null) return null;

        // Hierarquia ICMS: Fiscal_UF > NCM_UF > Fiscal_Aliquota (fallback genérico) > 0
        decimal aliquotaIcms;
        decimal aliquotaFcp;
        decimal reducao;
        byte? codigoTributoFallback;
        string? codigoBeneficio = null;

        if (bruto.PfuCodigoTributo.HasValue)
        {
            codigoTributoFallback = bruto.PfuCodigoTributo;
            aliquotaIcms = bruto.PfuIcms ?? 0;
            aliquotaFcp = bruto.PfuFcp ?? 0;
            reducao = bruto.PfuReducao ?? 0;
            codigoBeneficio = bruto.PfuCodigoBeneficio;
        }
        else if (bruto.PnuCodigoTributo.HasValue)
        {
            codigoTributoFallback = bruto.PnuCodigoTributo;
            aliquotaIcms = bruto.PnuIcms ?? 0;
            aliquotaFcp = 0;
            reducao = bruto.PnuReducao ?? 0;
        }
        else
        {
            codigoTributoFallback = null;
            aliquotaIcms = 0;
            aliquotaFcp = 0;
            reducao = 0;
        }

        // Fallback genérico em Fiscal_Aliquota (UFOrigem = UFDestino = UF da filial)
        if (aliquotaIcms == 0)
        {
            var aliquotaFallback = await conn.ExecuteScalarAsync<decimal?>(new CommandDefinition(@"
                SELECT TOP 1 Aliquota FROM Fiscal_Aliquota
                WHERE UFOrigem = @uf AND UFDestino = @uf
                ORDER BY CodigoAliquota",
                new { uf }, cancellationToken: ct));
            if (aliquotaFallback is > 0) aliquotaIcms = aliquotaFallback.Value;
        }

        // ── 2) Natureza de Operação: cenários CST/CSOSN ICMS + CFOPs por cenário + CST PIS/COFINS ──
        NaturezaFiscalRaw? natureza = null;
        if (_codigoNaturezaOperacaoNfce.HasValue)
        {
            natureza = await conn.QueryFirstOrDefaultAsync<NaturezaFiscalRaw>(new CommandDefinition(@"
                SELECT
                    fn.CodigoCST              AS CstPisCofinsNatureza,
                    fn.CFOPTribPFDE           AS CfopTribDe,
                    fn.CFOPSubstituicaoDE     AS CfopStDe,
                    fn.CFOPECFDE              AS CfopEcfDe,
                    fn.ReducaoICMS            AS ReducaoNatureza,
                    fnc.CSTTributadoSemInscricaoEstadualDE AS CstTribSemIeDe,
                    fnc.CSTSubstituicaoTributariaDE        AS CstStDe,
                    fnc.CSOSNTributadoSemInscricaoEstadualDE AS CsosnTribSemIeDe,
                    fnc.CSOSNSubstituicaoTributariaDE        AS CsosnStDe
                FROM Fiscal_Natureza fn
                LEFT JOIN Fiscal_Natureza_CSTCSOSN_ICMS fnc
                    ON fnc.CodigoNatureza = fn.CodigoNatureza
                WHERE fn.CodigoNatureza = @nat",
                new { nat = _codigoNaturezaOperacaoNfce.Value }, cancellationToken: ct));
        }

        // ── 3) Determinar cenário (Tributado vs Substituição Tributária) ──
        // Sinal primário: CSOSN do Produto_NCM_Tributo (vinculado ao produto via CodigoTributo).
        // CSOSNs de ST: 201, 202, 203, 500, 900. CSOSNs Tributados: 101, 102, 103, 300, 400.
        bool simples = codigoRegime == 1 || codigoRegime == 2;
        string? csosnProduto = null;
        if (codigoTributoFallback.HasValue)
        {
            csosnProduto = await conn.ExecuteScalarAsync<string?>(new CommandDefinition(@"
                SELECT CSOSN FROM Produto_NCM_Tributo WHERE CodigoTributo = @ct",
                new { ct = codigoTributoFallback.Value }, cancellationToken: ct));
        }

        bool cenarioSt = csosnProduto switch
        {
            "201" or "202" or "203" or "500" or "900" => true,
            _ => false
        };

        // ── 4) Resolver CST/CSOSN ICMS de acordo com cenário e regime ──
        string? cstIcms = null;
        string? csosn = null;

        if (simples)
        {
            // Prioriza CSOSN específico do produto (Produto_NCM_Tributo).
            // Fallback na coluna correspondente da Natureza.
            csosn = !string.IsNullOrWhiteSpace(csosnProduto)
                ? csosnProduto
                : (cenarioSt ? natureza?.CsosnStDe : natureza?.CsosnTribSemIeDe);
            if (string.IsNullOrWhiteSpace(csosn)) csosn = null;
        }
        else
        {
            // Regime Normal: CST vem da Natureza no cenário escolhido.
            cstIcms = cenarioSt ? natureza?.CstStDe : natureza?.CstTribSemIeDe;
            if (string.IsNullOrWhiteSpace(cstIcms)) cstIcms = null;
        }

        // Acumula redução adicional da Natureza, se houver.
        if (natureza?.ReducaoNatureza is > 0 && reducao == 0) reducao = natureza.ReducaoNatureza.Value;

        // ── 5) CFOP coerente com o cenário ──
        // ST → CFOPSubstituicaoDE (5405 padrão). Tributado → CFOPTribPFDE (5102 padrão).
        // Inovafarma armazena com ponto (ex: "5.102"); schema da SEFAZ exige só dígitos.
        // Sem fallback hardcoded: se a Natureza não tiver o CFOP do cenário, retorna null
        // e a venda é bloqueada com erro claro (decisão consciente — fiscal não admite chute).
        string? cfop = cenarioSt ? natureza?.CfopStDe : natureza?.CfopTribDe;
        if (string.IsNullOrWhiteSpace(cfop)) cfop = natureza?.CfopEcfDe;
        if (!string.IsNullOrWhiteSpace(cfop))
        {
            cfop = new string(cfop.Where(char.IsDigit).ToArray());
            if (cfop.Length != 4) cfop = null;
        }
        else cfop = null;

        // ── 3) PIS/COFINS ──
        // Simples (1 ou 2): CST="49", alíquotas zeradas (paga via DAS).
        // Lucro Presumido (3): Cumulativo. Lucro Real (4): Não-Cumulativo.
        string cstPisCofins;
        decimal aliquotaPis;
        decimal aliquotaCofins;

        if (simples)
        {
            cstPisCofins = "49";
            aliquotaPis = 0;
            aliquotaCofins = 0;
        }
        else
        {
            cstPisCofins = !string.IsNullOrWhiteSpace(natureza?.CstPisCofinsNatureza)
                ? natureza!.CstPisCofinsNatureza!
                : (!string.IsNullOrWhiteSpace(bruto.PisCofinsCstProduto) ? bruto.PisCofinsCstProduto! :
                   !string.IsNullOrWhiteSpace(bruto.PisCofinsCstNcm) ? bruto.PisCofinsCstNcm! : "49");

            // Carrega alíquotas Cumulativo/Não-Cumulativo do CST escolhido.
            var aliq = await conn.QueryFirstOrDefaultAsync<PisCofinsAliquotas>(new CommandDefinition(@"
                SELECT PISCumulativo, CofinsCumulativo, PISNaoCumulativo, CofinsNaoCumulativo
                FROM Fiscal_PISCofins
                WHERE CodigoCST = @cst",
                new { cst = cstPisCofins }, cancellationToken: ct));

            if (codigoRegime == 4)
            {
                aliquotaPis = aliq?.PISNaoCumulativo ?? bruto.AliquotaPis ?? 0;
                aliquotaCofins = aliq?.CofinsNaoCumulativo ?? bruto.AliquotaCofins ?? 0;
            }
            else
            {
                aliquotaPis = aliq?.PISCumulativo ?? bruto.AliquotaPis ?? 0;
                aliquotaCofins = aliq?.CofinsCumulativo ?? bruto.AliquotaCofins ?? 0;
            }
        }

        // ── 4) NCM/CEST ──
        var ncm = (bruto.Ncm ?? "00000000").Replace(".", "").PadRight(8, '0');
        if (ncm.Length > 8) ncm = ncm[..8];
        var cest = !string.IsNullOrWhiteSpace(bruto.Cest) ? bruto.Cest : bruto.CestNcm;

        return new ProdutoFiscalSnapshotDto
        {
            Ncm = ncm,
            Cest = string.IsNullOrWhiteSpace(cest) ? null : cest,
            OrigemMercadoria = (bruto.Origem ?? 0).ToString(),
            Cfop = cfop,
            Unidade = string.IsNullOrWhiteSpace(bruto.Unidade) ? "UN" : bruto.Unidade!,

            CstIcms = cstIcms,
            Csosn = csosn,
            AliquotaIcms = aliquotaIcms,
            AliquotaFcp = aliquotaFcp,
            PercentualReducaoBc = reducao,
            CodigoBeneficioFiscal = string.IsNullOrWhiteSpace(codigoBeneficio) ? null : codigoBeneficio,

            CstPis = cstPisCofins,
            AliquotaPis = aliquotaPis,
            CstCofins = cstPisCofins,
            AliquotaCofins = aliquotaCofins
        };
    }

    public async Task<List<NaturezaOperacaoErpDto>> ListarNaturezasOperacaoSaidaAsync(CancellationToken ct = default)
    {
        var conn = await GetConnectionAsync(ct);
        var rows = await conn.QueryAsync<NaturezaOperacaoErpDto>(new CommandDefinition(@"
            SELECT CodigoNatureza AS Codigo, Natureza AS Nome
            FROM Fiscal_Natureza
            WHERE Operacao = 1
            ORDER BY Natureza",
            cancellationToken: ct));
        return rows.ToList();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
        GC.SuppressFinalize(this);
    }

    // ── tipos auxiliares de mapeamento ──────────────────────────
    private sealed class ProdutoBasico
    {
        public string CodigoExterno { get; set; } = string.Empty;
        public string? CodigoBarras { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Ncm { get; set; }
        public string? Unidade { get; set; }
    }

    private sealed class PrecoEstoque
    {
        public decimal? PrecoCheio { get; set; }
        public decimal? PrecoFinal { get; set; }
        public decimal? EstoqueAtual { get; set; }
    }

    private sealed class NaturezaFiscalRaw
    {
        public string? CstPisCofinsNatureza { get; set; }
        public string? CfopTribDe { get; set; }
        public string? CfopStDe { get; set; }
        public string? CfopEcfDe { get; set; }
        public decimal? ReducaoNatureza { get; set; }
        public string? CstTribSemIeDe { get; set; }
        public string? CstStDe { get; set; }
        public string? CsosnTribSemIeDe { get; set; }
        public string? CsosnStDe { get; set; }
    }

    private sealed class PisCofinsAliquotas
    {
        public decimal? PISCumulativo { get; set; }
        public decimal? CofinsCumulativo { get; set; }
        public decimal? PISNaoCumulativo { get; set; }
        public decimal? CofinsNaoCumulativo { get; set; }
    }

    private sealed class FiscalRaw
    {
        public string? Ncm { get; set; }
        public string? Cest { get; set; }
        public byte? Origem { get; set; }
        public string? PisCofinsCstProduto { get; set; }
        public string? Unidade { get; set; }

        public byte? PfuCodigoTributo { get; set; }
        public decimal? PfuIcms { get; set; }
        public decimal? PfuFcp { get; set; }
        public decimal? PfuReducao { get; set; }
        public decimal? PfuIcmsImportado { get; set; }
        public string? PfuCodigoBeneficio { get; set; }

        public byte? PnuCodigoTributo { get; set; }
        public decimal? PnuIcms { get; set; }
        public decimal? PnuReducao { get; set; }
        public decimal? PnuIcmsImportado { get; set; }

        public string? PisCofinsCstNcm { get; set; }
        public decimal? AliquotaPis { get; set; }
        public decimal? AliquotaCofins { get; set; }
        public string? CestNcm { get; set; }
    }
}
