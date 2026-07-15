using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly int _noCodigo;

    public SyncController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        // Codigo do NO (eixo Origem/No). Fallback pra chave antiga "Filial:Codigo".
        _noCodigo = int.TryParse(config["No:Codigo"] ?? config["Filial:Codigo"], out var c) ? c : 0;
    }

    /// <summary>
    /// Recebe operações de uma filial, aplica no banco central e enfileira para outras filiais.
    /// </summary>
    [HttpPost("enviar")]
    public async Task<IActionResult> Enviar([FromBody] List<SyncOperacaoDto> operacoes)
    {
        try
        {
            var enfileirados = 0;
            var aplicadosDb = 0;
            var errosDb = 0;

            // 1. Ordenar por dependência antes de aplicar (pais primeiro em INSERT, filhos primeiro em DELETE)
            var ordenadas = operacoes
                .OrderBy(op => op.Operacao == "D"
                    ? -SyncApplicator.GetOrdemTabela(op.Tabela)
                    : SyncApplicator.GetOrdemTabela(op.Tabela))
                .ToList();

            // 2. Aplicar operações no banco central (Railway vira banco consolidado)
            _db.AplicandoSync = true;
            try
            {
                // Retry de itens que faltavam dependencia num push anterior (o central nao roda o
                // background loop, entao a drenagem acontece a cada push recebido).
                await SyncApplicator.DrenarQuarentenaAsync(_db);

                foreach (var op in ordenadas)
                {
                    try
                    {
                        var res = await SyncApplicator.AplicarOperacaoAsync(
                            _db, op.Tabela, op.Operacao, op.RegistroId, op.DadosJson, op.CriadoEm, op.NoOrigemId);

                        switch (res)
                        {
                            case ResultadoSync.Aplicado:
                                aplicadosDb++;
                                break;
                            case ResultadoSync.Idempotente:
                            case ResultadoSync.Stale:
                                break; // ja' no estado alvo / descartado por LWW
                            default: // PrecisaRetry | Conflito | TipoDesconhecido -> quarentena
                                await SyncApplicator.QuarentenarAsync(_db, op.Tabela, op.Operacao, op.RegistroId,
                                    op.DadosJson, op.CriadoEm, op.NoOrigemId, res.ToString(), null);
                                errosDb++;
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        SyncApplicator.Desanexar(_db);
                        await SyncApplicator.QuarentenarAsync(_db, op.Tabela, op.Operacao, op.RegistroId,
                            op.DadosJson, op.CriadoEm, op.NoOrigemId, "Erro", ex.Message);
                        Log.Warning(ex, "Sync enviar: erro ao aplicar no central {Tabela}/{Op} Id={Id}",
                            op.Tabela, op.Operacao, op.RegistroId);
                        errosDb++;
                    }
                }

                // Faxina das lapides fora da retencao (delete indexado, barato).
                await SyncApplicator.PurgarTombstonesAsync(_db);
            }
            finally
            {
                _db.AplicandoSync = false;
            }

            // 3. Enfileirar na SyncFila para outras filiais puxarem (na ordem original)
            foreach (var op in operacoes)
            {
                _db.SyncFila.Add(new SyncFila
                {
                    Tabela = op.Tabela,
                    Operacao = op.Operacao,
                    RegistroId = op.RegistroId,
                    RegistroCodigo = op.RegistroCodigo,
                    DadosJson = op.DadosJson,
                    NoOrigemId = op.NoOrigemId,
                    FilialDonoId = op.FilialDonoId,
                    CriadoEm = op.CriadoEm,
                    Enviado = false
                });
                enfileirados++;
            }

            await _db.SaveChangesAsync();

            Log.Information("Sync enviar: {Enfileirados} enfileirados, {AplicadosDb} aplicados no DB, {ErrosDb} erros",
                enfileirados, aplicadosDb, errosDb);

            return Ok(new { success = true, data = new { enfileirados, aplicadosDb, errosDb } });
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
    public async Task<IActionResult> Receber([FromQuery] int filialId, [FromQuery] string? filiais = null, [FromQuery] long ultimoId = 0, [FromQuery] int limite = 100)
    {
        try
        {
            // Escopo por-filial (Fase 3b): GLOBAL (FilialDonoId==null) vai pra TODOS; POR-FILIAL so' pras
            // filiais que ESTE no atende (No:Filiais, em ?filiais=). Lista vazia => so' GLOBAL (sem vazamento).
            var filiaisDono = (filiais ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => long.TryParse(s, out _)).Select(long.Parse).ToArray();

            var operacoes = await _db.SyncFila
                .Where(f => f.Id > ultimoId && f.NoOrigemId != filialId
                    && (f.FilialDonoId == null || filiaisDono.Contains(f.FilialDonoId.Value)))
                .OrderBy(f => f.Id)
                .Take(limite)
                .Select(f => new
                {
                    f.Id, f.Tabela, f.Operacao, f.RegistroId, f.RegistroCodigo,
                    f.DadosJson, f.NoOrigemId, f.FilialDonoId, f.CriadoEm
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

            // Quarentena (dead-letter do recebimento) — nada pode ficar silencioso no painel.
            var quarentenaPendente = await _db.SyncQuarentena.CountAsync(q => !q.Resolvido);
            var quarentenaPresos = await _db.SyncQuarentena.CountAsync(q => !q.Resolvido &&
                ((q.Motivo == "PrecisaRetry" && q.Tentativas >= SyncApplicator.MaxTentativasReordenacao) ||
                 (q.Motivo != "PrecisaRetry" && q.Tentativas >= SyncApplicator.MaxTentativasQuarentena)));

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
                    quarentenaPendente,
                    quarentenaPresos,
                    filialCodigo = _noCodigo // key mantida p/ compat do painel; valor e' o codigo do NO
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
            else if (status == "enviados") query = query.Where(f => f.Enviado && f.Erro == null && f.NoOrigemId == _noCodigo);
            else if (status == "recebidos") query = query.Where(f => f.Enviado && f.Erro == null && f.NoOrigemId != _noCodigo);
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
    /// Lista a QUARENTENA (dead-letter do recebimento): ops que nao aplicaram e estao em retry.
    /// filtro=presos mostra so' as que estouraram o teto de tentativas (precisam de acao).
    /// </summary>
    [HttpGet("quarentena")]
    public async Task<IActionResult> Quarentena(
        [FromQuery] string? filtro, [FromQuery] string? tabela,
        [FromQuery] int pagina = 1, [FromQuery] int porPagina = 20)
    {
        try
        {
            IQueryable<SyncQuarentena> query = _db.SyncQuarentena.AsNoTracking().Where(q => !q.Resolvido);

            if (filtro == "presos")
                query = query.Where(q =>
                    (q.Motivo == "PrecisaRetry" && q.Tentativas >= SyncApplicator.MaxTentativasReordenacao) ||
                    (q.Motivo != "PrecisaRetry" && q.Tentativas >= SyncApplicator.MaxTentativasQuarentena));

            if (!string.IsNullOrWhiteSpace(tabela))
                query = query.Where(q => q.Tabela.Contains(tabela));

            var total = await query.CountAsync();
            var registros = await query
                .OrderByDescending(q => q.AtualizadoEm)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .Select(q => new
                {
                    q.Id, q.Tabela, q.Operacao, q.RegistroId, q.Motivo, q.Tentativas,
                    q.UltimoErro, q.OpCriadoEm, q.NoOrigemId, q.CriadoEm, q.AtualizadoEm
                })
                .ToListAsync();

            return Ok(new { success = true, data = new { total, registros } });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.Quarentena");
            return StatusCode(500, new { success = false, message = "Erro ao listar quarentena." });
        }
    }

    /// <summary>Reprocessa a quarentena AGORA (reseta tentativas p/ destravar presos e drena).</summary>
    [HttpPost("quarentena/reprocessar")]
    public async Task<IActionResult> ReprocessarQuarentena([FromQuery] long? id)
    {
        try
        {
            if (id.HasValue)
                await _db.SyncQuarentena.Where(q => q.Id == id.Value && !q.Resolvido)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Tentativas, 0));
            else
                await _db.SyncQuarentena.Where(q => !q.Resolvido)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Tentativas, 0));

            _db.AplicandoSync = true;
            int resolvidos;
            try { resolvidos = await SyncApplicator.DrenarQuarentenaAsync(_db); }
            finally { _db.AplicandoSync = false; }

            return Ok(new { success = true, data = new { resolvidos } });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.ReprocessarQuarentena");
            return StatusCode(500, new { success = false, message = "Erro ao reprocessar quarentena." });
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
    /// Reseta o ponteiro de recebimento para rebuscar tudo do Railway.
    /// </summary>
    [HttpPost("resetar-recebimento")]
    public async Task<IActionResult> ResetarRecebimento()
    {
        try
        {
            var config = await _db.Configuracoes
                .FirstOrDefaultAsync(c => c.Chave == "sync.ultimo.id.recebido");

            if (config != null)
            {
                config.Valor = "0";
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true, message = "Ponteiro de recebimento resetado. Próximo ciclo vai rebuscar tudo." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.ResetarRecebimento");
            return StatusCode(500, new { success = false, message = "Erro ao resetar." });
        }
    }

    /// <summary>
    /// Limpa registros já enviados com mais de X dias.
    /// </summary>
    [HttpPost("limpar")]
    public async Task<IActionResult> Limpar([FromQuery] int dias = 7)
    {
        try
        {
            var corte = DataHoraHelper.Agora().AddDays(-dias);
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
    string? DadosJson, long NoOrigemId, long? FilialDonoId, DateTime CriadoEm
);
