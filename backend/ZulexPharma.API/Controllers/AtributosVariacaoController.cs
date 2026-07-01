using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.API.Filters;
using ZulexPharma.Application.DTOs.Grade;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/atributos-variacao")]
public class AtributosVariacaoController : ControllerBase
{
    private readonly IAtributoVariacaoService _service;

    public AtributosVariacaoController(IAtributoVariacaoService service) => _service = service;

    [HttpGet]
    [Permissao("atributos-variacao", "c")]
    public async Task<IActionResult> Listar()
    {
        try { return Ok(new { success = true, data = await _service.ListarAsync() }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em AtributosVariacaoController.Listar");
            return StatusCode(500, new { success = false, message = "Erro ao listar atributos." });
        }
    }

    [HttpPost]
    [Permissao("atributos-variacao", "i")]
    public async Task<IActionResult> Criar([FromBody] AtributoVariacaoFormDto dto)
    {
        try { return Created(string.Empty, new { success = true, data = await _service.CriarAsync(dto) }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em AtributosVariacaoController.Criar");
            return StatusCode(500, new { success = false, message = "Erro ao criar atributo." });
        }
    }

    [HttpPut("{id:long}")]
    [Permissao("atributos-variacao", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] AtributoVariacaoFormDto dto)
    {
        try { await _service.AtualizarAsync(id, dto); return Ok(new { success = true }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Atributo não encontrado." }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em AtributosVariacaoController.Atualizar | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao atualizar atributo." });
        }
    }

    [HttpDelete("{id:long}")]
    [Permissao("atributos-variacao", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try { var r = await _service.ExcluirAsync(id); return Ok(new { success = true, resultado = r }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Atributo não encontrado." }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em AtributosVariacaoController.Excluir | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao excluir atributo." });
        }
    }
}
