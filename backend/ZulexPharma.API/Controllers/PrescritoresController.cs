using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Prescritores;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PrescritoresController : ControllerBase
{
    private readonly IPrescritorService _service;
    private readonly ILogAcaoService _log;

    public PrescritoresController(IPrescritorService service, ILogAcaoService log) { _service = service; _log = log; }

    [HttpGet]
    [Permissao("prescritores", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PrescritoresController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar prescritores." }); }
    }

    [HttpPost]
    [Permissao("prescritores", "i")]
    public async Task<IActionResult> Criar([FromBody] PrescritorFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PrescritoresController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar prescritor." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("prescritores", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] PrescritorFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Prescritor não encontrado." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PrescritoresController.Atualizar | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao atualizar prescritor." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("prescritores", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Prescritor não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PrescritoresController.Excluir | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir prescritor." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("prescritores", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("Prescritor", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PrescritoresController.ObterLog | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
