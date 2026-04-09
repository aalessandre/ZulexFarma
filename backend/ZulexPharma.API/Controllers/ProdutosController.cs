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
                .Select(p => new { p.Id, p.Codigo, p.Nome, fabricante = p.Fabricante != null ? p.Fabricante.Nome : "" })
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

            var termoNorm = termo.Trim().ToUpper();

            var produtos = await _db.Produtos
                .Where(p => p.Ativo && (
                    p.Nome.ToUpper().Contains(termoNorm) ||
                    p.Codigo!.Contains(termoNorm) ||
                    p.Barras.Any(b => b.Barras.Contains(termoNorm))
                ))
                .OrderBy(p => p.Nome)
                .Take(limit)
                .Select(p => new { p.Id, p.Codigo, p.Nome, fabricante = p.Fabricante != null ? p.Fabricante.Nome : "" })
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
                    temPromocao = promoSet.Contains(p.Id)
                };
            });

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ProdutosController.Buscar"); return StatusCode(500, new { success = false, message = "Erro ao buscar produtos." }); }
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
