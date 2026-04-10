using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Adquirentes;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AdquirentesController : ControllerBase
{
    private readonly IAdquirenteService _service;
    private readonly ILogAcaoService _log;

    public AdquirentesController(IAdquirenteService service, ILogAcaoService log) { _service = service; _log = log; }

    [HttpGet]
    [Permissao("adquirentes", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em AdquirentesController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar adquirentes." }); }
    }

    [HttpGet("{id:long}")]
    [Permissao("adquirentes", "c")]
    public async Task<IActionResult> Obter(long id)
    {
        try
        {
            var dto = await _service.ObterAsync(id);
            if (dto == null) return NotFound(new { success = false, message = "Adquirente não encontrada." });
            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em AdquirentesController.Obter"); return StatusCode(500, new { success = false, message = "Erro ao obter adquirente." }); }
    }

    [HttpPost]
    [Permissao("adquirentes", "i")]
    public async Task<IActionResult> Criar([FromBody] AdquirenteFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em AdquirentesController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar adquirente." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("adquirentes", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] AdquirenteFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Adquirente não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em AdquirentesController.Atualizar"); return StatusCode(500, new { success = false, message = "Erro ao atualizar adquirente." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("adquirentes", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Adquirente não encontrada." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em AdquirentesController.Excluir"); return StatusCode(500, new { success = false, message = "Erro ao excluir adquirente." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("adquirentes", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("Adquirente", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em AdquirentesController.ObterLog"); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
