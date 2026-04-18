using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/municipios")]
public class MunicipiosController : ControllerBase
{
    private readonly IMunicipioService _service;

    public MunicipiosController(IMunicipioService service) => _service = service;

    /// <summary>Autocomplete por UF + prefixo do nome.</summary>
    [HttpGet]
    public async Task<IActionResult> Pesquisar([FromQuery] string uf, [FromQuery] string? termo, [FromQuery] int limit = 20)
    {
        try
        {
            var lista = await _service.PesquisarAsync(uf, termo, limit);
            return Ok(new { success = true, data = lista });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao pesquisar municípios (uf={Uf}, termo={Termo})", uf, termo);
            return StatusCode(500, new { success = false, message = "Erro ao pesquisar municípios." });
        }
    }
}
