using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.TiposPagamento;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TiposPagamentoController : ControllerBase
{
    private readonly ITipoPagamentoService _service;
    private readonly ILogAcaoService _log;

    public TiposPagamentoController(ITipoPagamentoService service, ILogAcaoService log) { _service = service; _log = log; }

    [HttpGet]
    [Permissao("tipos-pagamento", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em TiposPagamentoController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar tipos de pagamento." }); }
    }

    [HttpPost]
    [Permissao("tipos-pagamento", "i")]
    public async Task<IActionResult> Criar([FromBody] TipoPagamentoFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em TiposPagamentoController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar tipo de pagamento." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("tipos-pagamento", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] TipoPagamentoFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Tipo de pagamento não encontrado." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em TiposPagamentoController.Atualizar | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao atualizar tipo de pagamento." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("tipos-pagamento", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Tipo de pagamento não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em TiposPagamentoController.Excluir | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir tipo de pagamento." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("tipos-pagamento", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("TipoPagamento", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em TiposPagamentoController.ObterLog | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
