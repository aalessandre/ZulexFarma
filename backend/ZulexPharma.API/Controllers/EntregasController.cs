using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Security.Claims;
using ZulexPharma.Application.DTOs.Entregas;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Enums;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/entregas")]
public class EntregasController : ControllerBase
{
    private readonly IEntregaService _service;

    public EntregasController(IEntregaService service) => _service = service;

    private long? UsuarioId => long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] long? filialId = null,
        [FromQuery] StatusEntrega? status = null,
        [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync(filialId, status, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro Entregas.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar entregas." }); }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id)
    {
        try { return Ok(new { success = true, data = await _service.ObterAsync(id) }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
    }

    [HttpGet("calcular")]
    public async Task<IActionResult> Calcular([FromQuery] long filialId, [FromQuery] long enderecoId)
    {
        try { return Ok(new { success = true, data = await _service.CalcularAsync(filialId, enderecoId) }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { success = false, message = e.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro Entregas.Calcular"); return StatusCode(500, new { success = false, message = "Erro ao calcular entrega." }); }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] EntregaFormDto dto)
    {
        try { return Created("", new { success = true, data = await _service.CriarAsync(dto, UsuarioId) }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
        catch (ArgumentException e) { return BadRequest(new { success = false, message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { success = false, message = e.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro Entregas.Criar"); return StatusCode(500, new { success = false, message = ex.Message }); }
    }

    [HttpPost("{id:long}/atribuir-entregador")]
    public async Task<IActionResult> AtribuirEntregador(long id, [FromBody] EntregaAtribuirEntregadorDto dto)
    {
        try { await _service.AtribuirEntregadorAsync(id, dto.EntregadorId, UsuarioId); return Ok(new { success = true }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { success = false, message = e.Message }); }
    }

    [HttpPost("{id:long}/status")]
    public async Task<IActionResult> MudarStatus(long id, [FromBody] EntregaMudarStatusDto dto)
    {
        try { await _service.MudarStatusAsync(id, dto.NovoStatus, UsuarioId, dto.Observacao); return Ok(new { success = true }); }
        catch (KeyNotFoundException e) { return NotFound(new { success = false, message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { success = false, message = e.Message }); }
    }

    /// <summary>Endpoint público do cliente — sem autenticação, identificado pelo token.</summary>
    [AllowAnonymous]
    [HttpGet("/api/rastreio/{token:guid}")]
    public async Task<IActionResult> Rastreio(Guid token)
    {
        try { return Ok(new { success = true, data = await _service.ObterPorTokenAsync(token) }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Entrega não encontrada." }); }
    }
}
