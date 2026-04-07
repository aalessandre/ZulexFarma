using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.ContasPagar;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ContasPagarController : ControllerBase
{
    private readonly IContaPagarService _service;
    private readonly ILogAcaoService _log;

    public ContasPagarController(IContaPagarService service, ILogAcaoService log) { _service = service; _log = log; }

    [HttpGet]
    [Permissao("contas-pagar", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasPagarController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar contas a pagar." }); }
    }

    [HttpPost]
    [Permissao("contas-pagar", "i")]
    public async Task<IActionResult> Criar([FromBody] ContaPagarFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasPagarController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar conta a pagar." }); }
    }

    [HttpPost("recorrente")]
    [Permissao("contas-pagar", "i")]
    public async Task<IActionResult> CriarRecorrente([FromBody] ContaPagarRecorrenteDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarRecorrenteAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasPagarController.CriarRecorrente"); return StatusCode(500, new { success = false, message = "Erro ao criar lançamento recorrente." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("contas-pagar", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] ContaPagarFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Conta a pagar não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasPagarController.Atualizar | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao atualizar conta a pagar." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("contas-pagar", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Conta a pagar não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasPagarController.Excluir | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir conta a pagar." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("contas-pagar", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("ContaPagar", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasPagarController.ObterLog | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
