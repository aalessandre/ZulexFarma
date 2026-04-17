using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.API.Filters;
using ZulexPharma.Application.DTOs.Fidelidade;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Enums;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/fidelidade/campanhas")]
public class CampanhasFidelidadeController : ControllerBase
{
    private readonly ICampanhaFidelidadeService _service;
    private readonly ILogAcaoService _log;
    public CampanhasFidelidadeController(ICampanhaFidelidadeService service, ILogAcaoService log) { _service = service; _log = log; }

    [HttpGet]
    [Permissao("fidelidade", "c")]
    public async Task<IActionResult> Listar([FromQuery] TipoFidelidade? tipo = null)
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync(tipo) }); }
        catch (Exception ex) { Log.Error(ex, "Erro CampanhasFidelidade.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar." }); }
    }

    [HttpGet("{id:long}")]
    [Permissao("fidelidade", "c")]
    public async Task<IActionResult> Obter(long id)
    {
        try
        {
            var c = await _service.ObterAsync(id);
            if (c == null) return NotFound(new { success = false, message = "Campanha não encontrada." });
            return Ok(new { success = true, data = c });
        }
        catch (Exception ex) { Log.Error(ex, "Erro CampanhasFidelidade.Obter"); return StatusCode(500, new { success = false, message = "Erro ao obter." }); }
    }

    [HttpPost]
    [Permissao("fidelidade", "i")]
    public async Task<IActionResult> Criar([FromBody] CampanhaFidelidadeFormDto dto)
    {
        try { return Created("", new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro CampanhasFidelidade.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("fidelidade", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] CampanhaFidelidadeFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Campanha não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro CampanhasFidelidade.Atualizar"); return StatusCode(500, new { success = false, message = "Erro ao atualizar." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("fidelidade", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Campanha não encontrada." }); }
        catch (Exception ex) { Log.Error(ex, "Erro CampanhasFidelidade.Excluir"); return StatusCode(500, new { success = false, message = "Erro ao excluir." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("fidelidade", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("CampanhaFidelidade", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro CampanhasFidelidade.ObterLog"); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
