using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

/// <summary>
/// Configurador de visibilidade por ramo (SH). Guarda só as EXCEÇÕES ao default
/// por feature. GET é aberto a qualquer usuário logado (o front precisa dos
/// overrides pra gatear a UI); PUT é só do SISTEMA/SH.
/// Ver docs/specs/configurador-ramo-visibilidade.md.
/// </summary>
[Authorize]
[ApiController]
[Route("api/visibilidade-ramo")]
public class VisibilidadeRamoController : ControllerBase
{
    private readonly AppDbContext _db;
    public VisibilidadeRamoController(AppDbContext db) => _db = db;

    private bool IsSistema() => string.Equals(User.Identity?.Name, "SISTEMA", StringComparison.OrdinalIgnoreCase);

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var data = await _db.RamosVisibilidade
                .Select(v => new { ramo = v.Ramo.ToString(), elementoId = v.ElementoId, visivel = v.Visivel })
                .ToListAsync();
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em VisibilidadeRamoController.Listar");
            return StatusCode(500, new { success = false, message = "Erro ao listar visibilidade." });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Salvar([FromBody] List<VisibilidadeItemDto> itens)
    {
        if (!IsSistema())
            return StatusCode(403, new { success = false, message = "Apenas o usuário SISTEMA pode alterar a visibilidade por ramo." });
        try
        {
            var todos = await _db.RamosVisibilidade.ToListAsync();
            foreach (var item in itens ?? new())
            {
                if (!Enum.TryParse<RamoFilial>(item.Ramo, out var ramo)) continue;
                var elem = (item.ElementoId ?? "").Trim();
                if (elem.Length == 0) continue;

                var existente = todos.FirstOrDefault(v => v.Ramo == ramo && v.ElementoId == elem);
                if (item.Override)
                {
                    // Há override explícito → grava/atualiza.
                    if (existente == null)
                        _db.RamosVisibilidade.Add(new RamoVisibilidade { Ramo = ramo, ElementoId = elem, Visivel = item.Visivel });
                    else
                        existente.Visivel = item.Visivel;
                }
                else if (existente != null)
                {
                    // Voltou pro default → remove o override.
                    _db.RamosVisibilidade.Remove(existente);
                }
            }
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em VisibilidadeRamoController.Salvar");
            return StatusCode(500, new { success = false, message = "Erro ao salvar visibilidade." });
        }
    }
}

public class VisibilidadeItemDto
{
    public string Ramo { get; set; } = "";
    public string ElementoId { get; set; } = "";
    /// <summary>true = existe override explícito (grava Visivel); false = usar default (remove override).</summary>
    public bool Override { get; set; }
    public bool Visivel { get; set; }
}
