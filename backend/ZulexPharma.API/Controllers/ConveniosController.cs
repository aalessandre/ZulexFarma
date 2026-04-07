using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Convenios;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ConveniosController : ControllerBase
{
    private readonly IConvenioService _service;
    private readonly ILogAcaoService _log;

    public ConveniosController(IConvenioService service, ILogAcaoService log) { _service = service; _log = log; }

    [HttpGet]
    [Permissao("convenios", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ConveniosController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar convênios." }); }
    }

    [HttpGet("{id:long}")]
    [Permissao("convenios", "c")]
    public async Task<IActionResult> Obter(long id)
    {
        try
        {
            var dto = await _service.ObterAsync(id);
            if (dto == null) return NotFound(new { success = false, message = "Convênio não encontrado." });
            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ConveniosController.Obter | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao obter convênio." }); }
    }

    [HttpPost]
    [Permissao("convenios", "i")]
    public async Task<IActionResult> Criar([FromBody] ConvenioFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ConveniosController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar convênio." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("convenios", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] ConvenioFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Convênio não encontrado." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ConveniosController.Atualizar | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao atualizar convênio." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("convenios", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Convênio não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ConveniosController.Excluir | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir convênio." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("convenios", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("Convenio", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ConveniosController.ObterLog | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
