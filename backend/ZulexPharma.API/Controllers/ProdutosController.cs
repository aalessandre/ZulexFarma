using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Produtos;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _service;
    private readonly ILogAcaoService _log;
    private readonly AppDbContext _db;

    public ProdutosController(IProdutoService service, ILogAcaoService log, AppDbContext db)
    {
        _service = service;
        _log = log;
        _db = db;
    }

    [HttpGet("verificar-barras/{barras}")]
    public async Task<IActionResult> VerificarBarras(string barras, [FromQuery] long? excluirProdutoId = null)
    {
        try
        {
            var query = _db.ProdutosBarras
                .Include(b => b.Produto)
                .Where(b => b.Barras == barras.Trim());

            if (excluirProdutoId.HasValue)
                query = query.Where(b => b.ProdutoId != excluirProdutoId.Value);

            var existente = await query.FirstOrDefaultAsync();

            // Também verificar no campo CodigoBarras principal
            if (existente == null)
            {
                var queryPrincipal = _db.Produtos
                    .Where(p => p.CodigoBarras == barras.Trim() && !p.Eliminado);
                if (excluirProdutoId.HasValue)
                    queryPrincipal = queryPrincipal.Where(p => p.Id != excluirProdutoId.Value);
                var prodPrincipal = await queryPrincipal.FirstOrDefaultAsync();
                if (prodPrincipal != null)
                    return Ok(new { success = true, data = new { existe = true, produtoId = prodPrincipal.Id, produtoNome = prodPrincipal.Nome, codigoBarras = prodPrincipal.CodigoBarras } });
            }

            if (existente == null)
                return Ok(new { success = true, data = new { existe = false } });

            return Ok(new { success = true, data = new {
                existe = true,
                produtoId = existente.ProdutoId,
                produtoNome = existente.Produto.Nome,
                codigoBarras = existente.Produto.CodigoBarras
            }});
        }
        catch (Exception ex) { return await ErroInterno(ex, "VerificarBarras"); }
    }

    /// <summary>Busca avançada de produtos com múltiplos filtros. Retorna lista para seleção múltipla.</summary>
    [HttpGet("busca-avancada")]
    public async Task<IActionResult> BuscaAvancada(
        [FromQuery] long filialId = 1,
        [FromQuery] string? descricao = null,
        [FromQuery] long? fabricanteId = null,
        [FromQuery] long? fornecedorId = null,
        [FromQuery] long? grupoPrincipalId = null,
        [FromQuery] long? grupoProdutoId = null,
        [FromQuery] long? subGrupoId = null,
        [FromQuery] long? secaoId = null,
        [FromQuery] long? familiaId = null,
        [FromQuery] decimal? precoMin = null,
        [FromQuery] decimal? precoMax = null,
        [FromQuery] decimal? estoqueMinimo = null,
        [FromQuery] string? status = "ativos",
        [FromQuery] int limit = 200)
    {
        try
        {
            var query = _db.Produtos.AsQueryable();

            // Filtro ativo/inativo
            if (status == "ativos") query = query.Where(p => p.Ativo);
            else if (status == "inativos") query = query.Where(p => !p.Ativo);

            // Filtro por descrição
            if (!string.IsNullOrWhiteSpace(descricao))
            {
                var termo = descricao.Trim().ToUpper();
                query = query.Where(p => p.Nome.ToUpper().Contains(termo) || p.Codigo!.Contains(termo));
            }

            // Filtros de agrupamento
            if (fabricanteId.HasValue) query = query.Where(p => p.FabricanteId == fabricanteId);
            if (grupoPrincipalId.HasValue) query = query.Where(p => p.GrupoPrincipalId == grupoPrincipalId);
            if (grupoProdutoId.HasValue) query = query.Where(p => p.GrupoProdutoId == grupoProdutoId);
            if (subGrupoId.HasValue) query = query.Where(p => p.SubGrupoId == subGrupoId);

            // Fornecedor (via ProdutoFornecedor)
            if (fornecedorId.HasValue)
                query = query.Where(p => _db.ProdutosFornecedores.Any(pf => pf.ProdutoId == p.Id && pf.FornecedorId == fornecedorId));

            var prodIds = await query.OrderBy(p => p.Nome).Take(limit).Select(p => p.Id).ToListAsync();

            // Buscar dados da filial para filtros de preço/estoque/família/seção
            var dadosQuery = _db.ProdutosDados
                .Where(d => prodIds.Contains(d.ProdutoId) && d.FilialId == filialId);

            if (precoMin.HasValue) dadosQuery = dadosQuery.Where(d => d.ValorVenda >= precoMin);
            if (precoMax.HasValue) dadosQuery = dadosQuery.Where(d => d.ValorVenda <= precoMax);
            if (estoqueMinimo.HasValue) dadosQuery = dadosQuery.Where(d => d.EstoqueAtual >= estoqueMinimo);
            if (familiaId.HasValue) dadosQuery = dadosQuery.Where(d => d.ProdutoFamiliaId == familiaId);
            if (secaoId.HasValue) dadosQuery = dadosQuery.Where(d => d.SecaoId == secaoId);

            var dados = await dadosQuery.Select(d => new { d.ProdutoId, d.ValorVenda, d.CustoMedio, d.EstoqueAtual, d.CurvaAbc }).ToListAsync();
            var dadosIds = dados.Select(d => d.ProdutoId).ToHashSet();

            // Buscar produtos com dados resolvidos
            var produtos = await _db.Produtos
                .Where(p => dadosIds.Contains(p.Id))
                .Include(p => p.Fabricante)
                .OrderBy(p => p.Nome)
                .Select(p => new { p.Id, p.Codigo, p.Nome, fabricante = p.Fabricante != null ? p.Fabricante.Nome : "", p.PermitirConferenciaDigitando, p.ClasseTerapeutica })
                .ToListAsync();

            var result = produtos.Select(p =>
            {
                var d = dados.FirstOrDefault(x => x.ProdutoId == p.Id);
                return new
                {
                    id = p.Id, codigo = p.Codigo, nome = p.Nome, fabricante = p.fabricante,
                    valorVenda = d?.ValorVenda ?? 0, custoMedio = d?.CustoMedio ?? 0,
                    estoqueAtual = d?.EstoqueAtual ?? 0, curvaAbc = d?.CurvaAbc ?? ""
                };
            });

            return Ok(new { success = true, data = result, total = result.Count() });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ProdutosController.BuscaAvancada"); return StatusCode(500, new { success = false, message = "Erro na busca avançada." }); }
    }

    /// <summary>Busca leve de produtos para promoções e seleção rápida. Retorna dados de preço/custo/estoque.</summary>
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string termo, [FromQuery] long filialId = 1, [FromQuery] int limit = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(termo) || termo.Trim().Length < 3)
                return Ok(new { success = true, data = Array.Empty<object>() });

            // Código de barras de balança (peso/preço embutido) → resolve o produto pesável
            // pelo PLU e devolve a quantidade (peso) já calculada.
            var balanca = await TryResolverBalancaAsync(termo.Trim(), filialId);
            if (balanca != null)
                return Ok(new { success = true, data = new List<object> { balanca } });

            var termoNorm = termo.Trim().ToUpper();

            var produtos = await _db.Produtos
                .Where(p => p.Ativo && (
                    p.Nome.ToUpper().Contains(termoNorm) ||
                    p.Codigo!.Contains(termoNorm) ||
                    (p.CodigoBarras != null && p.CodigoBarras.Contains(termoNorm)) ||
                    p.Barras.Any(b => b.Barras.Contains(termoNorm))
                ))
                .OrderBy(p => p.Nome)
                .Take(limit)
                .Select(p => new { p.Id, p.Codigo, p.Nome, fabricante = p.Fabricante != null ? p.Fabricante.Nome : "", p.PermitirConferenciaDigitando, p.ClasseTerapeutica, p.PrecoFp, p.PrecoFpBolsaFamilia, p.ParticipaFarmaciaPopular, p.CodigoBarras, p.ControlaGrade, TemVariacoes = p.Variacoes.Any(v => v.Ativo) })
                .ToListAsync();

            // Buscar dados da filial
            var prodIds = produtos.Select(p => p.Id).ToList();
            var dados = await _db.ProdutosDados
                .Where(d => prodIds.Contains(d.ProdutoId) && d.FilialId == filialId)
                .Select(d => new { d.ProdutoId, d.ValorVenda, d.CustoMedio, d.EstoqueAtual, d.CurvaAbc })
                .ToListAsync();

            // Verificar quais produtos têm promoção ativa
            var agora = DataHoraHelper.Agora();
            var diaAtual = (int)Math.Pow(2, (int)agora.DayOfWeek);

            // Debug: diagnosticar filtros
            var debugPromos = await _db.PromocaoProdutos
                .Include(pp => pp.Promocao).ThenInclude(p => p.Filiais)
                .Where(pp => prodIds.Contains(pp.ProdutoId) && pp.Promocao.Ativo)
                .ToListAsync();
            foreach (var dp in debugPromos)
            {
                var p = dp.Promocao;
                var filiais = string.Join(",", p.Filiais.Select(f => f.FilialId));
                Log.Information("Promo debug: promoId={Id}, nome={Nome}, inicio={Inicio}, fim={Fim}, diaSemana={Dia}, filiais=[{Filiais}], diaAtualBit={DiaAtual}, dataOk={DataOk}, diaOk={DiaOk}, filialOk={FilialOk}",
                    p.Id, p.Nome, p.DataHoraInicio, p.DataHoraFim, p.DiaSemana, filiais, diaAtual,
                    p.DataHoraInicio <= agora && (p.DataHoraFim == null || p.DataHoraFim >= agora),
                    (p.DiaSemana & diaAtual) != 0,
                    p.Filiais.Any(f => f.FilialId == filialId));
            }

            var promoIds = await _db.PromocaoProdutos
                .Where(pp => prodIds.Contains(pp.ProdutoId)
                    && pp.Promocao.Ativo
                    && pp.Promocao.DataHoraInicio <= agora
                    && (pp.Promocao.DataHoraFim == null || pp.Promocao.DataHoraFim >= agora)
                    && (pp.Promocao.DiaSemana & diaAtual) != 0
                    && pp.Promocao.Filiais.Any(f => f.FilialId == filialId))
                .Select(pp => pp.ProdutoId)
                .Distinct()
                .ToListAsync();
            Log.Information("Buscar produtos promo filtrado: {Count} produtos com promoção", promoIds.Count);
            var promoSet = new HashSet<long>(promoIds);

            var result = produtos.Select(p =>
            {
                var d = dados.FirstOrDefault(x => x.ProdutoId == p.Id);
                return new
                {
                    id = p.Id, codigo = p.Codigo, nome = p.Nome, fabricante = p.fabricante,
                    valorVenda = d?.ValorVenda ?? 0, custoMedio = d?.CustoMedio ?? 0,
                    estoqueAtual = d?.EstoqueAtual ?? 0, curvaAbc = d?.CurvaAbc ?? "",
                    temPromocao = promoSet.Contains(p.Id),
                    permitirConferenciaDigitando = p.PermitirConferenciaDigitando,
                    classeTerapeutica = p.ClasseTerapeutica,
                    precoFp = p.PrecoFp,
                    precoFpBolsaFamilia = p.PrecoFpBolsaFamilia,
                    participaFarmaciaPopular = p.ParticipaFarmaciaPopular,
                    codigoBarras = p.CodigoBarras,
                    // Precisa do picker se o produto tem grade OU tem variações ativas
                    // (mesmo que o flag ControlaGrade não tenha sido marcado no cadastro).
                    controlaGrade = p.ControlaGrade || p.TemVariacoes,
                    produtoVariacaoId = (long?)null,
                    variacaoDescricao = (string?)null
                };
            });

            // Também casa código de barras de VARIAÇÃO (grade): resolve direto o SKU,
            // com preço da variação (ValorVenda do SKU) e fallback pro preço do produto.
            var variacoes = await _db.ProdutosVariacoes
                .Where(v => v.Ativo && v.CodigoBarras != null && v.CodigoBarras.ToUpper().Contains(termoNorm) && v.Produto.Ativo)
                .Select(v => new
                {
                    v.Id, v.ProdutoId, v.CodigoBarras,
                    Nome = v.Produto.Nome, Codigo = v.Produto.Codigo,
                    Fabricante = v.Produto.Fabricante != null ? v.Produto.Fabricante.Nome : "",
                    v.Produto.PermitirConferenciaDigitando, v.Produto.ClasseTerapeutica,
                    v.Produto.PrecoFp, v.Produto.PrecoFpBolsaFamilia, v.Produto.ParticipaFarmaciaPopular,
                    Labels = v.Valores.OrderBy(x => x.AtributoVariacao.Ordem).Select(x => x.ValorAtributo.Valor).ToList()
                })
                .Take(limit)
                .ToListAsync();

            var data = new List<object>();
            if (variacoes.Count > 0)
            {
                var varIds = variacoes.Select(v => v.Id).ToList();
                var prodIdsVar = variacoes.Select(v => v.ProdutoId).Distinct().ToList();
                var dadosVar = await _db.ProdutosDados
                    .Where(d => d.FilialId == filialId && (
                        (d.ProdutoVariacaoId != null && varIds.Contains(d.ProdutoVariacaoId.Value)) ||
                        (d.ProdutoVariacaoId == null && prodIdsVar.Contains(d.ProdutoId))))
                    .Select(d => new { d.ProdutoId, d.ProdutoVariacaoId, d.ValorVenda, d.CustoMedio, d.EstoqueAtual, d.CurvaAbc })
                    .ToListAsync();

                foreach (var v in variacoes)
                {
                    var dSku = dadosVar.FirstOrDefault(x => x.ProdutoVariacaoId == v.Id);
                    var dBase = dadosVar.FirstOrDefault(x => x.ProdutoId == v.ProdutoId && x.ProdutoVariacaoId == null);
                    var preco = (dSku?.ValorVenda ?? 0) > 0 ? dSku!.ValorVenda : (dBase?.ValorVenda ?? 0);
                    data.Add(new
                    {
                        id = v.ProdutoId, codigo = v.Codigo, nome = v.Nome, fabricante = v.Fabricante,
                        valorVenda = preco, custoMedio = dSku?.CustoMedio ?? 0,
                        estoqueAtual = dSku?.EstoqueAtual ?? 0, curvaAbc = dSku?.CurvaAbc ?? "",
                        temPromocao = false,
                        permitirConferenciaDigitando = v.PermitirConferenciaDigitando,
                        classeTerapeutica = v.ClasseTerapeutica,
                        precoFp = v.PrecoFp, precoFpBolsaFamilia = v.PrecoFpBolsaFamilia,
                        participaFarmaciaPopular = v.ParticipaFarmaciaPopular,
                        codigoBarras = v.CodigoBarras,
                        controlaGrade = true,
                        produtoVariacaoId = (long?)v.Id,
                        variacaoDescricao = string.Join(" / ", v.Labels)
                    });
                }
            }
            data.AddRange(result.Cast<object>());

            return Ok(new { success = true, data });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ProdutosController.Buscar"); return StatusCode(500, new { success = false, message = "Erro ao buscar produtos." }); }
    }

    /// <summary>Variações (SKUs) de um produto com grade, com preço/estoque resolvidos
    /// pra filial — usado no PDV pra escolher qual variação está sendo vendida.</summary>
    [HttpGet("{id:long}/variacoes-venda")]
    public async Task<IActionResult> VariacoesVenda(long id, [FromQuery] long filialId = 1)
    {
        try
        {
            var variacoes = await _db.ProdutosVariacoes
                .Where(v => v.ProdutoId == id && v.Ativo)
                .Select(v => new
                {
                    v.Id, v.CodigoBarras,
                    Labels = v.Valores.OrderBy(x => x.AtributoVariacao.Ordem).Select(x => x.ValorAtributo.Valor).ToList()
                })
                .ToListAsync();

            var varIds = variacoes.Select(v => v.Id).ToList();
            var dados = await _db.ProdutosDados
                .Where(d => d.FilialId == filialId && (
                    (d.ProdutoVariacaoId != null && varIds.Contains(d.ProdutoVariacaoId.Value)) ||
                    (d.ProdutoVariacaoId == null && d.ProdutoId == id)))
                .Select(d => new { d.ProdutoVariacaoId, d.ValorVenda, d.EstoqueAtual })
                .ToListAsync();
            var precoBase = dados.FirstOrDefault(x => x.ProdutoVariacaoId == null)?.ValorVenda ?? 0;

            var lista = variacoes.Select(v =>
            {
                var dSku = dados.FirstOrDefault(x => x.ProdutoVariacaoId == v.Id);
                var preco = (dSku?.ValorVenda ?? 0) > 0 ? dSku!.ValorVenda : precoBase;
                return new
                {
                    produtoVariacaoId = v.Id,
                    descricao = string.Join(" / ", v.Labels),
                    codigoBarras = v.CodigoBarras,
                    valorVenda = preco,
                    estoqueAtual = dSku?.EstoqueAtual ?? 0
                };
            }).ToList();

            return Ok(new { success = true, data = lista });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ProdutosController.VariacoesVenda"); return StatusCode(500, new { success = false, message = "Erro ao buscar variações." }); }
    }

    /// <summary>
    /// Tenta interpretar o termo como código de barras de balança (produto pesável):
    /// prefixo + PLU + valor(peso/preço) + verificador. Se casar, resolve o produto pelo
    /// PLU e devolve o item com a quantidade (peso) e o preço já calculados. Formato via config.
    /// Retorna null se não for um código de balança válido / produto não encontrado.
    /// </summary>
    private async Task<object?> TryResolverBalancaAsync(string termo, long filialId)
    {
        if (!termo.All(char.IsDigit)) return null;

        var cfg = await _db.Configuracoes
            .Where(c => c.Chave.StartsWith("balanca.barcode."))
            .ToDictionaryAsync(c => c.Chave, c => c.Valor);
        string Get(string k, string def) => cfg.TryGetValue("balanca.barcode." + k, out var v) && !string.IsNullOrWhiteSpace(v) ? v : def;

        var prefixo = Get("prefixo", "2");
        if (!int.TryParse(Get("tam_codigo", "6"), out var tamCodigo)) tamCodigo = 6;
        if (!int.TryParse(Get("tam_valor", "5"), out var tamValor)) tamValor = 5;
        var tipoValor = Get("tipo_valor", "peso").ToLower();

        // EAN-13: prefixo + PLU + valor + 1 dígito verificador.
        var totalLen = prefixo.Length + tamCodigo + tamValor + 1;
        if (string.IsNullOrEmpty(prefixo) || termo.Length != totalLen || !termo.StartsWith(prefixo)) return null;

        if (!int.TryParse(termo.Substring(prefixo.Length, tamCodigo), out var plu)) return null;
        if (!int.TryParse(termo.Substring(prefixo.Length + tamCodigo, tamValor), out var valorNum)) return null;

        var produto = await _db.Produtos
            .Where(p => p.Ativo && p.Pesavel && p.CodigoBalanca == plu)
            .Select(p => new { p.Id, p.Codigo, p.Nome, p.Unidade, p.CodigoBarras,
                fabricante = p.Fabricante != null ? p.Fabricante.Nome : "" })
            .FirstOrDefaultAsync();
        if (produto == null) return null;

        var dados = await _db.ProdutosDados
            .Where(d => d.ProdutoId == produto.Id && d.FilialId == filialId && d.ProdutoVariacaoId == null)
            .Select(d => new { d.ValorVenda, d.EstoqueAtual, d.CurvaAbc })
            .FirstOrDefaultAsync();
        var precoKg = dados?.ValorVenda ?? 0;

        decimal quantidade;
        if (tipoValor == "preco")
        {
            var precoItem = valorNum / 100m;                    // centavos → reais
            quantidade = precoKg > 0 ? Math.Round(precoItem / precoKg, 3) : 0;
        }
        else
        {
            quantidade = Math.Round(valorNum / 1000m, 3);       // gramas → kg
        }

        return new
        {
            id = produto.Id, codigo = produto.Codigo, nome = produto.Nome, fabricante = produto.fabricante,
            valorVenda = precoKg, custoMedio = 0m,
            estoqueAtual = dados?.EstoqueAtual ?? 0, curvaAbc = dados?.CurvaAbc ?? "",
            temPromocao = false, permitirConferenciaDigitando = false,
            classeTerapeutica = (string?)null, precoFp = (decimal?)null, precoFpBolsaFamilia = (decimal?)null,
            participaFarmaciaPopular = false, codigoBarras = produto.CodigoBarras,
            controlaGrade = false, produtoVariacaoId = (long?)null, variacaoDescricao = (string?)null,
            // Pesável: o PDV usa a quantidade (peso) já resolvida.
            pesavel = true, unidade = produto.Unidade, quantidadeBalanca = quantidade
        };
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca = null)
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync(busca) }); }
        catch (Exception ex) { return await ErroInterno(ex, "Listar"); }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id)
    {
        try { return Ok(new { success = true, data = await _service.ObterAsync(id) }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Não encontrado." }); }
        catch (Exception ex) { return await ErroInterno(ex, "Obter", id); }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] ProdutoFormDto dto)
    {
        try { return Created("", new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { return await ErroInterno(ex, "Criar"); }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] ProdutoFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Não encontrado." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { return await ErroInterno(ex, "Atualizar", id); }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Não encontrado." }); }
        catch (Exception ex) { return await ErroInterno(ex, "Excluir", id); }
    }

    [HttpGet("{id:long}/log")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("Produto", id, dataInicio, dataFim) }); }
        catch (Exception ex) { return await ErroInterno(ex, "ObterLog", id); }
    }

    /// <summary>
    /// Loga erro técnico na tabela LogsErro e retorna mensagem amigável ao usuário.
    /// </summary>
    private async Task<IActionResult> ErroInterno(Exception ex, string funcao, long? registroId = null)
    {
        Log.Error(ex, "Erro em Produtos.{Funcao} Id={Id}", funcao, registroId);

        try
        {
            _db.AplicandoSync = true;
            _db.LogsErro.Add(new LogErro
            {
                Tela = "Produtos",
                Funcao = funcao,
                Mensagem = ex.InnerException?.Message ?? ex.Message,
                StackTrace = ex.StackTrace,
                DadosAdicionais = registroId.HasValue ? $"RegistroId={registroId}" : null,
                UsuarioLogin = User.Identity?.Name
            });
            await _db.SaveChangesAsync();
            _db.AplicandoSync = false;
        }
        catch { /* silenciar erro ao gravar o próprio erro */ }

        return StatusCode(500, new
        {
            success = false,
            message = "Ocorreu um erro inesperado. A ação não foi concluída. Tente novamente. Se o erro persistir, acione o suporte técnico."
        });
    }
}

// ── ProdutoLocal Controller ─────────────────────────────────────────

[Authorize]
[ApiController]
[Route("api/produto-locais")]
public class ProdutoLocaisController : ControllerBase
{
    private readonly IProdutoLocalService _service;

    public ProdutoLocaisController(IProdutoLocalService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao listar ProdutoLocais"); return StatusCode(500, new { success = false, message = "Erro ao listar." }); }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] ProdutoLocalFormDto dto)
    {
        try { return Created("", new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao criar ProdutoLocal"); return StatusCode(500, new { success = false, message = "Erro ao criar." }); }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] ProdutoLocalFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao atualizar ProdutoLocal {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao atualizar." }); }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao excluir ProdutoLocal {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir." }); }
    }
}
