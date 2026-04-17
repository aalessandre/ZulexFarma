using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.HierarquiaComissoes;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class HierarquiaComissoesController : ControllerBase
{
    private readonly IHierarquiaComissaoService _service;
    private readonly ILogAcaoService _log;

    public HierarquiaComissoesController(IHierarquiaComissaoService service, ILogAcaoService log) { _service = service; _log = log; }

    [HttpGet]
    [Permissao("hierarquia-comissoes", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em HierarquiaComissoesController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar hierarquias." }); }
    }

    [HttpGet("{id:long}")]
    [Permissao("hierarquia-comissoes", "c")]
    public async Task<IActionResult> Obter(long id)
    {
        try
        {
            var dto = await _service.ObterAsync(id);
            if (dto == null) return NotFound(new { success = false, message = "Hierarquia não encontrada." });
            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em HierarquiaComissoesController.Obter | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao obter hierarquia." }); }
    }

    [HttpPost]
    [Permissao("hierarquia-comissoes", "i")]
    public async Task<IActionResult> Criar([FromBody] HierarquiaComissaoFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em HierarquiaComissoesController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar hierarquia." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("hierarquia-comissoes", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] HierarquiaComissaoFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Hierarquia não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em HierarquiaComissoesController.Atualizar | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao atualizar hierarquia." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("hierarquia-comissoes", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Hierarquia não encontrada." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em HierarquiaComissoesController.Excluir | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir hierarquia." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("hierarquia-comissoes", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("HierarquiaComissao", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em HierarquiaComissoesController.ObterLog | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
