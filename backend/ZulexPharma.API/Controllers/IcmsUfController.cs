using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Fiscal;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/icms-uf")]
public class IcmsUfController : ControllerBase
{
    private readonly IIcmsUfService _service;
    private readonly ILogAcaoService _log;

    public IcmsUfController(IIcmsUfService service, ILogAcaoService log)
    {
        _service = service;
        _log = log;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em IcmsUfController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar." }); }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id)
    {
        try { return Ok(new { success = true, data = await _service.ObterAsync(id) }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em IcmsUfController.Obter"); return StatusCode(500, new { success = false, message = "Erro ao obter." }); }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] IcmsUfFormDto dto)
    {
        try { return Created("", new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em IcmsUfController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar." }); }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] IcmsUfFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em IcmsUfController.Atualizar"); return StatusCode(500, new { success = false, message = "Erro ao atualizar." }); }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Não encontrado." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em IcmsUfController.Excluir"); return StatusCode(500, new { success = false, message = "Erro ao excluir." }); }
    }

    [HttpGet("{id:long}/log")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("IcmsUf", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em IcmsUfController.ObterLog"); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
