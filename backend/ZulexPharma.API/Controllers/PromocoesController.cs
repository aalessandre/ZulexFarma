using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Promocoes;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PromocoesController : ControllerBase
{
    private readonly IPromocaoService _service;
    private readonly ILogAcaoService _log;

    public PromocoesController(IPromocaoService service, ILogAcaoService log) { _service = service; _log = log; }

    [HttpGet]
    [Permissao("promocoes", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PromocoesController.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar promoções." }); }
    }

    [HttpGet("{id:long}")]
    [Permissao("promocoes", "c")]
    public async Task<IActionResult> Obter(long id)
    {
        try
        {
            var dto = await _service.ObterAsync(id);
            if (dto == null) return NotFound(new { success = false, message = "Promoção não encontrada." });
            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em PromocoesController.Obter | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao obter promoção." }); }
    }

    [HttpPost]
    [Permissao("promocoes", "i")]
    public async Task<IActionResult> Criar([FromBody] PromocaoFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PromocoesController.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar promoção." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("promocoes", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] PromocaoFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Promoção não encontrada." }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PromocoesController.Atualizar | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao atualizar promoção." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("promocoes", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Promoção não encontrada." }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PromocoesController.Excluir | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao excluir promoção." }); }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("promocoes", "c")]
    public async Task<IActionResult> ObterLog(long id, [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null)
    {
        try { return Ok(new { success = true, data = await _log.ListarPorRegistroAsync("Promocao", id, dataInicio, dataFim) }); }
        catch (Exception ex) { Log.Error(ex, "Erro em PromocoesController.ObterLog | Id: {Id}", id); return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." }); }
    }
}
