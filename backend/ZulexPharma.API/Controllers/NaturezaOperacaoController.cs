using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Fiscal;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/natureza-operacao")]
public class NaturezaOperacaoController : ControllerBase
{
    private readonly INaturezaOperacaoService _service;
    private readonly ILogAcaoService _log;

    public NaturezaOperacaoController(INaturezaOperacaoService service, ILogAcaoService log) { _service = service; _log = log; }

    [HttpGet]
    [Permissao("natureza-operacao", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em NaturezaOperacaoController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar naturezas de operação." }); }
    }

    [HttpGet("{id:long}")]
    [Permissao("natureza-operacao", "c")]
    public async Task<IActionResult> Obter(long id)
    {
        try { return Ok(new { success = true, data = await _service.ObterAsync(id) }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Natureza de operação não encontrada." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em NaturezaOperacaoController.Obter | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao obter natureza de operação." }); }
    }

    [HttpPost]
    [Permissao("natureza-operacao", "i")]
    public async Task<IActionResult> Criar([FromBody] NaturezaOperacaoFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em NaturezaOperacaoController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar natureza de operação." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("natureza-operacao", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] NaturezaOperacaoFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Natureza de operação não encontrada." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em NaturezaOperacaoController.Atualizar | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao atualizar natureza de operação." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("natureza-operacao", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Natureza de operação não encontrada." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em NaturezaOperacaoController.Excluir | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir natureza de operação." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("natureza-operacao", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("NaturezaOperacao", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em NaturezaOperacaoController.ObterLog | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
