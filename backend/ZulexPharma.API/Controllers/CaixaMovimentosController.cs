using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Caixa;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CaixaMovimentosController : ControllerBase
{
    private readonly ICaixaMovimentoService _service;

    public CaixaMovimentosController(ICaixaMovimentoService service) => _service = service;

    private long UsuarioId => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet("caixa/{caixaId:long}")]
    public async Task<IActionResult> ListarPorCaixa(long caixaId)
    {
        try { return Ok(new { success = true, data = await _service.ListarPorCaixaAsync(caixaId) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em CaixaMovimentosController.ListarPorCaixa"); return StatusCode(500, new { success = false, message = "Erro ao listar movimentos." }); }
    }

    [HttpGet("venda/{vendaId:long}")]
    public async Task<IActionResult> ListarPorVenda(long vendaId)
    {
        try { return Ok(new { success = true, data = await _service.ListarPorVendaAsync(vendaId) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em CaixaMovimentosController.ListarPorVenda"); return StatusCode(500, new { success = false, message = "Erro ao listar movimentos." }); }
    }

    [HttpGet("sangrias-pendentes")]
    public async Task<IActionResult> SangriasPendentes([FromQuery] long filialId)
    {
        try { return Ok(new { success = true, data = await _service.ListarSangriasPendentesAsync(filialId) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em CaixaMovimentosController.SangriasPendentes"); return StatusCode(500, new { success = false, message = "Erro ao listar sangrias pendentes." }); }
    }

    [HttpPost("sangria")]
    public async Task<IActionResult> Sangria([FromBody] SangriaFormDto dto)
    {
        try { var id = await _service.CriarSangriaAsync(dto, UsuarioId); return Ok(new { success = true, data = new { id } }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em Sangria"); return StatusCode(500, new { success = false, message = "Erro ao registrar sangria." }); }
    }

    [HttpPost("suprimento")]
    public async Task<IActionResult> Suprimento([FromBody] SuprimentoFormDto dto)
    {
        try { var id = await _service.CriarSuprimentoAsync(dto, UsuarioId); return Ok(new { success = true, data = new { id } }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em Suprimento"); return StatusCode(500, new { success = false, message = "Erro ao registrar suprimento." }); }
    }

    [HttpPost("recebimento")]
    public async Task<IActionResult> Recebimento([FromBody] RecebimentoFormDto dto)
    {
        try { var id = await _service.CriarRecebimentoAsync(dto, UsuarioId); return Ok(new { success = true, data = new { id } }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em Recebimento"); return StatusCode(500, new { success = false, message = "Erro ao registrar recebimento." }); }
    }

    [HttpPost("pagamento")]
    public async Task<IActionResult> Pagamento([FromBody] PagamentoFormDto dto)
    {
        try { var id = await _service.CriarPagamentoAsync(dto, UsuarioId); return Ok(new { success = true, data = new { id } }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em Pagamento"); return StatusCode(500, new { success = false, message = "Erro ao registrar pagamento." }); }
    }

    [HttpPost("bipar")]
    public async Task<IActionResult> Bipar([FromBody] BiparCanhotoFormDto dto)
    {
        try
        {
            var mov = await _service.BiparCanhotoAsync(dto.Codigo, UsuarioId);
            return Ok(new { success = true, data = mov });
        }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em Bipar"); return StatusCode(500, new { success = false, message = "Erro ao bipar canhoto." }); }
    }

    [HttpPost("{id:long}/confirmar-sangria")]
    public async Task<IActionResult> ConfirmarSangria(long id)
    {
        try { await _service.ConfirmarSangriaConferenteAsync(id, UsuarioId); return Ok(new { success = true }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ConfirmarSangria"); return StatusCode(500, new { success = false, message = "Erro ao confirmar sangria." }); }
    }

    [HttpGet("{id:long}/canhoto")]
    public async Task<IActionResult> Canhoto(long id)
    {
        try
        {
            var html = await _service.GerarCanhotoHtmlAsync(id);
            return Content(html, "text/html; charset=utf-8");
        }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Movimento não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em Canhoto"); return StatusCode(500, new { success = false, message = "Erro ao gerar canhoto." }); }
    }
}
