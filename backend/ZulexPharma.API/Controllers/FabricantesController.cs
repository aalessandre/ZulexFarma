using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Fabricantes;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FabricantesController : ControllerBase
{
    private readonly IFabricanteService _service;
    private readonly ILogAcaoService _log;

    public FabricantesController(IFabricanteService service, ILogAcaoService log) { _service = service; _log = log; }

    [HttpGet]
    [Permissao("fabricantes", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em FabricantesController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar fabricantes." }); }
    }

    [HttpPost]
    [Permissao("fabricantes", "i")]
    public async Task<IActionResult> Criar([FromBody] FabricanteFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em FabricantesController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar fabricante." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("fabricantes", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] FabricanteFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Fabricante não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em FabricantesController.Atualizar | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao atualizar fabricante." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("fabricantes", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Fabricante não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em FabricantesController.Excluir | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir fabricante." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("fabricantes", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("Fabricante", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em FabricantesController.ObterLog | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
