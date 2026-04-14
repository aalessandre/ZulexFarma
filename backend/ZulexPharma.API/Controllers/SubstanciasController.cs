using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Substancias;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SubstanciasController : ControllerBase
{
    private readonly ISubstanciaService _service;
    private readonly ILogAcaoService _log;

    public SubstanciasController(ISubstanciaService service, ILogAcaoService log)
    {
        _service = service;
        _log = log;
    }

    [HttpGet]
    [Permissao("substancias", "c")]
    public async Task<IActionResult> Listar()
    {
        try
        {
            return Ok(new { success = true, data = await _service.ListarAsync() });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SubstanciasController.Listar");
            return StatusCode(500, new { success = false, message = "Erro ao listar substâncias." });
        }
    }

    [HttpPost]
    [Permissao("substancias", "i")]
    public async Task<IActionResult> Criar([FromBody] SubstanciaFormDto dto)
    {
        try
        {
            return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SubstanciasController.Criar");
            return StatusCode(500, new { success = false, message = "Erro ao criar substância." });
        }
    }

    [HttpPut("{id:long}")]
    [Permissao("substancias", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] SubstanciaFormDto dto)
    {
        try
        {
            await _service.AtualizarAsync(id, dto);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Substância não encontrada." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SubstanciasController.Atualizar | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao atualizar substância." });
        }
    }

    [HttpDelete("{id:long}")]
    [Permissao("substancias", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try
        {
            var r = await _service.ExcluirAsync(id);
            return Ok(new { success = true, resultado = r });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Substância não encontrada." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SubstanciasController.Excluir | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao excluir substância." });
        }
    }

    [HttpPost("importar-planilha")]
    [Permissao("substancias", "i")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> ImportarPlanilha([FromForm] IFormFile arquivo, [FromForm] bool limparAntes = false)
    {
        try
        {
            if (arquivo == null || arquivo.Length == 0)
                return BadRequest(new { success = false, message = "Arquivo não enviado." });

            if (!arquivo.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Apenas arquivos .xlsx são suportados." });

            using var stream = arquivo.OpenReadStream();
            var r = await _service.ImportarPlanilhaAsync(stream, limparAntes);
            return Ok(new { success = true, data = r });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SubstanciasController.ImportarPlanilha");
            return StatusCode(500, new { success = false, message = "Erro ao importar planilha." });
        }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("substancias", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try
        {
            return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("Substancia", id, dataInicio, dataFim) });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SubstanciasController.ObterLog | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." });
        }
    }
}
