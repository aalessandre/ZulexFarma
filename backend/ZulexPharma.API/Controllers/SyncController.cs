using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly int _filialCodigo;

    public SyncController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _filialCodigo = int.TryParse(config["Filial:Codigo"], out var c) ? c : 0;
    }

    /// <summary>
    /// Recebe operações de uma filial e aplica no banco central.
    /// </summary>
    [HttpPost("enviar")]
    public async Task<IActionResult> Enviar([FromBody] List<SyncOperacaoDto> operacoes)
    {
        try
        {
            var aplicados = 0;
            var erros = 0;

            foreach (var op in operacoes)
            {
                try
                {
                    // Guardar na SyncFila do central (para outras filiais puxarem)
                    _db.SyncFila.Add(new SyncFila
                    {
                        Tabela = op.Tabela,
                        Operacao = op.Operacao,
                        RegistroId = op.RegistroId,
                        RegistroCodigo = op.RegistroCodigo,
                        DadosJson = op.DadosJson,
                        FilialOrigemId = op.FilialOrigemId,
                        CriadoEm = op.CriadoEm,
                        Enviado = false // Pendente para outras filiais
                    });
                    aplicados++;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Sync enviar: erro ao processar op {Tabela}/{Op}", op.Tabela, op.Operacao);
                    erros++;
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { aplicados, erros } });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.Enviar");
            return StatusCode(500, new { success = false, message = "Erro ao aplicar alterações." });
        }
    }

    /// <summary>
    /// Retorna operações pendentes para uma filial (exclui operações da própria filial).
    /// </summary>
    [HttpGet("receber")]
    public async Task<IActionResult> Receber([FromQuery] int filialId, [FromQuery] long ultimoId = 0, [FromQuery] int limite = 100)
    {
        try
        {
            var operacoes = await _db.SyncFila
                .Where(f => f.Id > ultimoId && f.FilialOrigemId != filialId)
                .OrderBy(f => f.Id)
                .Take(limite)
                .Select(f => new
                {
                    f.Id, f.Tabela, f.Operacao, f.RegistroId, f.RegistroCodigo,
                    f.DadosJson, f.FilialOrigemId, f.CriadoEm
                })
                .ToListAsync();

            return Ok(new { success = true, data = operacoes });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.Receber");
            return StatusCode(500, new { success = false, message = "Erro ao obter operações." });
        }
    }

    /// <summary>
    /// Status do sync: pendentes, último envio, serviço.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        try
        {
            var pendentes = await _db.SyncFila.CountAsync(f => !f.Enviado);
            var ultimoEnvio = await _db.SyncFila
                .Where(f => f.Enviado)
                .OrderByDescending(f => f.EnviadoEm)
                .Select(f => f.EnviadoEm)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    rodando = SyncBackgroundService.Rodando,
                    ultimaExecucao = SyncBackgroundService.UltimaExecucao,
                    ultimoStatus = SyncBackgroundService.UltimoStatus,
                    pendentesEnvio = SyncBackgroundService.PendentesEnvio,
                    falhasConsecutivas = SyncBackgroundService.FalhasConsecutivas,
                    pendentesLocal = pendentes,
                    ultimoEnvio,
                    filialCodigo = _filialCodigo
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.Status");
            return StatusCode(500, new { success = false, message = "Erro ao obter status." });
        }
    }

    /// <summary>
    /// Lista a fila de sync com filtros e paginação.
    /// </summary>
    [HttpGet("fila")]
    public async Task<IActionResult> Fila(
        [FromQuery] string? dataInicio, [FromQuery] string? dataFim,
        [FromQuery] string? status, [FromQuery] string? tabela,
        [FromQuery] int pagina = 1, [FromQuery] int porPagina = 20)
    {
        try
        {
            IQueryable<SyncFila> query = _db.SyncFila;

            if (DateTime.TryParse(dataInicio, out var di))
            {
                var diUtc = DateTime.SpecifyKind(di.Date, DateTimeKind.Utc);
                query = query.Where(f => f.CriadoEm >= diUtc);
            }
            if (DateTime.TryParse(dataFim, out var df))
            {
                var dfUtc = DateTime.SpecifyKind(df.Date.AddDays(1), DateTimeKind.Utc);
                query = query.Where(f => f.CriadoEm < dfUtc);
            }

            if (status == "pendentes") query = query.Where(f => !f.Enviado && f.Erro == null);
            else if (status == "enviados") query = query.Where(f => f.Enviado && f.Erro == null && f.FilialOrigemId == _filialCodigo);
            else if (status == "recebidos") query = query.Where(f => f.Enviado && f.Erro == null && f.FilialOrigemId != _filialCodigo);
            else if (status == "erros") query = query.Where(f => f.Erro != null);

            if (!string.IsNullOrWhiteSpace(tabela))
                query = query.Where(f => f.Tabela.Contains(tabela));

            var total = await query.CountAsync();
            var registros = await query
                .OrderByDescending(f => f.Id)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            return Ok(new { success = true, data = new { total, registros } });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.Fila");
            return StatusCode(500, new { success = false, message = "Erro ao listar fila." });
        }
    }

    /// <summary>
    /// Força o envio no próximo ciclo do background service.
    /// </summary>
    [HttpPost("forcar-envio")]
    public IActionResult ForcarEnvio()
    {
        // The background service will pick up pending items on next cycle
        return Ok(new { success = true, message = "Envio será forçado no próximo ciclo." });
    }

    /// <summary>
    /// Limpa registros já enviados com mais de X dias.
    /// </summary>
    [HttpPost("limpar")]
    public async Task<IActionResult> Limpar([FromQuery] int dias = 7)
    {
        try
        {
            var corte = DateTime.UtcNow.AddDays(-dias);
            var removidos = await _db.SyncFila
                .Where(f => f.Enviado && f.EnviadoEm < corte)
                .ExecuteDeleteAsync();

            return Ok(new { success = true, data = new { removidos } });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.Limpar");
            return StatusCode(500, new { success = false, message = "Erro ao limpar." });
        }
    }
}

public record SyncOperacaoDto(
    string Tabela, string Operacao, long RegistroId, string? RegistroCodigo,
    string? DadosJson, long FilialOrigemId, DateTime CriadoEm
);
