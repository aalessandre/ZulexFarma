using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.API.Filters;
using ZulexPharma.Application.DTOs.SelfCheckout;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/configuracoes/self-checkout")]
public class ConfiguracoesSelfCheckoutController : ControllerBase
{
    private readonly ISelfCheckoutConfiguracaoService _service;

    public ConfiguracoesSelfCheckoutController(ISelfCheckoutConfiguracaoService service)
    {
        _service = service;
    }

    [HttpGet("{filialId:long}")]
    [Permissao("self-checkout", "c")]
    public async Task<IActionResult> Obter(long filialId, CancellationToken ct)
    {
        try
        {
            var cfg = await _service.ObterPorFilialAsync(filialId, ct);
            return Ok(new { success = true, data = cfg });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ConfiguracoesSelfCheckoutController.Obter filialId={FilialId}", filialId);
            return StatusCode(500, new { success = false, message = "Erro ao obter configuração." });
        }
    }

    [HttpPut("{filialId:long}")]
    [Permissao("self-checkout", "a")]
    public async Task<IActionResult> Salvar(long filialId, [FromBody] SelfCheckoutConfiguracaoFormDto form, CancellationToken ct)
    {
        try
        {
            var cfg = await _service.SalvarAsync(filialId, form, ct);
            return Ok(new { success = true, data = cfg });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ConfiguracoesSelfCheckoutController.Salvar filialId={FilialId}", filialId);
            return StatusCode(500, new { success = false, message = "Erro ao salvar configuração." });
        }
    }

    [HttpGet("{filialId:long}/terminais")]
    [Permissao("self-checkout", "c")]
    public async Task<IActionResult> ListarTerminais(long filialId, CancellationToken ct)
    {
        try
        {
            var lista = await _service.ListarTerminaisAsync(filialId, ct);
            return Ok(new { success = true, data = lista });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ConfiguracoesSelfCheckoutController.ListarTerminais filialId={FilialId}", filialId);
            return StatusCode(500, new { success = false, message = "Erro ao listar terminais." });
        }
    }

    [HttpPost("{filialId:long}/terminais")]
    [Permissao("self-checkout", "i")]
    public async Task<IActionResult> CriarTerminal(long filialId, [FromBody] SelfCheckoutTerminalFormDto form, CancellationToken ct)
    {
        try
        {
            var t = await _service.CriarTerminalAsync(filialId, form, ct);
            return Ok(new { success = true, data = t });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ConfiguracoesSelfCheckoutController.CriarTerminal filialId={FilialId}", filialId);
            return StatusCode(500, new { success = false, message = "Erro ao criar terminal." });
        }
    }

    [HttpPut("terminais/{terminalId:long}")]
    [Permissao("self-checkout", "a")]
    public async Task<IActionResult> AtualizarTerminal(long terminalId, [FromBody] SelfCheckoutTerminalFormDto form, CancellationToken ct)
    {
        try
        {
            var t = await _service.AtualizarTerminalAsync(terminalId, form, ct);
            return Ok(new { success = true, data = t });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ConfiguracoesSelfCheckoutController.AtualizarTerminal terminalId={TerminalId}", terminalId);
            return StatusCode(500, new { success = false, message = "Erro ao atualizar terminal." });
        }
    }

    [HttpDelete("terminais/{terminalId:long}")]
    [Permissao("self-checkout", "e")]
    public async Task<IActionResult> RemoverTerminal(long terminalId, CancellationToken ct)
    {
        try
        {
            await _service.RemoverTerminalAsync(terminalId, ct);
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ConfiguracoesSelfCheckoutController.RemoverTerminal terminalId={TerminalId}", terminalId);
            return StatusCode(500, new { success = false, message = "Erro ao remover terminal." });
        }
    }
}
