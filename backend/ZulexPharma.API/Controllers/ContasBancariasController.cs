using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.ContasBancarias;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ContasBancariasController : ControllerBase
{
    private readonly IContaBancariaService _service;
    private readonly ILogAcaoService _log;

    public ContasBancariasController(IContaBancariaService service, ILogAcaoService log) { _service = service; _log = log; }

    [HttpGet]
    [Permissao("contas-bancarias", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasBancariasController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar contas bancárias." }); }
    }

    [HttpPost]
    [Permissao("contas-bancarias", "i")]
    public async Task<IActionResult> Criar([FromBody] ContaBancariaFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasBancariasController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar conta bancária." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("contas-bancarias", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] ContaBancariaFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Conta bancária não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasBancariasController.Atualizar | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao atualizar conta bancária." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("contas-bancarias", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Conta bancária não encontrada." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasBancariasController.Excluir | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir conta bancária." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("contas-bancarias", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("ContaBancaria", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ContasBancariasController.ObterLog | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
