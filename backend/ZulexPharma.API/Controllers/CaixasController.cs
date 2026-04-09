using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CaixasController : ControllerBase
{
    private readonly AppDbContext _db;

    public CaixasController(AppDbContext db) => _db = db;

    /// <summary>Retorna o caixa aberto do usuário logado (se houver).</summary>
    [HttpGet("aberto")]
    public async Task<IActionResult> ObterAberto()
    {
        try
        {
            var usuarioId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var caixa = await _db.Caixas
                .Include(c => c.Colaborador).ThenInclude(c => c.Pessoa)
                .Where(c => c.UsuarioId == usuarioId && c.Status == CaixaStatus.Aberto)
                .OrderByDescending(c => c.DataAbertura)
                .FirstOrDefaultAsync();

            if (caixa == null)
                return Ok(new { success = true, data = (object?)null });

            return Ok(new { success = true, data = MapearCaixa(caixa) });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em CaixasController.ObterAberto"); return StatusCode(500, new { success = false, message = "Erro ao buscar caixa aberto." }); }
    }

    /// <summary>Abre um novo caixa para o usuário logado.</summary>
    [HttpPost("abrir")]
    public async Task<IActionResult> Abrir([FromBody] AbrirCaixaRequest request)
    {
        try
        {
            var usuarioId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = await _db.Usuarios.Include(u => u.Colaborador).ThenInclude(c => c!.Pessoa).FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario == null) return BadRequest(new { success = false, message = "Usuário não encontrado." });

            // Verificar se já tem caixa aberto
            var caixaAberto = await _db.Caixas.AnyAsync(c => c.UsuarioId == usuarioId && c.Status == CaixaStatus.Aberto);
            if (caixaAberto) return BadRequest(new { success = false, message = "Já existe um caixa aberto para este usuário." });

            var filialId = usuario.FilialId;
            var colaboradorId = usuario.ColaboradorId ?? 0L;
            if (colaboradorId == 0) return BadRequest(new { success = false, message = "Usuário não possui colaborador vinculado." });

            var caixa = new Caixa
            {
                FilialId = filialId,
                ColaboradorId = colaboradorId,
                UsuarioId = usuarioId,
                DataAbertura = DataHoraHelper.Agora(),
                ValorAbertura = request.ValorAbertura,
                Status = CaixaStatus.Aberto
            };

            _db.Caixas.Add(caixa);
            await _db.SaveChangesAsync();

            // Recarregar com includes
            caixa = await _db.Caixas
                .Include(c => c.Colaborador).ThenInclude(c => c.Pessoa)
                .FirstAsync(c => c.Id == caixa.Id);

            return Created(string.Empty, new { success = true, data = MapearCaixa(caixa) });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em CaixasController.Abrir"); return StatusCode(500, new { success = false, message = "Erro ao abrir caixa." }); }
    }

    /// <summary>Fecha o caixa.</summary>
    [HttpPost("{id:long}/fechar")]
    public async Task<IActionResult> Fechar(long id)
    {
        try
        {
            var caixa = await _db.Caixas.FindAsync(id);
            if (caixa == null) return NotFound(new { success = false, message = "Caixa não encontrado." });
            if (caixa.Status != CaixaStatus.Aberto) return BadRequest(new { success = false, message = "O caixa não está aberto." });

            caixa.Status = CaixaStatus.Fechado;
            caixa.DataFechamento = DataHoraHelper.Agora();
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em CaixasController.Fechar"); return StatusCode(500, new { success = false, message = "Erro ao fechar caixa." }); }
    }

    private static object MapearCaixa(Caixa c) => new
    {
        id = c.Id,
        codigo = c.Codigo,
        colaboradorNome = c.Colaborador?.Pessoa?.Nome ?? "",
        dataAbertura = c.DataAbertura,
        valorAbertura = c.ValorAbertura,
        status = (int)c.Status
    };

    public class AbrirCaixaRequest
    {
        public decimal ValorAbertura { get; set; }
    }
}
