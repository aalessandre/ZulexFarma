using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Ncm;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NcmController : ControllerBase
{
    private readonly INcmService _service;

    public NcmController(INcmService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var lista = await _service.ListarAsync();
            return Ok(new { success = true, data = lista });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em NcmController.Listar");
            return StatusCode(500, new { success = false, message = "Erro ao listar NCMs." });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Obter(long id)
    {
        try
        {
            var dto = await _service.ObterAsync(id);
            return Ok(new { success = true, data = dto });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em NcmController.Obter | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao obter NCM." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] NcmFormDto dto)
    {
        try
        {
            var criado = await _service.CriarAsync(dto);
            return CreatedAtAction(nameof(Obter), new { id = criado.Id }, new { success = true, data = criado });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em NcmController.Criar");
            return StatusCode(500, new { success = false, message = "Erro ao criar NCM." });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] NcmFormDto dto)
    {
        try
        {
            await _service.AtualizarAsync(id, dto);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em NcmController.Atualizar | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao atualizar NCM." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(long id)
    {
        try
        {
            var resultado = await _service.ExcluirAsync(id);
            return Ok(new { success = true, data = resultado });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em NcmController.Excluir | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao excluir NCM." });
        }
    }
}
