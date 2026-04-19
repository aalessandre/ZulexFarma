using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Entregas;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/entrega-faixas")]
public class EntregaFaixasController : ControllerBase
{
    private readonly IEntregaFaixaService _service;

    public EntregaFaixasController(IEntregaFaixaService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] long filialId)
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync(filialId) }); }
        catch (Exception ex) { Log.Error(ex, "Erro EntregaFaixas.Listar"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] EntregaFaixaFormDto dto)
    {
        try { return Created("", new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException e) { return BadRequest(new { success = false, message = e.Message }); }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] EntregaFaixaFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
        catch (ArgumentException e) { return BadRequest(new { success = false, message = e.Message }); }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { await _service.ExcluirAsync(id); return Ok(new { success = true }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
    }
}
