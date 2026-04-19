using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Entregas;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/entregas-config")]
public class EntregasConfigController : ControllerBase
{
    private readonly IEntregaPerfilService _perfis;
    private readonly IEntregaAgendaService _agenda;

    public EntregasConfigController(IEntregaPerfilService perfis, IEntregaAgendaService agenda)
    {
        _perfis = perfis;
        _agenda = agenda;
    }

    // ── Perfis (+faixas aninhadas) ────────────────────────────────────

    [HttpGet("perfis")]
    [Permissao("entregas-config", "c")]
    public async Task<IActionResult> ListarPerfis([FromQuery] long filialId)
    {
        try { return Ok(new { success = true, data = await _perfis.ListarAsync(filialId) }); }
        catch (Exception ex) { Log.Error(ex, "Erro EntregasConfig.ListarPerfis"); return StatusCode(500, new { success = false, message = "Erro ao listar perfis." }); }
    }

    [HttpGet("perfis/{id:long}")]
    [Permissao("entregas-config", "c")]
    public async Task<IActionResult> ObterPerfil(long id)
    {
        try
        {
            var p = await _perfis.ObterAsync(id);
            if (p == null) return NotFound(new { success = false, message = "Perfil não encontrado." });
            return Ok(new { success = true, data = p });
        }
        catch (Exception ex) { Log.Error(ex, "Erro EntregasConfig.ObterPerfil"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpPost("perfis")]
    [Permissao("entregas-config", "i")]
    public async Task<IActionResult> CriarPerfil([FromBody] EntregaPerfilFormDto dto)
    {
        try { return Created("", new { success = true, data = await _perfis.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro EntregasConfig.CriarPerfil"); return StatusCode(500, new { success = false, message = "Erro ao criar perfil." }); }
    }

    [HttpPut("perfis/{id:long}")]
    [Permissao("entregas-config", "a")]
    public async Task<IActionResult> AtualizarPerfil(long id, [FromBody] EntregaPerfilFormDto dto)
    {
        try { await _perfis.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro EntregasConfig.AtualizarPerfil"); return StatusCode(500, new { success = false, message = "Erro ao atualizar." }); }
    }

    [HttpDelete("perfis/{id:long}")]
    [Permissao("entregas-config", "e")]
    public async Task<IActionResult> ExcluirPerfil(long id)
    {
        try { return Ok(new { success = true, resultado = await _perfis.ExcluirAsync(id) }); }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro EntregasConfig.ExcluirPerfil"); return StatusCode(500, new { success = false, message = "Erro ao excluir." }); }
    }

    // ── Agenda ─────────────────────────────────────────────────────────

    [HttpGet("agenda")]
    [Permissao("entregas-config", "c")]
    public async Task<IActionResult> ListarAgenda([FromQuery] long filialId)
    {
        try { return Ok(new { success = true, data = await _agenda.ListarAsync(filialId) }); }
        catch (Exception ex) { Log.Error(ex, "Erro EntregasConfig.ListarAgenda"); return StatusCode(500, new { success = false, message = "Erro ao listar agenda." }); }
    }

    [HttpPut("agenda")]
    [Permissao("entregas-config", "a")]
    public async Task<IActionResult> SalvarAgenda([FromBody] EntregaAgendaSaveDto dto)
    {
        try { await _agenda.SalvarAsync(dto); return Ok(new { success = true }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro EntregasConfig.SalvarAgenda"); return StatusCode(500, new { success = false, message = "Erro ao salvar agenda." }); }
    }
}
