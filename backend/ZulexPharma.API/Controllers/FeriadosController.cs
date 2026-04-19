using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Feriados;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/feriados")]
public class FeriadosController : ControllerBase
{
    private readonly IFeriadoService _service;

    public FeriadosController(IFeriadoService service) => _service = service;

    [HttpGet]
    [Permissao("feriados", "c")]
    public async Task<IActionResult> Listar([FromQuery] int? ano = null, [FromQuery] long? filialId = null)
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync(ano, filialId) }); }
        catch (Exception ex) { Log.Error(ex, "Erro Feriados.Listar"); return StatusCode(500, new { success = false, message = "Erro ao listar feriados." }); }
    }

    [HttpPost]
    [Permissao("feriados", "i")]
    public async Task<IActionResult> Criar([FromBody] FeriadoFormDto dto)
    {
        try { return Created("", new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro Feriados.Criar"); return StatusCode(500, new { success = false, message = "Erro ao criar feriado." }); }
    }

    [HttpPut("{id:long}")]
    [Permissao("feriados", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] FeriadoFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro Feriados.Atualizar"); return StatusCode(500, new { success = false, message = "Erro ao atualizar." }); }
    }

    [HttpDelete("{id:long}")]
    [Permissao("feriados", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { await _service.ExcluirAsync(id); return Ok(new { success = true }); }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro Feriados.Excluir"); return StatusCode(500, new { success = false, message = "Erro ao excluir." }); }
    }

    [HttpPost("importar-nacionais")]
    [Permissao("feriados", "i")]
    public async Task<IActionResult> ImportarNacionais([FromQuery] int ano)
    {
        try { return Ok(new { success = true, data = await _service.ImportarNacionaisAsync(ano) }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro Feriados.Importar"); return StatusCode(500, new { success = false, message = "Erro ao importar feriados." }); }
    }
}
