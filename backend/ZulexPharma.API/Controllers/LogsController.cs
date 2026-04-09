using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.API.Filters;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public LogsController(AppDbContext db) => _db = db;

    [HttpGet]
    [Permissao("log-geral", "c")]
    public async Task<IActionResult> Listar(
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null,
        [FromQuery] string? tela = null,
        [FromQuery] string? acao = null,
        [FromQuery] string? usuario = null,
        [FromQuery] bool? liberacaoPorSenha = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 50)
    {
        try
        {
            // Usar fuso horário local (Brasil UTC-3) para filtro de data
            var inicio = (dataInicio ?? DataHoraHelper.Agora().AddDays(-7)).Date
                         .AddHours(-3); // Ajuste para cobrir o dia inteiro no fuso BR
            var fim = (dataFim ?? DataHoraHelper.Agora()).Date
                      .AddDays(1).AddHours(-3);

            var query = _db.LogsAcao
                .Include(l => l.Usuario)
                .Include(l => l.UsuarioLiberou)
                .Where(l => l.RealizadoEm >= inicio && l.RealizadoEm < fim);

            if (!string.IsNullOrEmpty(tela))
            {
                var telaLower = tela.ToLower();
                query = query.Where(l => l.Tela.ToLower().Contains(telaLower));
            }
            if (!string.IsNullOrEmpty(acao))
                query = query.Where(l => l.Acao.Contains(acao));
            if (!string.IsNullOrEmpty(usuario))
            {
                var usuarioLower = usuario.ToLower();
                query = query.Where(l => l.Usuario.Nome.ToLower().Contains(usuarioLower) || l.Usuario.Login.ToLower().Contains(usuarioLower));
            }
            if (liberacaoPorSenha.HasValue)
                query = query.Where(l => l.LiberacaoPorSenha == liberacaoPorSenha.Value);

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(l => l.RealizadoEm)
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .Select(l => new
                {
                    l.Id,
                    l.RealizadoEm,
                    NomeUsuario = l.Usuario.Nome,
                    LoginUsuario = l.Usuario.Login,
                    l.Tela,
                    l.Acao,
                    l.Entidade,
                    l.RegistroId,
                    l.ValoresAnteriores,
                    l.ValoresNovos,
                    l.LiberacaoPorSenha,
                    LiberadoPor = l.UsuarioLiberou != null ? l.UsuarioLiberou.Nome + " (" + l.UsuarioLiberou.Login + ")" : null
                })
                .ToListAsync();

            return Ok(new { success = true, data, total, pagina, tamanhoPagina });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em LogsController.Listar");
            return StatusCode(500, new { success = false, message = "Erro ao listar logs." });
        }
    }
}
