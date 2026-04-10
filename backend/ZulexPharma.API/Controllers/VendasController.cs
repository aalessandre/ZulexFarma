using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Vendas;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class VendasController : ControllerBase
{
    private readonly IVendaService _service;

    public VendasController(IVendaService service) { _service = service; }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] long? filialId = null, [FromQuery] string? status = null)
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync(filialId, status) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em VendasController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar vendas." }); }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id)
    {
        try
        {
            var dto = await _service.ObterAsync(id);
            if (dto == null) return NotFound(new { success = false, message = "Venda não encontrada." });
            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em VendasController.Obter"); return StatusCode(500, new { success = false, message = "Erro ao obter venda." }); }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] VendaFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em VendasController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar venda." }); }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] VendaFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Venda não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em VendasController.Atualizar"); return StatusCode(500, new { success = false, message = "Erro ao atualizar venda." }); }
    }

    [HttpPost("{id:long}/finalizar")]
    public async Task<IActionResult> Finalizar(long id)
    {
        try { await _service.FinalizarAsync(id); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Venda não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em VendasController.Finalizar"); return StatusCode(500, new { success = false, message = "Erro ao finalizar." }); }
    }

    [HttpPost("{id:long}/cancelar")]
    public async Task<IActionResult> Cancelar(long id)
    {
        try { await _service.CancelarAsync(id); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Venda não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em VendasController.Cancelar"); return StatusCode(500, new { success = false, message = "Erro ao cancelar." }); }
    }
}
