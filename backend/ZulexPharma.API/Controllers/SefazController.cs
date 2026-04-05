using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Fiscal;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/sefaz")]
public class SefazController : ControllerBase
{
    private readonly ISefazService _service;

    public SefazController(ISefazService service)
    {
        _service = service;
    }

    [HttpGet("certificado/{filialId:long}")]
    public async Task<IActionResult> ObterCertificado(long filialId)
    {
        try { return Ok(new { success = true, data = await _service.ObterCertificadoAsync(filialId) }); }
        catch (Exception ex) { Log.Error(ex, "Erro ObterCertificado"); return StatusCode(500, new { success = false, message = "Erro." }); }
    }

    [HttpPost("certificado/upload")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadCertificado([FromBody] CertificadoUploadRequest request)
    {
        try { return Ok(new { success = true, data = await _service.UploadCertificadoAsync(request) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro UploadCertificado"); return StatusCode(500, new { success = false, message = "Erro ao processar certificado." }); }
    }

    [HttpPost("consultar-chave")]
    public async Task<IActionResult> ConsultarChave([FromBody] ConsultarChaveRequest request)
    {
        try { return Ok(new { success = true, data = await _service.ConsultarPorChaveAsync(request.FilialId, request.ChaveNfe) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro ConsultarChave"); return StatusCode(500, new { success = false, message = "Erro ao consultar SEFAZ." }); }
    }

    [HttpPost("consultar-nfe")]
    public async Task<IActionResult> ConsultarNfe([FromBody] ConsultaSefazRequest request)
    {
        try { return Ok(new { success = true, data = await _service.ConsultarNfePendentesAsync(request.FilialId) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro ConsultarNfe"); return StatusCode(500, new { success = false, message = "Erro ao consultar SEFAZ." }); }
    }
}
