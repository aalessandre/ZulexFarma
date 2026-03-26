using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ConfiguracoesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ConfiguracoesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var data = await _db.Configuracoes
                .OrderBy(c => c.Chave)
                .Select(c => new { c.Id, c.Chave, c.Valor, c.Descricao })
                .ToListAsync();
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ConfiguracoesController.Listar");
            return StatusCode(500, new { success = false, message = "Erro ao listar configurações." });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Salvar([FromBody] List<ConfigItemDto> items)
    {
        try
        {
            foreach (var item in items)
            {
                var config = await _db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == item.Chave);
                if (config != null)
                {
                    config.Valor = item.Valor;
                }
                else
                {
                    _db.Configuracoes.Add(new Configuracao
                    {
                        Chave = item.Chave,
                        Valor = item.Valor,
                        Descricao = item.Descricao
                    });
                }
            }
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ConfiguracoesController.Salvar");
            return StatusCode(500, new { success = false, message = "Erro ao salvar configurações." });
        }
    }

    [HttpGet("sessao")]
    [AllowAnonymous]
    public async Task<IActionResult> ObterConfigSessao()
    {
        try
        {
            var sessaoMaxima = await _db.Configuracoes
                .Where(c => c.Chave == "sessao.maxima.minutos")
                .Select(c => c.Valor).FirstOrDefaultAsync() ?? "480";
            var inatividade = await _db.Configuracoes
                .Where(c => c.Chave == "sessao.inatividade.minutos")
                .Select(c => c.Valor).FirstOrDefaultAsync() ?? "10";

            return Ok(new { success = true, sessaoMaximaMinutos = int.Parse(sessaoMaxima), inatividadeMinutos = int.Parse(inatividade) });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ConfiguracoesController.ObterConfigSessao");
            return StatusCode(500, new { success = false });
        }
    }
}

public record ConfigItemDto(string Chave, string Valor, string? Descricao = null);
