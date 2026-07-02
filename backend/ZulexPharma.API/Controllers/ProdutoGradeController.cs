using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.API.Filters;
using ZulexPharma.Application.DTOs.Grade;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/produtos/{produtoId:long}/grade")]
public class ProdutoGradeController : ControllerBase
{
    private readonly IProdutoGradeService _service;

    public ProdutoGradeController(IProdutoGradeService service) => _service = service;

    [HttpGet]
    [Permissao("produtos", "c")]
    public async Task<IActionResult> Obter(long produtoId)
    {
        try { return Ok(new { success = true, data = await _service.ObterAsync(produtoId) }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Produto não encontrado." }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ProdutoGradeController.Obter | Produto: {Id}", produtoId);
            return StatusCode(500, new { success = false, message = "Erro ao carregar a grade." });
        }
    }

    [HttpPut]
    [Permissao("produtos", "a")]
    public async Task<IActionResult> Salvar(long produtoId, [FromBody] SalvarGradeDto dto)
    {
        try { await _service.SalvarAsync(produtoId, dto); return Ok(new { success = true }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (KeyNotFoundException) { return NotFound(new { success = false, message = "Produto não encontrado." }); }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ProdutoGradeController.Salvar | Produto: {Id}", produtoId);
            return StatusCode(500, new { success = false, message = "Erro ao salvar a grade." });
        }
    }
}
