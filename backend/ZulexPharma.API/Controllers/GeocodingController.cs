using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Application.DTOs.Entregas;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/geocoding")]
public class GeocodingController : ControllerBase
{
    private readonly IGeocodingService _service;

    public GeocodingController(IGeocodingService service) => _service = service;

    /// <summary>Converte endereço textual em coordenadas (usa Nominatim OSM).</summary>
    [HttpPost("buscar")]
    public async Task<IActionResult> Buscar([FromBody] GeocodingRequestDto dto, CancellationToken ct)
    {
        try { return Ok(new { success = true, data = await _service.GeocodificarAsync(dto, ct) }); }
        catch (Exception ex) { Log.Error(ex, "Erro Geocoding.Buscar"); return StatusCode(500, new { success = false, message = "Erro ao consultar geocoding." }); }
    }
}
