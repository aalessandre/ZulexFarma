using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.API.Filters;
using ZulexPharma.Application.DTOs.Fidelidade;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/fidelidade/premios")]
public class PremiosFidelidadeController : ControllerBase
{
    private readonly IPremioFidelidadeService _service;
    private readonly ILogAcaoService _log;
    public PremiosFidelidadeController(IPremioFidelidadeService service, ILogAcaoService log) { _service = service; _log = log; }

    [HttpGet]
    [Permissao("fidelidade", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro PremiosFidelidade.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar." }); }
    }

    [HttpPost]
    [Permissao("fidelidade", "i")]
    public async Task<IActionResult> Criar([FromBody] PremioFidelidadeFormDto dto)
    {
        try { return Created("", new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro PremiosFidelidade.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("fidelidade", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] PremioFidelidadeFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Prêmio não encontrado." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro PremiosFidelidade.Atualizar"); return StatusCode(500, new { success = false, message = "Erro ao atualizar." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("fidelidade", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Prêmio não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro PremiosFidelidade.Excluir"); return StatusCode(500, new { success = false, message = "Erro ao excluir." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("fidelidade", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("PremioFidelidade", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro PremiosFidelidade.ObterLog"); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
