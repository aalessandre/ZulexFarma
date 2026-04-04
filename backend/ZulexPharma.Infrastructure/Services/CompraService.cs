using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Globalization;
using System.Xml.Linq;
using ZulexPharma.Application.DTOs.Compras;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class CompraService : ICompraService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;

    private const string TELA = "Compras";
    private const string ENTIDADE = "Compra";

    private static readonly XNamespace _nfe = "http://www.portalfiscal.inf.br/nfe";

    public CompraService(AppDbContext db, ILogAcaoService log)
    {
        _db = db;
        _log = log;
    }

    // ── Listar ───────────────────────────────────────────────────────
    public async Task<List<CompraListDto>> ListarAsync()
    {
        try
        {
            return await _db.Compras
                .Include(c => c.Fornecedor).ThenInclude(f => f.Pessoa)
                .Include(c => c.Produtos)
                .OrderByDescending(c => c.CriadoEm)
                .Select(c => new CompraListDto
                {
                    Id = c.Id,
                    Codigo = c.Codigo,
                    NumeroNf = c.NumeroNf,
                    SerieNf = c.SerieNf,
                    FornecedorNome = c.Fornecedor.Pessoa.Nome,
                    FornecedorCnpj = c.Fornecedor.Pessoa.CpfCnpj,
                    DataEmissao = c.DataEmissao,
                    DataEntrada = c.DataEntrada,
                    ValorNota = c.ValorNota,
                    Status = c.Status,
                    TotalItens = c.Produtos.Count,
                    ItensVinculados = c.Produtos.Count(p => p.Vinculado),
                    ItensPrecificados = c.Produtos.Count(p => p.SugestaoVenda.HasValue || p.PrecificacaoAplicada),
                    CriadoEm = c.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em CompraService.ListarAsync");
            throw;
        }
    }

    // ── Obter detalhe ────────────────────────────────────────────────
    public async Task<CompraDetalheDto> ObterAsync(long id)
    {
        var compra = await _db.Compras
            .Include(c => c.Fornecedor).ThenInclude(f => f.Pessoa)
            .Include(c => c.Produtos).ThenInclude(p => p.Fiscal)
            .Include(c => c.Produtos).ThenInclude(p => p.Produto)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException($"Compra {id} não encontrada.");

        return MapDetalhe(compra);
    }

    // ── Importar XML ─────────────────────────────────────────────────
    public async Task<CompraDetalheDto> ImportarXmlAsync(string xmlConteudo, long filialId)
    {
        try
        {
            var doc = XDocument.Parse(xmlConteudo);
            var infNFe = doc.Descendants(_nfe + "infNFe").FirstOrDefault()
                ?? throw new ArgumentException("XML inválido: tag <infNFe> não encontrada.");

            // ── Extrair chave da NF-e ──────────────────────────────
            var chaveNfe = infNFe.Attribute("Id")?.Value?.Replace("NFe", "") ?? "";
            if (chaveNfe.Length != 44)
                throw new ArgumentException("Chave da NF-e inválida.");

            // Verificar duplicata
            if (await _db.Compras.AnyAsync(c => c.ChaveNfe == chaveNfe))
                throw new ArgumentException($"Esta nota fiscal já foi importada (Chave: {chaveNfe}).");

            // ── Emitente (fornecedor) ──────────────────────────────
            var emit = infNFe.Element(_nfe + "emit")
                ?? throw new ArgumentException("XML inválido: tag <emit> não encontrada.");
            var cnpjFornecedor = Txt(emit, "CNPJ");
            var nomeFornecedor = Txt(emit, "xNome");
            var fantasiaFornecedor = Txt(emit, "xFant");
            var ieFornecedor = Txt(emit, "IE");

            var fornecedor = await ObterOuCriarFornecedorAsync(cnpjFornecedor, nomeFornecedor, fantasiaFornecedor, ieFornecedor, emit);

            // ── Cabeçalho ──────────────────────────────────────────
            var ide = infNFe.Element(_nfe + "ide")!;
            var total = infNFe.Element(_nfe + "total")?.Element(_nfe + "ICMSTot");

            var compra = new Compra
            {
                FilialId = filialId,
                FornecedorId = fornecedor.Id,
                ChaveNfe = chaveNfe,
                NumeroNf = Txt(ide, "nNF"),
                SerieNf = Txt(ide, "serie"),
                NaturezaOperacao = Txt(ide, "natOp"),
                DataEmissao = ParseDt(Txt(ide, "dhEmi")),
                DataEntrada = DateTime.UtcNow,
                ValorProdutos = Dec(total, "vProd"),
                ValorSt = Dec(total, "vST"),
                ValorFcpSt = Dec(total, "vFCPST"),
                ValorFrete = Dec(total, "vFrete"),
                ValorSeguro = Dec(total, "vSeg"),
                ValorDesconto = Dec(total, "vDesc"),
                ValorIpi = Dec(total, "vIPI"),
                ValorPis = Dec(total, "vPIS"),
                ValorCofins = Dec(total, "vCOFINS"),
                ValorOutros = Dec(total, "vOutro"),
                ValorNota = Dec(total, "vNF"),
                Status = CompraStatus.PreEntrada,
                XmlConteudo = xmlConteudo
            };

            _db.Compras.Add(compra);
            await _db.SaveChangesAsync();

            // ── Itens ──────────────────────────────────────────────
            var dets = infNFe.Elements(_nfe + "det").ToList();
            foreach (var det in dets)
            {
                var prod = det.Element(_nfe + "prod")!;
                var imposto = det.Element(_nfe + "imposto");
                var rastro = prod.Element(_nfe + "rastro");
                var med = prod.Element(_nfe + "med");

                var codigoBarras = Txt(prod, "cEAN");
                if (codigoBarras == "SEM GTIN") codigoBarras = null;

                var item = new CompraProduto
                {
                    CompraId = compra.Id,
                    NumeroItem = int.Parse(det.Attribute("nItem")?.Value ?? "0"),
                    CodigoProdutoFornecedor = Txt(prod, "cProd"),
                    CodigoBarrasXml = codigoBarras,
                    DescricaoXml = Txt(prod, "xProd"),
                    NcmXml = Txt(prod, "NCM"),
                    CestXml = Txt(prod, "CEST"),
                    CfopXml = Txt(prod, "CFOP"),
                    UnidadeXml = Txt(prod, "uCom"),
                    Quantidade = Dec(prod, "qCom"),
                    ValorUnitario = Dec(prod, "vUnCom"),
                    ValorTotal = Dec(prod, "vProd"),
                    ValorDesconto = Dec(prod, "vDesc"),
                    ValorFrete = Dec(prod, "vFrete"),
                    ValorOutros = Dec(prod, "vOutro"),
                    ValorItemNota = Dec(det, "vItem"),
                    Lote = Txt(rastro, "nLote"),
                    DataFabricacao = ParseDt(Txt(rastro, "dFab")),
                    DataValidade = ParseDt(Txt(rastro, "dVal")),
                    CodigoAnvisa = Txt(med, "cProdANVISA"),
                    PrecoMaximoConsumidor = med != null ? DecN(med, "vPMC") : null,
                    InfoAdicional = Txt(det, "infAdProd"),
                    Vinculado = false
                };

                _db.ComprasProdutos.Add(item);
                await _db.SaveChangesAsync();

                // ── Fiscal ─────────────────────────────────────────
                var fiscal = ParseFiscal(imposto, item.Id);
                _db.ComprasFiscal.Add(fiscal);
            }

            await _db.SaveChangesAsync();

            // ── Auto-vincular por código de barras ──────────────────
            await AutoVincularProdutosAsync(compra.Id, fornecedor.Id, filialId);

            await _log.RegistrarAsync(TELA, "IMPORTAÇÃO XML", ENTIDADE, compra.Id,
                novo: new Dictionary<string, string?>
                {
                    ["NF"] = compra.NumeroNf,
                    ["Série"] = compra.SerieNf,
                    ["Chave"] = compra.ChaveNfe,
                    ["Fornecedor"] = nomeFornecedor,
                    ["Valor"] = compra.ValorNota.ToString("N2"),
                    ["Itens"] = dets.Count.ToString()
                });

            // Recarregar com navigations para retorno
            return await ObterAsync(compra.Id);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            Log.Error(ex, "Erro em CompraService.ImportarXmlAsync");
            throw;
        }
    }

    // ── Vincular produto ─────────────────────────────────────────────
    public async Task<CompraProdutoDto> VincularProdutoAsync(VincularProdutoDto dto)
    {
        try
        {
            var item = await _db.ComprasProdutos
                .Include(p => p.Compra)
                .FirstOrDefaultAsync(p => p.Id == dto.CompraProdutoId)
                ?? throw new KeyNotFoundException("Item de compra não encontrado.");

            var produto = await _db.Produtos
                .Include(p => p.Barras)
                .FirstOrDefaultAsync(p => p.Id == dto.ProdutoId)
                ?? throw new KeyNotFoundException("Produto não encontrado.");

            // Vincular
            item.ProdutoId = produto.Id;
            item.Vinculado = true;

            // Adicionar código de barras se não existe
            if (!string.IsNullOrEmpty(item.CodigoBarrasXml))
            {
                var barrasExiste = produto.Barras.Any(b => b.Barras == item.CodigoBarrasXml);
                if (!barrasExiste)
                {
                    _db.ProdutosBarras.Add(new ProdutoBarras
                    {
                        ProdutoId = produto.Id,
                        Barras = item.CodigoBarrasXml
                    });
                }
            }

            // Vincular fornecedor ao produto se não existe
            var compra = item.Compra;
            var fornecedorVinculado = await _db.ProdutosFornecedores
                .AnyAsync(pf => pf.ProdutoId == produto.Id
                    && pf.FornecedorId == compra.FornecedorId
                    && pf.FilialId == compra.FilialId);

            if (!fornecedorVinculado)
            {
                _db.ProdutosFornecedores.Add(new ProdutoFornecedor
                {
                    ProdutoId = produto.Id,
                    FornecedorId = compra.FornecedorId,
                    FilialId = compra.FilialId,
                    CodigoProdutoFornecedor = item.CodigoProdutoFornecedor,
                    NomeProduto = item.DescricaoXml
                });
            }

            await _db.SaveChangesAsync();

            await _log.RegistrarAsync(TELA, "VINCULAÇÃO", "CompraProduto", item.Id,
                novo: new Dictionary<string, string?>
                {
                    ["Produto"] = produto.Nome,
                    ["Barras XML"] = item.CodigoBarrasXml,
                    ["Descrição XML"] = item.DescricaoXml
                });

            return await MapProdutoDto(item.Id);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            Log.Error(ex, "Erro em CompraService.VincularProdutoAsync");
            throw;
        }
    }

    // ── Desvincular produto ──────────────────────────────────────────
    public async Task<CompraProdutoDto> DesvincularProdutoAsync(long compraProdutoId)
    {
        try
        {
            var item = await _db.ComprasProdutos
                .FirstOrDefaultAsync(p => p.Id == compraProdutoId)
                ?? throw new KeyNotFoundException("Item de compra não encontrado.");

            item.ProdutoId = null;
            item.Vinculado = false;
            await _db.SaveChangesAsync();

            return await MapProdutoDto(item.Id);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            Log.Error(ex, "Erro em CompraService.DesvincularProdutoAsync");
            throw;
        }
    }

    // ── Re-vincular ──────────────────────────────────────────────────
    public async Task<CompraDetalheDto> ReVincularAsync(long compraId)
    {
        var compra = await _db.Compras
            .Include(c => c.Produtos)
            .FirstOrDefaultAsync(c => c.Id == compraId)
            ?? throw new KeyNotFoundException($"Compra {compraId} não encontrada.");

        await AutoVincularProdutosAsync(compra.Id, compra.FornecedorId, compra.FilialId);
        return await ObterAsync(compra.Id);
    }

    // ── Precificação ─────────────────────────────────────────────────
    public async Task<PrecificacaoResult> GerarPrecificacaoAsync(PrecificacaoRequest request)
    {
        // Buscar itens vinculados das compras selecionadas
        var itensCompra = await _db.ComprasProdutos
            .Include(p => p.Fiscal)
            .Include(p => p.Compra)
            .Where(p => request.CompraIds.Contains(p.CompraId) && p.Vinculado && p.ProdutoId.HasValue)
            .ToListAsync();

        // Buscar dados dos produtos
        var produtoIds = itensCompra.Select(i => i.ProdutoId!.Value).Distinct().ToList();
        var produtos = await _db.Produtos
            .Include(p => p.Fabricante)
            .Where(p => produtoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        var dadosProdutos = await _db.ProdutosDados
            .Where(d => produtoIds.Contains(d.ProdutoId) && d.FilialId == request.FilialId)
            .ToDictionaryAsync(d => d.ProdutoId, d => d);

        // Buscar PMC ABCFarma
        var eans = produtos.Values
            .Where(p => !string.IsNullOrEmpty(p.CodigoBarras))
            .Select(p => p.CodigoBarras!).ToList();
        var barrasExtras = await _db.ProdutosBarras
            .Where(b => produtoIds.Contains(b.ProdutoId))
            .ToListAsync();
        var todosEans = eans.Concat(barrasExtras.Select(b => b.Barras)).Distinct().ToList();

        var abcFarma = await _db.AbcFarmaBase
            .Where(a => todosEans.Contains(a.Ean))
            .ToListAsync();
        var abcByEan = abcFarma.GroupBy(a => a.Ean)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.DataVigencia ?? DateTime.MinValue).First());

        // Buscar alíquota da filial
        var filial = await _db.Filiais.FindAsync(request.FilialId);
        var aliquota = filial?.AliquotaIcms ?? 18;

        var resultado = new List<PrecificacaoItem>();

        foreach (var item in itensCompra)
        {
            var produtoId = item.ProdutoId!.Value;
            if (!produtos.TryGetValue(produtoId, out var produto)) continue;
            dadosProdutos.TryGetValue(produtoId, out var dados);

            // Custo compra anterior (soma dos 8 campos no cadastro)
            decimal custoCompraAnterior = 0;
            if (dados != null)
            {
                custoCompraAnterior = dados.UltimaCompraUnitario + dados.UltimaCompraSt
                    + dados.UltimaCompraOutros + dados.UltimaCompraIpi + dados.UltimaCompraFpc
                    + dados.UltimaCompraBoleto + dados.UltimaCompraDifal + dados.UltimaCompraFrete;
            }

            // Custo compra atual (da nota)
            // Prioridade: ValorItemNota/Qtde > (ValorTotal + ST)/Qtde > ValorUnitario
            decimal custoCompraAtual;
            if (item.ValorItemNota > 0 && item.Quantidade > 0)
                custoCompraAtual = item.ValorItemNota / item.Quantidade;
            else if (item.ValorTotal > 0 && item.Quantidade > 0)
                custoCompraAtual = (item.ValorTotal + (item.Fiscal?.ValorSt ?? 0)) / item.Quantidade;
            else
                custoCompraAtual = item.ValorUnitario;

            // Custo médio
            var custoMedioAnterior = dados?.CustoMedio ?? 0;
            var estoqueAtual = dados?.EstoqueAtual ?? 0;
            decimal custoMedioAtual;
            if (estoqueAtual <= 0)
                custoMedioAtual = custoCompraAtual;
            else
                custoMedioAtual = ((estoqueAtual * custoMedioAnterior) + (item.Quantidade * custoCompraAtual))
                    / (estoqueAtual + item.Quantidade);
            custoMedioAtual = Math.Round(custoMedioAtual, 4);

            // Variações
            var varCustoCompra = custoCompraAnterior > 0
                ? Math.Round(((custoCompraAtual - custoCompraAnterior) / custoCompraAnterior) * 100, 2) : 0;
            var varCustoMedio = custoMedioAnterior > 0
                ? Math.Round(((custoMedioAtual - custoMedioAnterior) / custoMedioAnterior) * 100, 2) : 0;

            // Configuração de formação de preço
            var markup = dados?.Markup ?? 0;
            var projecao = dados?.ProjecaoLucro ?? 0;

            // Sugestão: usa projecaoLucro com custo compra como base padrão
            decimal sugVenda = dados?.ValorVenda ?? 0;
            if (projecao > 0 && projecao < 99 && custoCompraAtual > 0)
                sugVenda = Math.Round(custoCompraAtual / (1 - projecao / 100), 2);
            else if (markup > 0 && custoCompraAtual > 0)
                sugVenda = Math.Round(custoCompraAtual * (1 + markup / 100), 2);

            // PMC
            var pmcNota = item.PrecoMaximoConsumidor ?? 0;
            decimal pmcAbcFarma = 0;
            AbcFarmaBase? abc = null;
            if (!string.IsNullOrEmpty(produto.CodigoBarras))
                abcByEan.TryGetValue(produto.CodigoBarras, out abc);
            if (abc == null)
            {
                var barrasProd = barrasExtras.Where(b => b.ProdutoId == produtoId).Select(b => b.Barras);
                foreach (var b in barrasProd)
                    if (abcByEan.TryGetValue(b, out abc)) break;
            }
            if (abc != null)
                pmcAbcFarma = ObterPmcPorAliquota(abc, aliquota);

            resultado.Add(new PrecificacaoItem
            {
                ProdutoId = produtoId,
                ProdutoDadosId = dados?.Id ?? 0,
                CompraProdutoId = item.Id,
                ProdutoNome = produto.Nome,
                Ean = produto.CodigoBarras,
                FabricanteNome = produto.Fabricante?.Nome,
                CustoCompraAnterior = Math.Round(custoCompraAnterior, 4),
                CustoCompraAtual = Math.Round(custoCompraAtual, 4),
                VarCustoCompraPercent = varCustoCompra,
                CustoMedioAnterior = custoMedioAnterior,
                CustoMedioAtual = custoMedioAtual,
                VarCustoMedioPercent = varCustoMedio,
                PrecoVendaAtual = dados?.ValorVenda ?? 0,
                SugestaoVendaCustoCompra = sugVenda,
                SugestaoVendaCustoMedio = sugVenda,
                NovoPrecoVenda = sugVenda,
                PmcNota = pmcNota,
                PmcAbcFarma = pmcAbcFarma,
                Markup = markup,
                ProjecaoLucro = projecao,
                Quantidade = item.Quantidade
            });
        }

        return new PrecificacaoResult { TotalProdutos = resultado.Count, Itens = resultado };
    }

    public async Task<int> AplicarPrecificacaoAsync(AplicarPrecificacaoRequest request)
    {
        var alterados = 0;
        foreach (var item in request.Itens)
        {
            var dados = await _db.ProdutosDados.FindAsync(item.ProdutoDadosId);
            if (dados == null) continue;

            var anterior = new Dictionary<string, string?> {
                ["Vlr Venda"] = dados.ValorVenda.ToString("N2"),
                ["Custo Médio"] = dados.CustoMedio.ToString("N2"),
                ["PMC"] = dados.Pmc.ToString("N2")
            };

            dados.ValorVenda = item.NovoPrecoVenda;
            dados.Markup = item.NovoMarkup;
            dados.ProjecaoLucro = item.NovaProjecaoLucro;
            dados.Pmc = item.NovoPmc > 0 ? item.NovoPmc : dados.Pmc;
            // CustoMedio e custos da compra serão atualizados na finalização da nota

            // Marcar item da compra como precificado
            if (item.CompraProdutoId > 0)
            {
                var cp = await _db.ComprasProdutos.FindAsync(item.CompraProdutoId);
                if (cp != null)
                {
                    cp.PrecificacaoAplicada = true;
                    cp.SugestaoVenda = item.NovoPrecoVenda;
                    cp.SugestaoMarkup = item.NovoMarkup;
                    cp.SugestaoProjecao = item.NovaProjecaoLucro;
                }
            }

            await _log.RegistrarAsync("Produtos", "AJUSTE PREÇO (COMPRA)", "Produto", item.ProdutoId,
                anterior: anterior,
                novo: new Dictionary<string, string?> {
                    ["Vlr Venda"] = dados.ValorVenda.ToString("N2"),
                    ["Custo Médio"] = dados.CustoMedio.ToString("N2"),
                    ["PMC"] = dados.Pmc.ToString("N2"),
                    ["Markup %"] = dados.Markup.ToString("N2"),
                    ["Proj. Lucro %"] = dados.ProjecaoLucro.ToString("N2")
                });

            alterados++;
        }

        await _db.SaveChangesAsync();
        return alterados;
    }

    public async Task<int> SalvarSugestoesAsync(SalvarSugestaoRequest request)
    {
        var salvos = 0;
        foreach (var item in request.Itens)
        {
            var cp = await _db.ComprasProdutos.FindAsync(item.CompraProdutoId);
            if (cp == null) continue;

            cp.SugestaoVenda = item.SugestaoVenda;
            cp.SugestaoMarkup = item.SugestaoMarkup;
            cp.SugestaoProjecao = item.SugestaoProjecao;
            cp.SugestaoCustoMedio = item.SugestaoCustoMedio;
            cp.PrecificacaoAplicada = false;
            salvos++;
        }
        await _db.SaveChangesAsync();
        return salvos;
    }

    private static decimal ObterPmcPorAliquota(Domain.Entities.AbcFarmaBase abc, decimal aliquota)
    {
        return aliquota switch
        {
            0 => abc.Pmc0, 12 => abc.Pmc12, 17 => abc.Pmc17, 18 => abc.Pmc18,
            19 => abc.Pmc19, 19.5m => abc.Pmc195, 20 => abc.Pmc20, 20.5m => abc.Pmc205,
            21 => abc.Pmc21, 22 => abc.Pmc22, 22.5m => abc.Pmc225, 23 => abc.Pmc23,
            _ => abc.Pmc18
        };
    }

    // ── Excluir ──────────────────────────────────────────────────────
    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var compra = await _db.Compras
                .Include(c => c.Produtos).ThenInclude(p => p.Fiscal)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new KeyNotFoundException($"Compra {id} não encontrada.");

            if (compra.Status == CompraStatus.Finalizada)
                throw new ArgumentException("Não é possível excluir uma compra finalizada.");

            _db.Compras.Remove(compra);
            await _db.SaveChangesAsync();

            await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id,
                anterior: new Dictionary<string, string?>
                {
                    ["NF"] = compra.NumeroNf,
                    ["Chave"] = compra.ChaveNfe,
                    ["Valor"] = compra.ValorNota.ToString("N2")
                });

            return "excluido";
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException)
        {
            Log.Error(ex, "Erro em CompraService.ExcluirAsync | Id: {Id}", id);
            throw;
        }
    }

    // ═════════════════════════════════════════════════════════════════
    // HELPERS PRIVADOS
    // ═════════════════════════════════════════════════════════════════

    private async Task<Fornecedor> ObterOuCriarFornecedorAsync(
        string cnpj, string nome, string? fantasia, string? ie, XElement emit)
    {
        var pessoaExistente = await _db.Pessoas
            .Include(p => p.Fornecedor)
            .FirstOrDefaultAsync(p => p.CpfCnpj == cnpj);

        if (pessoaExistente?.Fornecedor != null)
            return pessoaExistente.Fornecedor;

        Pessoa pessoa;
        if (pessoaExistente != null)
        {
            pessoa = pessoaExistente;
        }
        else
        {
            pessoa = new Pessoa
            {
                Tipo = "J",
                Nome = (fantasia ?? nome).Trim().ToUpper(),
                RazaoSocial = nome.Trim().ToUpper(),
                CpfCnpj = cnpj,
                InscricaoEstadual = ie?.Trim().ToUpper()
            };
            _db.Pessoas.Add(pessoa);
            await _db.SaveChangesAsync();

            // Endereço do emitente
            var enderEmit = emit.Element(_nfe + "enderEmit");
            if (enderEmit != null)
            {
                _db.PessoasEndereco.Add(new PessoaEndereco
                {
                    PessoaId = pessoa.Id,
                    Tipo = "PRINCIPAL",
                    Rua = Txt(enderEmit, "xLgr").ToUpper(),
                    Numero = Txt(enderEmit, "nro").ToUpper(),
                    Bairro = Txt(enderEmit, "xBairro").ToUpper(),
                    Cidade = Txt(enderEmit, "xMun").ToUpper(),
                    Uf = Txt(enderEmit, "UF").ToUpper(),
                    Cep = Txt(enderEmit, "CEP"),
                    Principal = true
                });
            }

            // Telefone do emitente
            var fone = Txt(emit.Element(_nfe + "enderEmit"), "fone");
            if (!string.IsNullOrEmpty(fone))
            {
                _db.PessoasContato.Add(new PessoaContato
                {
                    PessoaId = pessoa.Id,
                    Tipo = "TELEFONE",
                    Valor = fone,
                    Principal = true
                });
            }

            await _db.SaveChangesAsync();
        }

        var fornecedor = new Fornecedor
        {
            PessoaId = pessoa.Id,
            Ativo = true
        };
        _db.Fornecedores.Add(fornecedor);
        await _db.SaveChangesAsync();

        return fornecedor;
    }

    private async Task AutoVincularProdutosAsync(long compraId, long fornecedorId, long filialId)
    {
        var itens = await _db.ComprasProdutos
            .Where(p => p.CompraId == compraId && !p.Vinculado)
            .ToListAsync();

        foreach (var item in itens)
        {
            long? produtoId = null;

            // 1. Buscar por código de barras
            if (!string.IsNullOrEmpty(item.CodigoBarrasXml))
            {
                produtoId = await _db.ProdutosBarras
                    .Where(b => b.Barras == item.CodigoBarrasXml)
                    .Select(b => b.ProdutoId)
                    .FirstOrDefaultAsync();
            }

            // 2. Buscar por código do produto no fornecedor
            if (produtoId == null || produtoId == 0)
            {
                if (!string.IsNullOrEmpty(item.CodigoProdutoFornecedor))
                {
                    produtoId = await _db.ProdutosFornecedores
                        .Where(pf => pf.FornecedorId == fornecedorId
                            && pf.FilialId == filialId
                            && pf.CodigoProdutoFornecedor == item.CodigoProdutoFornecedor)
                        .Select(pf => pf.ProdutoId)
                        .FirstOrDefaultAsync();
                }
            }

            if (produtoId != null && produtoId > 0)
            {
                item.ProdutoId = produtoId;
                item.Vinculado = true;

                // Garantir vínculo fornecedor-produto
                var existeVinculo = await _db.ProdutosFornecedores
                    .AnyAsync(pf => pf.ProdutoId == produtoId
                        && pf.FornecedorId == fornecedorId
                        && pf.FilialId == filialId);

                if (!existeVinculo)
                {
                    _db.ProdutosFornecedores.Add(new ProdutoFornecedor
                    {
                        ProdutoId = produtoId.Value,
                        FornecedorId = fornecedorId,
                        FilialId = filialId,
                        CodigoProdutoFornecedor = item.CodigoProdutoFornecedor,
                        NomeProduto = item.DescricaoXml
                    });
                }

                // Garantir código de barras no cadastro
                if (!string.IsNullOrEmpty(item.CodigoBarrasXml))
                {
                    var barrasExiste = await _db.ProdutosBarras
                        .AnyAsync(b => b.ProdutoId == produtoId && b.Barras == item.CodigoBarrasXml);
                    if (!barrasExiste)
                    {
                        _db.ProdutosBarras.Add(new ProdutoBarras
                        {
                            ProdutoId = produtoId.Value,
                            Barras = item.CodigoBarrasXml
                        });
                    }
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    private CompraFiscal ParseFiscal(XElement? imposto, long compraProdutoId)
    {
        var fiscal = new CompraFiscal { CompraProdutoId = compraProdutoId };
        if (imposto == null) return fiscal;

        // ICMS — pode ser ICMS00, ICMS10, ICMS20, ICMS60, etc.
        var icmsGroup = imposto.Element(_nfe + "ICMS");
        var icms = icmsGroup?.Elements().FirstOrDefault();
        if (icms != null)
        {
            fiscal.OrigemMercadoria = Txt(icms, "orig");
            fiscal.CstIcms = Txt(icms, "CST");
            fiscal.BaseIcms = Dec(icms, "vBC");
            fiscal.AliquotaIcms = Dec(icms, "pICMS");
            fiscal.ValorIcms = Dec(icms, "vICMS");
            fiscal.ModalidadeBcSt = Txt(icms, "modBCST");
            fiscal.MvaSt = Dec(icms, "pMVAST");
            fiscal.BaseSt = Dec(icms, "vBCST");
            fiscal.AliquotaSt = Dec(icms, "pICMSST");
            fiscal.ValorSt = Dec(icms, "vICMSST");
            fiscal.BaseFcpSt = Dec(icms, "vBCFCPST");
            fiscal.AliquotaFcpSt = Dec(icms, "pFCPST");
            fiscal.ValorFcpSt = Dec(icms, "vFCPST");
        }

        // PIS
        var pisGroup = imposto.Element(_nfe + "PIS");
        var pis = pisGroup?.Elements().FirstOrDefault();
        if (pis != null)
        {
            fiscal.CstPis = Txt(pis, "CST");
            fiscal.BasePis = Dec(pis, "vBC");
            fiscal.AliquotaPis = Dec(pis, "pPIS");
            fiscal.ValorPis = Dec(pis, "vPIS");
        }

        // COFINS
        var cofinsGroup = imposto.Element(_nfe + "COFINS");
        var cofins = cofinsGroup?.Elements().FirstOrDefault();
        if (cofins != null)
        {
            fiscal.CstCofins = Txt(cofins, "CST");
            fiscal.BaseCofins = Dec(cofins, "vBC");
            fiscal.AliquotaCofins = Dec(cofins, "pCOFINS");
            fiscal.ValorCofins = Dec(cofins, "vCOFINS");
        }

        // IBS/CBS (Reforma Tributária)
        var ibscbs = imposto.Element(_nfe + "IBSCBS");
        if (ibscbs != null)
        {
            fiscal.CstIbsCbs = Txt(ibscbs, "CST");
            fiscal.ClasseTributariaIbsCbs = Txt(ibscbs, "cClassTrib");
            var gIbsCbs = ibscbs.Element(_nfe + "gIBSCBS");
            if (gIbsCbs != null)
            {
                fiscal.BaseIbsCbs = Dec(gIbsCbs, "vBC");
                var gIbsUf = gIbsCbs.Element(_nfe + "gIBSUF");
                fiscal.AliquotaIbsUf = Dec(gIbsUf, "pIBSUF");
                fiscal.ValorIbsUf = Dec(gIbsUf, "vIBSUF");
                var gIbsMun = gIbsCbs.Element(_nfe + "gIBSMun");
                fiscal.AliquotaIbsMun = Dec(gIbsMun, "pIBSMun");
                fiscal.ValorIbsMun = Dec(gIbsMun, "vIBSMun");
                var gCbs = gIbsCbs.Element(_nfe + "gCBS");
                fiscal.AliquotaCbs = Dec(gCbs, "pCBS");
                fiscal.ValorCbs = Dec(gCbs, "vCBS");
            }
        }

        return fiscal;
    }

    private CompraDetalheDto MapDetalhe(Compra c)
    {
        return new CompraDetalheDto
        {
            Id = c.Id,
            Codigo = c.Codigo,
            FilialId = c.FilialId,
            FornecedorId = c.FornecedorId,
            FornecedorNome = c.Fornecedor.Pessoa.Nome,
            FornecedorCnpj = c.Fornecedor.Pessoa.CpfCnpj,
            ChaveNfe = c.ChaveNfe,
            NumeroNf = c.NumeroNf,
            SerieNf = c.SerieNf,
            NaturezaOperacao = c.NaturezaOperacao,
            DataEmissao = c.DataEmissao,
            DataEntrada = c.DataEntrada,
            ValorProdutos = c.ValorProdutos,
            ValorSt = c.ValorSt,
            ValorFcpSt = c.ValorFcpSt,
            ValorFrete = c.ValorFrete,
            ValorSeguro = c.ValorSeguro,
            ValorDesconto = c.ValorDesconto,
            ValorIpi = c.ValorIpi,
            ValorPis = c.ValorPis,
            ValorCofins = c.ValorCofins,
            ValorOutros = c.ValorOutros,
            ValorNota = c.ValorNota,
            Status = c.Status,
            CriadoEm = c.CriadoEm,
            Produtos = c.Produtos.OrderBy(p => p.NumeroItem).Select(p => new CompraProdutoDto
            {
                Id = p.Id,
                NumeroItem = p.NumeroItem,
                ProdutoId = p.ProdutoId,
                ProdutoNome = p.Produto?.Nome,
                ProdutoCodigoBarras = p.Produto?.CodigoBarras,
                CodigoProdutoFornecedor = p.CodigoProdutoFornecedor,
                CodigoBarrasXml = p.CodigoBarrasXml,
                DescricaoXml = p.DescricaoXml,
                NcmXml = p.NcmXml,
                CestXml = p.CestXml,
                CfopXml = p.CfopXml,
                UnidadeXml = p.UnidadeXml,
                Quantidade = p.Quantidade,
                ValorUnitario = p.ValorUnitario,
                ValorTotal = p.ValorTotal,
                ValorDesconto = p.ValorDesconto,
                ValorFrete = p.ValorFrete,
                ValorOutros = p.ValorOutros,
                ValorItemNota = p.ValorItemNota,
                Lote = p.Lote,
                DataFabricacao = p.DataFabricacao,
                DataValidade = p.DataValidade,
                CodigoAnvisa = p.CodigoAnvisa,
                PrecoMaximoConsumidor = p.PrecoMaximoConsumidor,
                Vinculado = p.Vinculado,
                InfoAdicional = p.InfoAdicional,
                Fiscal = p.Fiscal != null ? new CompraFiscalDto
                {
                    OrigemMercadoria = p.Fiscal.OrigemMercadoria,
                    CstIcms = p.Fiscal.CstIcms,
                    BaseIcms = p.Fiscal.BaseIcms,
                    AliquotaIcms = p.Fiscal.AliquotaIcms,
                    ValorIcms = p.Fiscal.ValorIcms,
                    ModalidadeBcSt = p.Fiscal.ModalidadeBcSt,
                    MvaSt = p.Fiscal.MvaSt,
                    BaseSt = p.Fiscal.BaseSt,
                    AliquotaSt = p.Fiscal.AliquotaSt,
                    ValorSt = p.Fiscal.ValorSt,
                    BaseFcpSt = p.Fiscal.BaseFcpSt,
                    AliquotaFcpSt = p.Fiscal.AliquotaFcpSt,
                    ValorFcpSt = p.Fiscal.ValorFcpSt,
                    CstPis = p.Fiscal.CstPis,
                    BasePis = p.Fiscal.BasePis,
                    AliquotaPis = p.Fiscal.AliquotaPis,
                    ValorPis = p.Fiscal.ValorPis,
                    CstCofins = p.Fiscal.CstCofins,
                    BaseCofins = p.Fiscal.BaseCofins,
                    AliquotaCofins = p.Fiscal.AliquotaCofins,
                    ValorCofins = p.Fiscal.ValorCofins,
                    CstIbsCbs = p.Fiscal.CstIbsCbs,
                    ClasseTributariaIbsCbs = p.Fiscal.ClasseTributariaIbsCbs,
                    BaseIbsCbs = p.Fiscal.BaseIbsCbs,
                    AliquotaIbsUf = p.Fiscal.AliquotaIbsUf,
                    ValorIbsUf = p.Fiscal.ValorIbsUf,
                    AliquotaIbsMun = p.Fiscal.AliquotaIbsMun,
                    ValorIbsMun = p.Fiscal.ValorIbsMun,
                    AliquotaCbs = p.Fiscal.AliquotaCbs,
                    ValorCbs = p.Fiscal.ValorCbs
                } : null
            }).ToList()
        };
    }

    private async Task<CompraProdutoDto> MapProdutoDto(long compraProdutoId)
    {
        var p = await _db.ComprasProdutos
            .Include(x => x.Fiscal)
            .Include(x => x.Produto)
            .FirstAsync(x => x.Id == compraProdutoId);

        return new CompraProdutoDto
        {
            Id = p.Id,
            NumeroItem = p.NumeroItem,
            ProdutoId = p.ProdutoId,
            ProdutoNome = p.Produto?.Nome,
            ProdutoCodigoBarras = p.Produto?.CodigoBarras,
            CodigoProdutoFornecedor = p.CodigoProdutoFornecedor,
            CodigoBarrasXml = p.CodigoBarrasXml,
            DescricaoXml = p.DescricaoXml,
            NcmXml = p.NcmXml,
            CestXml = p.CestXml,
            CfopXml = p.CfopXml,
            UnidadeXml = p.UnidadeXml,
            Quantidade = p.Quantidade,
            ValorUnitario = p.ValorUnitario,
            ValorTotal = p.ValorTotal,
            ValorDesconto = p.ValorDesconto,
            ValorFrete = p.ValorFrete,
            ValorOutros = p.ValorOutros,
            ValorItemNota = p.ValorItemNota,
            Lote = p.Lote,
            DataFabricacao = p.DataFabricacao,
            DataValidade = p.DataValidade,
            CodigoAnvisa = p.CodigoAnvisa,
            PrecoMaximoConsumidor = p.PrecoMaximoConsumidor,
            Vinculado = p.Vinculado,
            InfoAdicional = p.InfoAdicional
        };
    }

    // ── XML Helpers ──────────────────────────────────────────────────

    private static string Txt(XElement? el, string tag)
        => el?.Element(_nfe + tag)?.Value?.Trim() ?? "";

    private static decimal Dec(XElement? el, string tag)
    {
        var val = Txt(el, tag);
        return decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;
    }

    private static decimal? DecN(XElement? el, string tag)
    {
        var val = Txt(el, tag);
        return decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static DateTime? ParseDt(string? val)
    {
        if (string.IsNullOrEmpty(val)) return null;
        if (DateTimeOffset.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            return dto.UtcDateTime;
        if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return null;
    }
}
