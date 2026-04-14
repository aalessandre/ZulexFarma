using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Security.Claims;
using ZulexPharma.API.Filters;
using ZulexPharma.Application.DTOs.GestorTributario;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/gestor-tributario")]
public class GestorTributarioController : ControllerBase
{
    private readonly IGestorTributarioService _service;
    public GestorTributarioController(IGestorTributarioService s) { _service = s; }

    private long? UsuarioId =>
        long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    [HttpGet("status")]
    [Permissao("gestor_tributario", "c")]
    public async Task<IActionResult> Status()
    {
        try { return Ok(new { success = true, data = await _service.ObterStatusAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro GT.Status"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpGet("consultar-ean")]
    [Permissao("gestor_tributario", "c")]
    public async Task<IActionResult> ConsultarEan([FromQuery] string ean)
    {
        try
        {
            var dto = await _service.ConsultarPorEanAsync(ean);
            return Ok(new { success = true, data = dto });
        }
        catch (InvalidOperationException ex)
        {
            // Erros de configuração / negócio — retornam 200 com success=false para o front mostrar em modal
            return Ok(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro GT.ConsultarEan");
            return Ok(new { success = false, message = "Erro inesperado ao consultar Gestor Tributário: " + ex.Message });
        }
    }

    [HttpPost("produtos/{id:long}/revisar")]
    [Permissao("gestor_tributario", "a")]
    public async Task<IActionResult> RevisarProduto(long id, [FromQuery] bool forcar = false)
    {
        try
        {
            var dto = await _service.RevisarProdutoAsync(id, UsuarioId, forcar);
            if (dto == null) return Ok(new { success = false, message = "Produto não encontrado na base do Gestor Tributário." });
            return Ok(new { success = true, data = dto });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro GT.RevisarProduto"); return StatusCode(500, new { success = false, message = "Erro ao revisar produto." }); }
    }

    [HttpPost("revisar-base")]
    [Permissao("gestor_tributario", "a")]
    public async Task<IActionResult> IniciarRevisaoBase([FromBody] RevisarBaseRequest req)
    {
        try
        {
            var jobId = await _service.IniciarRevisaoBaseAsync(req, UsuarioId);
            return Ok(new { success = true, data = new IniciarJobResponse { JobId = jobId, Mensagem = "Job iniciado." } });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro GT.IniciarRevisaoBase"); return StatusCode(500, new { success = false, message = "Erro ao iniciar job." }); }
    }

    [HttpGet("jobs")]
    [Permissao("gestor_tributario", "c")]
    public async Task<IActionResult> ListarJobs([FromQuery] int limite = 50)
    {
        try { return Ok(new { success = true, data = await _service.ListarJobsAsync(limite) }); }
        catch (Exception ex) { Log.Error(ex, "Erro GT.ListarJobs"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpGet("jobs/{id:long}")]
    [Permissao("gestor_tributario", "c")]
    public async Task<IActionResult> ObterJob(long id)
    {
        try
        {
            var job = await _service.ObterJobAsync(id);
            if (job == null) return NotFound(new { success = false, message = "Job não encontrado." });
            return Ok(new { success = true, data = job });
        }
        catch (Exception ex) { Log.Error(ex, "Erro GT.ObterJob"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpPost("jobs/{id:long}/cancelar")]
    [Permissao("gestor_tributario", "a")]
    public async Task<IActionResult> CancelarJob(long id)
    {
        try { await _service.CancelarJobAsync(id); return Ok(new { success = true }); }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }
}
