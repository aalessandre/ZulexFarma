using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Filiais;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FiliaisController : ControllerBase
{
    private readonly IFilialService _service;
    private readonly ILogAcaoService _log;

    public FiliaisController(IFilialService service, ILogAcaoService log)
    {
        _service = service;
        _log = log;
    }

    [HttpGet]
    [Permissao("filiais", "c")]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var data = await _service.ListarAsync();
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FiliaisController.Listar");
            return StatusCode(500, new { success = false, message = "Erro ao listar filiais." });
        }
    }

    [HttpPost]
    [Permissao("filiais", "i")]
    public async Task<IActionResult> Criar([FromBody] FilialFormDto dto)
    {
        try
        {
            var data = await _service.CriarAsync(dto);
            return Created(string.Empty, new { success = true, data });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FiliaisController.Criar");
            return StatusCode(500, new { success = false, message = "Erro ao criar filial." });
        }
    }

    [HttpPut("{id:long}")]
    [Permissao("filiais", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] FilialFormDto dto)
    {
        try
        {
            await _service.AtualizarAsync(id, dto);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Filial não encontrada." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FiliaisController.Atualizar | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao atualizar filial." });
        }
    }

    [HttpDelete("{id:long}")]
    [Permissao("filiais", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try
        {
            var resultado = await _service.ExcluirAsync(id);
            return Ok(new { success = true, resultado });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Filial não encontrada." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FiliaisController.Excluir | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao excluir filial." });
        }
    }

    [HttpGet("{id:long}/log")]
    [Permissao("filiais", "c")]
    public async Task<IActionResult> ObterLog(long id,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null)
    {
        try
        {
            var data = await _log.ListarPorRegistroAsync("Filial", id, dataInicio, dataFim);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FiliaisController.ObterLog | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao buscar histórico." });
        }
    }
}
