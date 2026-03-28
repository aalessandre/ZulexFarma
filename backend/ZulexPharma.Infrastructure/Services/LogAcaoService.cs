using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using ZulexPharma.Application.DTOs.Logs;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class LogAcaoService : ILogAcaoService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public LogAcaoService(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task RegistrarAsync(string tela, string acao, string entidade, long registroId,
        Dictionary<string, string?>? anterior = null,
        Dictionary<string, string?>? novo = null)
    {
        var log = new LogAcao
        {
            RealizadoEm       = DateTime.UtcNow,
            UsuarioId         = ObterUsuarioId(),
            Tela              = tela,
            Acao              = acao,
            Entidade          = entidade,
            RegistroId        = registroId.ToString(),
            ValoresAnteriores = anterior != null ? JsonSerializer.Serialize(anterior) : null,
            ValoresNovos      = novo      != null ? JsonSerializer.Serialize(novo)     : null
        };

        _db.LogsAcao.Add(log);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log de auditoria não pode impedir a operação principal
            _db.LogsAcao.Remove(log);
            Serilog.Log.Warning(ex, "Falha ao registrar log de auditoria (UsuarioId={UserId})", log.UsuarioId);
        }
    }

    public async Task<List<LogAcaoListDto>> ListarPorRegistroAsync(string entidade, long registroId,
        DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        var inicio = DateTime.SpecifyKind((dataInicio ?? DateTime.UtcNow.AddDays(-30)).Date, DateTimeKind.Utc);
        var fim    = DateTime.SpecifyKind((dataFim    ?? DateTime.UtcNow).Date.AddDays(1),   DateTimeKind.Utc);

        var logs = await _db.LogsAcao
            .Include(l => l.Usuario)
            .Where(l => l.Entidade == entidade
                     && l.RegistroId == registroId.ToString()
                     && l.RealizadoEm >= inicio
                     && l.RealizadoEm < fim)
            .OrderByDescending(l => l.RealizadoEm)
            .ToListAsync();

        return logs.Select(l => new LogAcaoListDto
        {
            Id          = l.Id,
            RealizadoEm = l.RealizadoEm,
            Acao        = l.Acao,
            NomeUsuario = l.Usuario.Nome,
            Campos      = GerarDiff(l.Acao, l.ValoresAnteriores, l.ValoresNovos)
        }).ToList();
    }

    // ── helpers ──────────────────────────────────────────────────────

    private static List<LogCampoDto> GerarDiff(string acao, string? jsonAnterior, string? jsonNovo)
    {
        var campos = new List<LogCampoDto>();
        if (acao == "EXCLUSÃO")
        {
            // Mostra todos os valores que o registro tinha antes de ser excluído
            if (jsonAnterior != null)
            {
                var opts2 = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var ant = JsonSerializer.Deserialize<Dictionary<string, string?>>(jsonAnterior, opts2) ?? new();
                foreach (var kv in ant)
                    campos.Add(new LogCampoDto { Campo = kv.Key, ValorAnterior = kv.Value, ValorAtual = null });
            }
            return campos;
        }

        if (acao == "DESATIVAÇÃO") return campos;

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var anterior = jsonAnterior != null
            ? JsonSerializer.Deserialize<Dictionary<string, string?>>(jsonAnterior, opts) ?? new()
            : new Dictionary<string, string?>();

        var novo = jsonNovo != null
            ? JsonSerializer.Deserialize<Dictionary<string, string?>>(jsonNovo, opts) ?? new()
            : new Dictionary<string, string?>();

        if (acao == "CRIAÇÃO")
        {
            foreach (var kv in novo)
                campos.Add(new LogCampoDto { Campo = kv.Key, ValorAnterior = null, ValorAtual = kv.Value });
            return campos;
        }

        // ALTERAÇÃO — mostra apenas campos que mudaram
        foreach (var key in anterior.Keys.Union(novo.Keys))
        {
            var a = anterior.GetValueOrDefault(key);
            var n = novo.GetValueOrDefault(key);
            if (a != n)
                campos.Add(new LogCampoDto { Campo = key, ValorAnterior = a, ValorAtual = n });
        }

        return campos;
    }

    private long ObterUsuarioId()
    {
        var claim = _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(claim, out var id) ? id : 0;
    }
}
