using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class IbptController : ControllerBase
{
    private readonly IbptService _service;

    public IbptController(IbptService service) => _service = service;

    /// <summary>Status da tabela IBPTax importada.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var status = await _service.ObterStatusAsync();
        return Ok(new { success = true, data = status });
    }

    /// <summary>Sincroniza via API do IBPT (manual).</summary>
    [HttpPost("sincronizar")]
    public async Task<IActionResult> Sincronizar()
    {
        try
        {
            if (IbptBackgroundService.Sincronizando)
                return BadRequest(new { success = false, message = "Sincronização já em andamento. Aguarde." });

            var result = await _service.SincronizarViaApiAsync();
            return Ok(new
            {
                success = true,
                data = result,
                message = $"IBPTax sincronizado: {result.TotalSincronizado}/{result.TotalNcms} NCMs (versão {result.Versao}).{(result.Erros > 0 ? $" {result.Erros} erro(s)." : "")}"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao sincronizar IBPTax via API");
            return StatusCode(500, new { success = false, message = $"Erro ao sincronizar: {ex.Message}" });
        }
    }

    /// <summary>Upload do CSV do IBPTax (fallback).</summary>
    [HttpPost("importar")]
    public async Task<IActionResult> Importar([FromForm] IFormFile arquivo, [FromForm] string uf = "PR")
    {
        try
        {
            if (arquivo == null || arquivo.Length == 0)
                return BadRequest(new { success = false, message = "Arquivo CSV é obrigatório." });

            using var reader = new StreamReader(arquivo.OpenReadStream());
            var csv = await reader.ReadToEndAsync();

            var result = await _service.ImportarCsvAsync(csv, uf);
            return Ok(new
            {
                success = true,
                data = result,
                message = $"IBPTax importado: {result.TotalImportado} registros (versão {result.Versao})."
            });
        }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao importar IBPTax");
            return StatusCode(500, new { success = false, message = $"Erro ao importar: {ex.Message}" });
        }
    }

    /// <summary>Status em tempo real do background service.</summary>
    [HttpGet("vigencia")]
    public IActionResult Vigencia()
    {
        return Ok(new
        {
            success = true,
            data = new
            {
                tabelaExpirada = IbptBackgroundService.TabelaExpirada,
                vigenciaFim = IbptBackgroundService.VigenciaFim,
                ultimaVerificacao = IbptBackgroundService.UltimaVerificacao,
                ultimaSincronizacao = IbptBackgroundService.UltimaSincronizacao,
                versaoAtual = IbptBackgroundService.VersaoAtual,
                totalRegistros = IbptBackgroundService.TotalRegistros,
                sincronizando = IbptBackgroundService.Sincronizando
            }
        });
    }
}
