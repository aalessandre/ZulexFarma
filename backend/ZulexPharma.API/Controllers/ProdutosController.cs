using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Produtos;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _service;
    private readonly ILogAcaoService _log;

    public ProdutosController(IProdutoService service, ILogAcaoService log)
    {
        _service = service;
        _log = log;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca = null)
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync(busca) }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao listar Produtos"); return StatusCode(500, new { success = false, message = "Erro ao listar." }); }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id)
    {
        try { return Ok(new { success = true, data = await _service.ObterAsync(id) }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao obter Produto {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao obter." }); }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] ProdutoFormDto dto)
    {
        try { return Created("", new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao criar Produto"); return StatusCode(500, new { success = false, message = ex.InnerException?.Message ?? ex.Message }); }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] ProdutoFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Não encontrado." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao atualizar Produto {Id}", id); return StatusCode(500, new { success = false, message = ex.InnerException?.Message ?? ex.Message }); }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao excluir Produto {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir." }); }
    }

    [HttpGet("{id:long}/log")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("Produto", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro ao buscar log Produto {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
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
