using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.PreVendas;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PreVendasController : ControllerBase
{
    private readonly IPreVendaService _service;

    public PreVendasController(IPreVendaService service) { _service = service; }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] long? filialId = null)
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync(filialId) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PreVendasController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar pré-vendas." }); }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id)
    {
        try
        {
            var dto = await _service.ObterAsync(id);
            if (dto == null) return NotFound(new { success = false, message = "Pré-venda não encontrada." });
            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em PreVendasController.Obter"); return StatusCode(500, new { success = false, message = "Erro ao obter pré-venda." }); }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] PreVendaFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PreVendasController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar pré-venda." }); }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] PreVendaFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Pré-venda não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PreVendasController.Atualizar"); return StatusCode(500, new { success = false, message = "Erro ao atualizar pré-venda." }); }
    }

    [HttpPost("{id:long}/finalizar")]
    public async Task<IActionResult> Finalizar(long id)
    {
        try { await _service.FinalizarAsync(id); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Pré-venda não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PreVendasController.Finalizar"); return StatusCode(500, new { success = false, message = "Erro ao finalizar." }); }
    }

    [HttpPost("{id:long}/cancelar")]
    public async Task<IActionResult> Cancelar(long id)
    {
        try { await _service.CancelarAsync(id); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Pré-venda não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PreVendasController.Cancelar"); return StatusCode(500, new { success = false, message = "Erro ao cancelar." }); }
    }
}
