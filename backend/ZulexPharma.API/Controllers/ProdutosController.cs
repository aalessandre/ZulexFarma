using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Produtos;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
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
