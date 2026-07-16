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

            // 3. Enfileirar na SyncFila para outras filiais puxarem (na ordem original).
            // IDEMPOTENCIA (Fase 4b): se o PUSH chegou mas a RESPOSTA se perdeu, o no reenvia as MESMAS ops
            // (continuam !Enviado la'). Sem dedup, cada reenvio DUPLICA a linha de redistribuicao (aplicar e'
            // idempotente, mas a fila/painel poluem e todo no puxa de novo). Chave: OpUid (Guid da op, nasce
            // no no que a gerou). Guid — e NAO o Id/sequence do no — porque identity e' RECICLAVEL (restore do
            // banco do no reinicia a sequence) e reusar a chave faria DESCARTAR op NOVA como "duplicata".
            // OpUid null = no ANTIGO (pre-4b) -> sem dedup, comportamento de antes (nada e' descartado).
            var jaRedistribuidas = new HashSet<Guid>();
            var uids = operacoes.Where(o => o.OpUid.HasValue).Select(o => o.OpUid!.Value).Distinct().ToList();
            if (uids.Count > 0)
            {
                var existentes = await _db.SyncFila
                    .Where(f => f.OpUid != null && uids.Contains(f.OpUid.Value))
                    .Select(f => f.OpUid!.Value)
                    .ToListAsync();
                foreach (var u in existentes) jaRedistribuidas.Add(u);
            }

            var duplicadas = 0;
            foreach (var op in operacoes)
            {
                // Add() == false -> ja' existia no banco OU repetida no proprio lote (cobre os dois casos)
                if (op.OpUid.HasValue && !jaRedistribuidas.Add(op.OpUid.Value))
                {
                    duplicadas++; // reenvio da MESMA op (mesmo Guid) -> ja' redistribuida, pula
                    continue;
                }

                _db.SyncFila.Add(new SyncFila
                {
                    Tabela = op.Tabela,
                    Operacao = op.Operacao,
                    RegistroId = op.RegistroId,
                    RegistroCodigo = op.RegistroCodigo,
                    DadosJson = op.DadosJson,
                    NoOrigemId = op.NoOrigemId,
                    FilialDonoId = op.FilialDonoId,
                    OpUid = op.OpUid,
                    CriadoEm = op.CriadoEm,
                    Enviado = false
                });
                enfileirados++;
            }

            await _db.SaveChangesAsync();

            Log.Information("Sync enviar: {Enfileirados} enfileirados, {Duplicadas} duplicadas (reenvio), {AplicadosDb} aplicados no DB, {ErrosDb} erros",
                enfileirados, duplicadas, aplicadosDb, errosDb);

            return Ok(new { success = true, data = new { enfileirados, duplicadas, aplicadosDb, errosDb } });
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

            // GAP CONHECIDO (pre-existente, NAO corrigido aqui — precisa de tarefa propria): este cursor
            // (Id > ultimoId) NAO prova consumo. O SyncFila.Id sai no INSERT e so' fica VISIVEL no COMMIT,
            // entao com duas transacoes concorrentes (Id 101 e 102) em que a 102 commita primeiro, um pull
            // neste instante serve a 102, o no crava o ponteiro em 102 e a 101 — ao commitar depois — NUNCA
            // e' entregue (perda silenciosa sob escrita concorrente). Cura: servir so' abaixo de um horizonte
            // de estabilidade (xmin via pg_snapshot_xmin(pg_current_snapshot())) ou ack por no. E' por causa
            // deste gap que a RETENCAO/compactacao da fila central esta' revertida (apagar com base num cursor
            // que nao prova consumo transforma gap recuperavel em perda definitiva).
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
    public async Task<IActionResult> Limpar([FromQuery] int? dias = null)
    {
        try
        {
            // O slider "Limpeza de registros (dias)" grava sync.limpeza.dias — e ate' agora NINGUEM lia
            // (controle morto: o operador arrastava pra 15, salvava, e o botao continuava usando 7 fixo).
            // Fonte da verdade: parametro explicito > config do slider > 7. Piso de 1 dia porque valor
            // negativo inverteria o corte (AddDays(+n)) e apagaria a fila TODA.
            var cfg = await _db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "sync.limpeza.dias");
            var efetivo = dias ?? (int.TryParse(cfg?.Valor, out var cd) ? cd : 7);
            efetivo = Math.Max(efetivo, 1);

            var corte = DataHoraHelper.Agora().AddDays(-efetivo);
            var removidos = await _db.SyncFila
                .Where(f => f.Enviado && f.EnviadoEm < corte)
                .ExecuteDeleteAsync();

            // NOTA: na CENTRAL isso apaga ZERO — as linhas de redistribuicao ficam Enviado=false pra sempre
            // (a central nao faz PUSH). A fila central cresce sem teto por decisao consciente (a retencao
            // foi revertida: ver o comentario em SyncApplicator). Monitorar o tamanho da SyncFila na Railway.
            return Ok(new { success = true, data = new { removidos, dias = efetivo } });
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
    string? DadosJson, long NoOrigemId, long? FilialDonoId, DateTime CriadoEm,
    // Fase 4b: identidade GLOBAL e imutavel da op (Guid nascido no no de origem) = chave de idempotencia
    // do re-enfileiramento. Ausente/null = no ANTIGO (pre-4b) -> sem dedup (nada e' descartado).
    Guid? OpUid = null
);
