using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Usuarios;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.API.Filters;

namespace ZulexPharma.API.Controllers;

// Tela de "Usuarios" aposentada: usuarios sao geridos pela tela de Colaboradores.
// Estes endpoints ficam gateados pela permissao de colaboradores (ou admin) — antes
// eram [Authorize] puro, o que deixava QUALQUER autenticado criar/editar/excluir
// usuarios e resetar senhas (caminho de takeover).
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _service;

    public UsuariosController(IUsuarioService service) => _service = service;

    [HttpGet]
    [Permissao("colaboradores", "c")]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var data = await _service.ListarAsync();
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em UsuariosController.Listar");
            return StatusCode(500, new { success = false, message = "Erro ao listar usuários." });
        }
    }

    [HttpPost]
    [Permissao("colaboradores", "i")]
    public async Task<IActionResult> Criar([FromBody] UsuarioFormDto dto)
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
            Log.Error(ex, "Erro em UsuariosController.Criar");
            return StatusCode(500, new { success = false, message = "Erro ao criar usuário." });
        }
    }

    [HttpPut("{id:long}")]
    [Permissao("colaboradores", "a")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] UsuarioFormDto dto)
    {
        try
        {
            await _service.AtualizarAsync(id, dto);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Usuário não encontrado." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em UsuariosController.Atualizar | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao atualizar usuário." });
        }
    }

    [HttpDelete("{id:long}")]
    [Permissao("colaboradores", "e")]
    public async Task<IActionResult> Excluir(long id)
    {
        try
        {
            await _service.ExcluirAsync(id);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Usuário não encontrado." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em UsuariosController.Excluir | Id: {Id}", id);
            return StatusCode(500, new { success = false, message = "Erro ao excluir usuário." });
        }
    }
}
