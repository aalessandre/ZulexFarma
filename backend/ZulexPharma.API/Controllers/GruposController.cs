using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Grupos;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GruposController : ControllerBase
{
    private readonly IGrupoService _service;

    public GruposController(IGrupoService service) => _service = service;

    [HttpGet]
    [Permissao("grupos", "c")]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var data = await _service.ListarAsync();
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em GruposController.Listar");
            return StatusCode(500, new { success = false, message = "Erro ao listar grupos." });
        }
    }

    [HttpPost]
    [Permissao("grupos", "i")]
    public async Task<IActionResult> Criar([FromBody] GrupoFormDto dto)
    {
        try
        {
            var data = await _service.CriarAsync(dto);
            return Created(string.Empty, new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em GruposController.Criar");
            return StatusCode(500, new { success = false, message = "Erro ao criar grupo." });
        }
    }

    [HttpPut("{id:long}")]
    [Permissao("grupos", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] GrupoFormDto dto)
    {
        try
        {
            await _service.AtualizarAsync(id, dto);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Grupo não encontrado." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em GruposController.Atualizar | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao atualizar grupo." });
        }
    }

    [HttpDelete("{id:long}")]
    [Permissao("grupos", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try
        {
            await _service.ExcluirAsync(id);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Grupo não encontrado." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em GruposController.Excluir | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao excluir grupo." });
        }
    }

    [HttpGet("{id:long}/permissoes")]
    [Permissao("grupos", "c")]
    public async Task<IActionResult> ListarPermissoes(long id)
    {
        try
        {
            var data = await _service.ListarPermissoesAsync(id);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em GruposController.ListarPermissoes | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao listar permissões." });
        }
    }

    [HttpPut("{id:long}/permissoes")]
    [Permissao("grupos", "a")]
    public async Task<IActionResult> SalvarPermissoes(long id, [FromBody] SalvarPermissoesDto dto)
    {
        try
        {
            await _service.SalvarPermissoesAsync(id, dto);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Grupo não encontrado." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em GruposController.SalvarPermissoes | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao salvar permissões." });
        }
    }
}
