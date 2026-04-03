using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Fiscal;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/atualizacao-precos")]
public class AtualizacaoPrecoController : ControllerBase
{
    private readonly IAtualizacaoPrecoService _service;

    public AtualizacaoPrecoController(IAtualizacaoPrecoService service)
    {
        _service = service;
    }

    [HttpGet("info-base")]
    public async Task<IActionResult> InfoBase()
    {
        try { return Ok(new { success = true, data = await _service.ObterInfoBaseAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em InfoBase"); return StatusCode(500, new { success = false, message = "Erro ao obter info." }); }
    }

    [HttpPost("upload-base")]
    [RequestSizeLimit(50_000_000)] // 50MB
    public async Task<IActionResult> UploadBase([FromBody] UploadAbcFarmaRequest request)
    {
        try { return Ok(new { success = true, data = await _service.UploadBaseAsync(request.ConteudoJson) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em UploadBase"); return StatusCode(500, new { success = false, message = "Erro ao carregar base." }); }
    }

    [HttpPost("processar")]
    public async Task<IActionResult> Processar([FromBody] ProcessarAtualizacaoRequest request)
    {
        try { return Ok(new { success = true, data = await _service.ProcessarAsync(request) }); }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em Processar"); return StatusCode(500, new { success = false, message = "Erro ao processar." }); }
    }

    [HttpGet("historico/{filialId:long}")]
    public async Task<IActionResult> Historico(long filialId)
    {
        try { return Ok(new { success = true, data = await _service.ListarHistoricoAsync(filialId) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em Historico"); return StatusCode(500, new { success = false, message = "Erro ao listar histórico." }); }
    }

    [HttpPost("reverter/{id:long}")]
    public async Task<IActionResult> Reverter(long id)
    {
        try { await _service.ReverterAsync(id); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Atualização não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em Reverter"); return StatusCode(500, new { success = false, message = "Erro ao reverter." }); }
    }
}
