using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Clientes;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _service;
    private readonly ILogAcaoService _log;

    public ClientesController(IClienteService service, ILogAcaoService log) { _service = service; _log = log; }

    [HttpGet]
    [Permissao("clientes", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ClientesController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar clientes." }); }
    }

    [HttpGet("{id:long}")]
    [Permissao("clientes", "c")]
    public async Task<IActionResult> Obter(long id)
    {
        try
        {
            var dto = await _service.ObterAsync(id);
            if (dto == null) return NotFound(new { success = false, message = "Cliente não encontrado." });
            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ClientesController.Obter | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao obter cliente." }); }
    }

    [HttpPost]
    [Permissao("clientes", "i")]
    public async Task<IActionResult> Criar([FromBody] ClienteFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ClientesController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar cliente." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("clientes", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] ClienteFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Cliente não encontrado." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ClientesController.Atualizar | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao atualizar cliente." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("clientes", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Cliente não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ClientesController.Excluir | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir cliente." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("clientes", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("Cliente", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em ClientesController.ObterLog | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
