using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.API.Controllers;

// FASE 1 (autorizacao): o [Authorize] de CLASSE e' so' a linha de base (token valido). CADA action
// declara a policy que precisa: data plane = "SyncNode" (token de MAQUINA emitido pelo /handshake);
// painel/admin = "SyncAdmin" (usuario com claim isAdmin). Um JWT humano comum NAO alcanca mais o
// data plane, e um token de no NAO alcanca o painel. Handshake e' anonimo (valida credencial propria).
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly int _noCodigo;

    /// <summary>Teto de lote do data plane (qtde de ops por request) — protecao contra abuso/erro.</summary>
    public const int MaxOpsPorLote = 500;

    public SyncController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
        // Codigo do NO (eixo Origem/No). Fallback pra chave antiga "Filial:Codigo".
        _noCodigo = int.TryParse(config["No:Codigo"] ?? config["Filial:Codigo"], out var c) ? c : 0;
    }

    /// <summary>Codigo do no autenticado (claim do token de maquina emitido no handshake).</summary>
    private int NoCodigoDoToken() => int.TryParse(User.FindFirst("noCodigo")?.Value, out var n) ? n : -1;

    /// <summary>
    /// FASE 2 — geracao do log do hub (uuid criado UMA vez, em SyncEstadoLocal). O edge guarda a
    /// geracao vista junto do cursor; se o hub for RESTAURADO de backup (sequence/fila voltam no
    /// tempo), a geracao muda e o edge PARA com RebootstrapNecessario — em vez de confiar num cursor
    /// que aponta pra um log que nao existe mais.
    /// </summary>
    private async Task<string> ObterGeracaoHubAsync()
    {
        const string chave = "sync.hub.geracao";
        var estado = await _db.SyncEstadoLocal.FirstOrDefaultAsync(e => e.Chave == chave);
        if (estado != null) return estado.Valor;
        estado = new SyncEstadoLocal { Chave = chave, Valor = Guid.NewGuid().ToString() };
        _db.SyncEstadoLocal.Add(estado);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            // corrida entre dois pulls simultaneos no primeiro boot — o primeiro venceu, usa o dele
            _db.Entry(estado).State = EntityState.Detached;
            estado = await _db.SyncEstadoLocal.FirstAsync(e => e.Chave == chave);
        }
        return estado.Valor;
    }

    // ─── HANDSHAKE (autenticacao de maquina) ───────────────────────────────

    /// <summary>
    /// Autentica um NO cadastrado (credencial por no + anti-gemeo) e emite token de MAQUINA de curta
    /// duracao com claims { syncNode, noCodigo }. Substitui o login SISTEMA no transporte.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("handshake")]
    public async Task<IActionResult> Handshake([FromBody] SyncHandshakeDto dto)
    {
        try
        {
            var (resultado, no) = await SyncNoAuth.ValidarHandshakeAsync(
                _db, dto.NoCodigo, dto.InstanciaUid, dto.Chave, dto.VersaoApp);

            switch (resultado)
            {
                case HandshakeResultado.CredencialInvalida:
                    Log.Warning("Sync handshake NEGADO (credencial) para NoCodigo={No}.", dto.NoCodigo);
                    return Unauthorized(new { success = false, message = "Credencial de no invalida." });
                case HandshakeResultado.NoInativo:
                    return StatusCode(403, new { success = false, message = $"No {dto.NoCodigo} esta '{no!.Status}' — acao necessaria no painel de nos." });
                case HandshakeResultado.Gemeo:
                    return Conflict(new { success = false, codigo = "NoGemeoDetectado", message =
                        $"Ja existe OUTRA instalacao ativa com o codigo {dto.NoCodigo}. Dois nos com o mesmo codigo " +
                        "corrompem a replicacao (colisao de faixa de Id). Se isto e' uma reinstalacao legitima, use " +
                        "'Resetar instancia' no painel de nos da central." });
            }

            var expiraEm = DateTime.UtcNow.AddHours(1);
            var token = GerarTokenDeNo(no!.NoCodigo, expiraEm);
            return Ok(new { success = true, data = new { token, expiraEm } });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.Handshake");
            return StatusCode(500, new { success = false, message = "Erro no handshake." });
        }
    }

    private string GerarTokenDeNo(int noCodigo, DateTime expiraEm)
    {
        var jwt = _config.GetSection("JwtSettings");
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, $"NO:{noCodigo}"),
            new Claim("syncNode", "true"),
            new Claim("noCodigo", noCodigo.ToString())
        };
        var token = new JwtSecurityToken(jwt["Issuer"], jwt["Audience"], claims, expires: expiraEm, signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ─── CADASTRO DE NOS (painel/admin) ────────────────────────────────────

    /// <summary>
    /// Lista os nos cadastrados (sem hash de chave). FASE 5: inclui atraso de entrega
    /// (marca - ack) e alerta de SLA offline (Sync:SlaOfflineDias, default 30) — o painel mostra
    /// QUAL no esta' atrasado e ha' quanto tempo, sem psql.
    /// </summary>
    [Authorize(Policy = "SyncAdmin")]
    [HttpGet("nos")]
    public async Task<IActionResult> ListarNos()
    {
        var marca = await _db.SyncFila.MaxAsync(f => (long?)f.SeqEntrega) ?? 0;
        var slaDias = int.TryParse(_config["Sync:SlaOfflineDias"], out var s) ? s : 30;
        var corteSla = DataHoraHelper.Agora().AddDays(-slaDias);

        var nos = await _db.SyncNos.AsNoTracking()
            .Select(n => new
            {
                n.NoCodigo, n.Nome, n.Status, n.InstanciaUid, n.UltimoAckSeq,
                n.UltimoPushEm, n.UltimoPullEm, n.VersaoApp, n.CriadoEm,
                filiais = n.Filiais.Select(f => f.FilialId).ToList()
            })
            .OrderBy(n => n.NoCodigo)
            .ToListAsync();

        var data = nos.Select(n => new
        {
            n.NoCodigo, n.Nome, n.Status, n.InstanciaUid, n.UltimoAckSeq,
            n.UltimoPushEm, n.UltimoPullEm, n.VersaoApp, n.CriadoEm, n.filiais,
            atrasoSeq = Math.Max(0, marca - n.UltimoAckSeq),
            // SLA estourado NUNCA age sozinho (regra do plano): o painel ALERTA e o admin decide
            // (mudar o status pra RebootstrapNecessario tira o no da retencao explicitamente).
            alertaSla = n.Status == "Ativo" && (n.UltimoPullEm == null || n.UltimoPullEm < corteSla)
        });
        return Ok(new { success = true, data, marcaEntrega = marca, slaDias });
    }

    /// <summary>
    /// FASE 5 — info de BOOTSTRAP (rodar na central, na janela de manutencao): numera tudo que esta'
    /// commitado e devolve a MARCA (watermark) + a geracao. O runbook usa: restore do dump no no
    /// novo + POST /api/sync/cursor com estes valores = o no entra sem gap e sem re-pull do mundo.
    /// </summary>
    [Authorize(Policy = "SyncAdmin")]
    [HttpGet("bootstrap-info")]
    public async Task<IActionResult> BootstrapInfo()
    {
        var marca = await SyncPublicador.NumerarEObterMarcaAsync(_db);
        var geracao = await ObterGeracaoHubAsync();
        return Ok(new { success = true, data = new { marca, geracao, geradoEm = DataHoraHelper.Agora() } });
    }

    /// <summary>
    /// FASE 5 — define o cursor local (rodar NO NO, apos o restore do bootstrap): grava
    /// sync.cursor.entrega e sync.hub.geracao.vista em SyncEstadoLocal.
    /// </summary>
    [Authorize(Policy = "SyncAdmin")]
    [HttpPost("cursor")]
    public async Task<IActionResult> DefinirCursor([FromBody] SyncCursorDto dto)
    {
        async Task Upsert(string chave, string valor)
        {
            var e = await _db.SyncEstadoLocal.FirstOrDefaultAsync(x => x.Chave == chave);
            if (e == null) _db.SyncEstadoLocal.Add(new SyncEstadoLocal { Chave = chave, Valor = valor });
            else { e.Valor = valor; e.AtualizadoEm = DataHoraHelper.Agora(); }
        }
        await Upsert("sync.cursor.entrega", dto.Cursor.ToString());
        if (!string.IsNullOrWhiteSpace(dto.Geracao))
            await Upsert("sync.hub.geracao.vista", dto.Geracao!);
        await _db.SaveChangesAsync();
        Log.Warning("Sync: cursor local DEFINIDO por {User}: {Cursor} (geracao {Geracao}).", User.Identity?.Name, dto.Cursor, dto.Geracao);
        return Ok(new { success = true });
    }

    /// <summary>
    /// FASE 5 — CHECKSUM de reconciliacao: contagem + md5 de (Id, AtualizadoEm) ordenado. O admin
    /// compara hub × no (mesma tabela, mesmo filtro) e enxerga divergencia sem psql. Tabela validada
    /// pelo dicionario (sem SQL arbitrario).
    /// </summary>
    [Authorize(Policy = "SyncAdmin")]
    [HttpGet("checksum")]
    public async Task<IActionResult> Checksum([FromQuery] string tabela, [FromQuery] long? filialId = null)
    {
        var tipo = SyncApplicator.ResolverTipo(tabela);
        if (tipo == null)
            return BadRequest(new { success = false, message = $"Tabela '{tabela}' desconhecida no dicionario de replicacao." });
        var et = _db.Model.FindEntityType(tipo)!;
        var nomeTabela = et.GetTableName()!;
        if (filialId != null && et.FindProperty("FilialId") == null)
            return BadRequest(new { success = false, message = $"'{tabela}' nao tem FilialId — chame sem o filtro." });

        var where = filialId != null ? $" WHERE \"FilialId\" = {filialId.Value}" : "";
        var conn = _db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        // to_char com mascara fixa: o ::text dependeria do DateStyle do servidor (hub e loja podem
        // divergir e o hash mentiria "divergencia" com dado identico).
        cmd.CommandText = $@"SELECT COUNT(*)::bigint,
            COALESCE(md5(string_agg(""Id""::text || ':' || COALESCE(to_char(""AtualizadoEm"", 'YYYY-MM-DD HH24:MI:SS.US'), ''), ',' ORDER BY ""Id"")), '')
            FROM ""{nomeTabela}""{where}";
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        return Ok(new { success = true, data = new { tabela = nomeTabela, filialId, count = reader.GetInt64(0), hash = reader.GetString(1) } });
    }

    /// <summary>
    /// Cadastra um no e devolve a CHAVE EM CLARO (unica vez — so o hash fica no banco).
    /// NoCodigo nunca e' reutilizado (mesmo espaco da faixa de Id).
    /// </summary>
    [Authorize(Policy = "SyncAdmin")]
    [HttpPost("nos")]
    public async Task<IActionResult> CriarNo([FromBody] SyncNoCriarDto dto)
    {
        if (dto.NoCodigo < 1)
            return BadRequest(new { success = false, message = "NoCodigo deve ser >= 1 (0 e' o hub)." });
        if (await _db.SyncNos.AnyAsync(n => n.NoCodigo == dto.NoCodigo))
            return Conflict(new { success = false, message = $"NoCodigo {dto.NoCodigo} ja cadastrado (codigo nunca e' reutilizado)." });

        var chave = SyncNoAuth.GerarChave();
        var no = new SyncNo
        {
            NoCodigo = dto.NoCodigo, Nome = dto.Nome, ChaveHash = SyncNoAuth.HashChave(chave),
            Status = "Provisionando",
            Filiais = (dto.Filiais ?? new List<long>()).Distinct().Select(f => new SyncNoFilial { NoCodigo = dto.NoCodigo, FilialId = f }).ToList()
        };
        _db.SyncNos.Add(no);
        await _db.SaveChangesAsync();
        Log.Information("Sync: no {No} ({Nome}) cadastrado por {User}.", dto.NoCodigo, dto.Nome, User.Identity?.Name);
        return Ok(new { success = true, data = new { no.NoCodigo, chave, aviso = "Guarde a chave AGORA — ela nao sera exibida de novo (so o hash fica no banco)." } });
    }

    /// <summary>Rotaciona a chave do no (invalida a anterior) e devolve a nova em claro (unica vez).</summary>
    [Authorize(Policy = "SyncAdmin")]
    [HttpPost("nos/{noCodigo:int}/nova-chave")]
    public async Task<IActionResult> NovaChave(int noCodigo)
    {
        var no = await _db.SyncNos.FindAsync(noCodigo);
        if (no == null) return NotFound(new { success = false, message = "No nao cadastrado." });
        var chave = SyncNoAuth.GerarChave();
        no.ChaveHash = SyncNoAuth.HashChave(chave);
        await _db.SaveChangesAsync();
        Log.Warning("Sync: chave do no {No} ROTACIONADA por {User}.", noCodigo, User.Identity?.Name);
        return Ok(new { success = true, data = new { no.NoCodigo, chave } });
    }

    /// <summary>Limpa o InstanciaUid (reinstalacao LEGITIMA do servidor do no) — o proximo handshake crava o novo.</summary>
    [Authorize(Policy = "SyncAdmin")]
    [HttpPost("nos/{noCodigo:int}/resetar-instancia")]
    public async Task<IActionResult> ResetarInstancia(int noCodigo)
    {
        var no = await _db.SyncNos.FindAsync(noCodigo);
        if (no == null) return NotFound(new { success = false, message = "No nao cadastrado." });
        Log.Warning("Sync: InstanciaUid do no {No} RESETADO por {User} (era {Uid}).", noCodigo, User.Identity?.Name, no.InstanciaUid);
        no.InstanciaUid = null;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Instancia resetada — o proximo handshake crava a nova." });
    }

    /// <summary>Atualiza nome/status/filiais do no.</summary>
    [Authorize(Policy = "SyncAdmin")]
    [HttpPut("nos/{noCodigo:int}")]
    public async Task<IActionResult> AtualizarNo(int noCodigo, [FromBody] SyncNoAtualizarDto dto)
    {
        var statusValidos = new[] { "Provisionando", "Ativo", "Suspenso", "RebootstrapNecessario", "Desativado" };
        if (dto.Status != null && !statusValidos.Contains(dto.Status))
            return BadRequest(new { success = false, message = $"Status invalido. Use: {string.Join(", ", statusValidos)}." });

        var no = await _db.SyncNos.Include(n => n.Filiais).FirstOrDefaultAsync(n => n.NoCodigo == noCodigo);
        if (no == null) return NotFound(new { success = false, message = "No nao cadastrado." });

        if (dto.Nome != null) no.Nome = dto.Nome;
        if (dto.Status != null) no.Status = dto.Status;
        var avisoEscopo = (string?)null;
        if (dto.Filiais != null)
        {
            // Fase 2b (achado ALTO): AMPLIAR o escopo de um no que ja' puxou cria gap permanente —
            // as ops da filial nova com SeqEntrega < cursor ficam ATRAS do ponteiro pra sempre.
            // Falha ruidosa: o no vai pra RebootstrapNecessario e so' volta por acao explicita
            // (bootstrap/resetar-recebimento). REMOVER filial e' seguro.
            var atuais = no.Filiais.Select(f => f.FilialId).ToHashSet();
            var adicionadas = dto.Filiais.Where(f => !atuais.Contains(f)).ToList();
            if (adicionadas.Count > 0 && no.UltimoPullEm != null && dto.Status == null)
            {
                no.Status = "RebootstrapNecessario";
                avisoEscopo = $"Filiais adicionadas ({string.Join(",", adicionadas)}) a um no que ja' puxou: " +
                    "status mudou pra RebootstrapNecessario — o historico da filial nova esta' ATRAS do cursor. " +
                    "Faca o BOOTSTRAP (runbook fase 5) e reative. Obs.: resetar-recebimento so' funciona se a " +
                    "central NUNCA compactou (marca de retencao zero) — senao o pull leva 409.";
            }
            _db.SyncNoFiliais.RemoveRange(no.Filiais);
            no.Filiais = dto.Filiais.Distinct().Select(f => new SyncNoFilial { NoCodigo = noCodigo, FilialId = f }).ToList();
        }
        await _db.SaveChangesAsync();
        Log.Information("Sync: no {No} atualizado por {User} (status={Status}). {Aviso}", noCodigo, User.Identity?.Name, no.Status, avisoEscopo);
        return Ok(new { success = true, aviso = avisoEscopo });
    }

    // ─── DATA PLANE (token de maquina) ─────────────────────────────────────

    /// <summary>
    /// Recebe operações de um NO autenticado, aplica no banco central e enfileira para os outros.
    /// FASE 1: a ORIGEM das ops e' o no do TOKEN (derivada pelo servidor) — o NoOrigemId do body e' ignorado.
    /// </summary>
    [Authorize(Policy = "SyncNode")]
    [HttpPost("enviar")]
    public async Task<IActionResult> Enviar([FromBody] List<SyncOperacaoDto> operacoes)
    {
        try
        {
            var noAutenticado = NoCodigoDoToken();
            if (noAutenticado < 1)
                return StatusCode(403, new { success = false, message = "Token sem identidade de no." });
            // Fase 1b: Suspenso/Desativado no painel corta o data plane JA (o token vale ate' 1h;
            // sem esta checagem a suspensao so' teria efeito no proximo handshake).
            var statusNo = await _db.SyncNos.AsNoTracking()
                .Where(n => n.NoCodigo == noAutenticado).Select(n => n.Status).FirstOrDefaultAsync();
            if (statusNo != "Ativo")
                return StatusCode(403, new { success = false, message = statusNo == null
                    ? $"No {noAutenticado} nao cadastrado."
                    : $"No {noAutenticado} esta '{statusNo}' — data plane bloqueado." });
            if (operacoes.Count > MaxOpsPorLote)
                return BadRequest(new { success = false, message = $"Lote acima do teto ({operacoes.Count} > {MaxOpsPorLote}). Reduza Sync:LoteTamanho." });

            var enfileirados = 0;
            var aplicadosDb = 0;
            var errosDb = 0;
            // FASE 2 (push honesto, P0.14): resultado POR OP — o edge marca Enviado so' pelo que o hub
            // declarou ter tratado de forma DURAVEL (aplicado, descartado por LWW ou quarentenado).
            var resultadoPorOp = new Dictionary<SyncOperacaoDto, ResultadoSync>(ReferenceEqualityComparer.Instance);

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
                // background loop, entao a drenagem acontece a cada push recebido). Fase 2: retry que
                // APLICA re-enfileira a op (conflito nao se espalhou na chegada — espalha ao resolver).
                // So' no hub (no 0): edge nao redistribui.
                await SyncApplicator.DrenarQuarentenaAsync(_db, enfileirarAoResolver: _noCodigo == 0);

                foreach (var op in ordenadas)
                {
                    try
                    {
                        // Origem = no do TOKEN (P0.3/Codex: origem vem da credencial, nunca do body).
                        var res = await SyncApplicator.AplicarOperacaoAsync(
                            _db, op.Tabela, op.Operacao, op.RegistroId, op.DadosJson, op.CriadoEm, noAutenticado);
                        resultadoPorOp[op] = res;

                        switch (res)
                        {
                            case ResultadoSync.Aplicado:
                                aplicadosDb++;
                                break;
                            case ResultadoSync.Idempotente:
                                break; // ja' no estado alvo
                            case ResultadoSync.Stale:
                                // Objetivo 7 (NADA silencioso): descarte por LWW vira trilha auditavel.
                                await SyncApplicator.RegistrarDescarteLwwAsync(_db, op.Tabela, op.Operacao,
                                    op.RegistroId, op.DadosJson, op.CriadoEm, noAutenticado, op.OpUid, op.FilialDonoId);
                                break;
                            default: // PrecisaRetry | Conflito | TipoDesconhecido -> quarentena
                                await SyncApplicator.QuarentenarAsync(_db, op.Tabela, op.Operacao, op.RegistroId,
                                    op.DadosJson, op.CriadoEm, noAutenticado, res.ToString(), null,
                                    op.OpUid, op.FilialDonoId);
                                errosDb++;
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        SyncApplicator.Desanexar(_db);
                        resultadoPorOp[op] = ResultadoSync.Conflito;
                        await SyncApplicator.QuarentenarAsync(_db, op.Tabela, op.Operacao, op.RegistroId,
                            op.DadosJson, op.CriadoEm, noAutenticado, "Erro", ex.Message,
                            op.OpUid, op.FilialDonoId);
                        Log.Warning(ex, "Sync enviar: erro ao aplicar no central {Tabela}/{Op} Id={Id}",
                            op.Tabela, op.Operacao, op.RegistroId);
                        errosDb++;
                    }
                }

                // (Fase 3: a purga de lapides por idade FOI REMOVIDA — decisao A7. Lapide e' marcador
                //  minimo sem PII; purgar era o que permitia ressurreicao por no/backup atrasado.)
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
                // FASE 2 (P1.4): so' redistribui o que APLICOU (ou ja' estava aplicado). Quarentenada/
                // Stale NAO entra na fila — conflito nao se espalha antes de resolvido (a drenagem
                // enfileira quando o retry aplicar). Idempotente entra pra cobrir o crash entre o
                // apply e o enfileiramento do request anterior (o dedup por OpUid impede duplicar).
                // TryGetValue de proposito: GetValueOrDefault devolveria Aplicado (enum 0) pra chave
                // ausente — fail-open. Ausente = nao redistribui.
                if (!resultadoPorOp.TryGetValue(op, out var resOp)
                    || resOp is not (ResultadoSync.Aplicado or ResultadoSync.Idempotente))
                    continue;

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
                    NoOrigemId = noAutenticado, // origem = credencial, nunca o body
                    FilialDonoId = op.FilialDonoId,
                    OpUid = op.OpUid,
                    CriadoEm = op.CriadoEm,
                    Enviado = false
                });
                enfileirados++;
            }

            await _db.SaveChangesAsync();

            // FASE 2: publicador oportunista — numera as linhas recem-commitadas (e quaisquer outras
            // pendentes) pra ficarem visiveis ao pull. Try-lock: se outro request estiver numerando, ok.
            await SyncPublicador.NumerarEObterMarcaAsync(_db);

            // Telemetria por no (painel de nos)
            await _db.SyncNos.Where(n => n.NoCodigo == noAutenticado)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.UltimoPushEm, DataHoraHelper.Agora()));

            Log.Information("Sync enviar: {Enfileirados} enfileirados, {Duplicadas} duplicadas (reenvio), {AplicadosDb} aplicados no DB, {ErrosDb} erros",
                enfileirados, duplicadas, aplicadosDb, errosDb);

            // FASE 2: resposta POR OP (indice = posicao no lote enviado) — o edge marca Enviado so'
            // pelo que aparece aqui. Tudo listado esta' DURAVEL no hub (aplicado, stale auditado ou
            // quarentenado); falha de transporte = nada listado = nada marcado.
            var resultados = operacoes
                .Select((op, indice) => new
                {
                    indice,
                    opUid = op.OpUid,
                    // Ausente do mapa (nao deveria acontecer) = "NaoProcessada": NADA duravel no hub —
                    // o edge NAO marca Enviado (o "Erro" do catch e' diferente: aquele esta' quarentenado).
                    resultado = resultadoPorOp.TryGetValue(op, out var r) ? r.ToString() : "NaoProcessada"
                })
                .ToList();

            return Ok(new { success = true, data = new { enfileirados, duplicadas, aplicadosDb, errosDb, resultados } });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.Enviar");
            return StatusCode(500, new { success = false, message = "Erro ao aplicar alterações." });
        }
    }

    /// <summary>
    /// Retorna operações pendentes para o NO autenticado (exclui as do próprio no).
    /// FASE 1: identidade e ESCOPO vem do servidor (token + cadastro SyncNoFiliais) — os parametros
    /// filialId/filiais da query sao IGNORADOS (mantidos so' por compat de assinatura com nos antigos).
    /// FASE 2: cursor = SeqEntrega (numerada pelo publicador SO' em linha commitada — gap do Id morto);
    /// resposta inclui cursorProximo (calculado no servidor), a GERACAO do hub (restore = rebootstrap)
    /// e cada op leva SeqEntrega+OpUid. O request traz ack = cursor duravel do no (retencao fase 5).
    /// </summary>
    [Authorize(Policy = "SyncNode")]
    [HttpGet("receber")]
    public async Task<IActionResult> Receber([FromQuery] int filialId = 0, [FromQuery] string? filiais = null, [FromQuery] long ultimoId = 0, [FromQuery] int limite = 100, [FromQuery] long cursor = 0, [FromQuery] long ack = -1)
    {
        try
        {
            var noAutenticado = NoCodigoDoToken();
            if (noAutenticado < 1)
                return StatusCode(403, new { success = false, message = "Token sem identidade de no." });
            var statusNo = await _db.SyncNos.AsNoTracking()
                .Where(n => n.NoCodigo == noAutenticado).Select(n => n.Status).FirstOrDefaultAsync();
            if (statusNo != "Ativo")
                return StatusCode(403, new { success = false, message = statusNo == null
                    ? $"No {noAutenticado} nao cadastrado."
                    : $"No {noAutenticado} esta '{statusNo}' — data plane bloqueado." });
            limite = Math.Clamp(limite, 1, MaxOpsPorLote);

            // Escopo por-filial: GLOBAL (FilialDonoId==null) vai pra todos; POR-FILIAL so' pras filiais
            // AUTORIZADAS no cadastro do no (SyncNoFiliais). Nada vem do cliente: um no nao consegue
            // pedir escopo alheio. Lista vazia => so' GLOBAL (sem vazamento).
            var filiaisDono = await _db.SyncNoFiliais.AsNoTracking()
                .Where(nf => nf.NoCodigo == noAutenticado)
                .Select(nf => nf.FilialId)
                .ToArrayAsync();

            // Fase 2b (achado ALTO): edge FASE-1 nao manda ack (default -1) e usa cursor por Id — no
            // protocolo novo ele ficaria CONGELADO nas primeiras N ops pra sempre, com painel OK dos
            // dois lados. Falha RUIDOSA: 426 ate' o no ser atualizado (rollout e' hub+lojas juntos).
            if (ack < 0)
                return StatusCode(426, new { success = false, codigo = "ProtocoloAntigo", message =
                    $"O no {noAutenticado} usa o protocolo de pull antigo (sem ack/cursor de entrega). " +
                    "Atualize o backend do no pra fase 2 — servir o protocolo antigo congelaria o pull em silencio." });

            // Fase 1b (achado ALTO): edge configurado com No:Filiais mas cadastro SEM filiais = provavel
            // esquecimento no provisionamento. Servir so' GLOBAL aqui avancaria o cursor POR CIMA das ops
            // por-filial — quando o admin corrigisse, o gap seria permanente (ate' um resetar-recebimento).
            // Falha RUIDOSA > gap silencioso.
            if (filiaisDono.Length == 0 && !string.IsNullOrWhiteSpace(filiais))
                return StatusCode(422, new { success = false, codigo = "EscopoNaoConfigurado", message =
                    $"O no {noAutenticado} declara filiais ({filiais}) mas o cadastro na central nao tem NENHUMA. " +
                    "Configure as filiais do no no painel (PUT /api/sync/nos/{codigo}) antes de puxar — servir so' " +
                    "GLOBAL avancaria o cursor por cima das ops por-filial e o gap seria permanente." });

            // FASE 5 — guard da compactacao: pull ABAIXO da marca de retencao significa que as ops
            // que o no pediria JA' FORAM APAGADAS (ele nao e' Ativo com ack — foi suspenso/rebaixado
            // e voltou sem bootstrap). Servir o que sobrou seria lote parcial SILENCIOSO.
            var marcaRetidaCfg = await _db.SyncEstadoLocal.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Chave == "sync.retencao.marca");
            if (marcaRetidaCfg != null && long.TryParse(marcaRetidaCfg.Valor, out var marcaRetida) && cursor < marcaRetida)
                return StatusCode(409, new { success = false, codigo = "RebootstrapNecessario", message =
                    $"O cursor {cursor} esta' ABAIXO da marca de compactacao {marcaRetida}: as ops desse trecho ja' " +
                    "foram apagadas pela retencao. O no precisa de BOOTSTRAP (runbook fase 5) — servir o resto " +
                    "seria um lote parcial silencioso." });

            // FASE 2: publicador oportunista (try-lock; quem nao pega serve o que ja' esta' numerado)
            // + marca d'agua. O select e' LIMITADO pela marca — numeracao concorrente alem dela fica
            // pro proximo pull (senao o cursorProximo podia pular ops nao servidas).
            var marca = await SyncPublicador.NumerarEObterMarcaAsync(_db);
            var geracao = await ObterGeracaoHubAsync();

            // ACK: maior SeqEntrega processada DURAVELMENTE pelo no (base da retencao na fase 5).
            if (ack >= 0)
                await _db.SyncNos.Where(n => n.NoCodigo == noAutenticado && n.UltimoAckSeq < ack)
                    .ExecuteUpdateAsync(s => s.SetProperty(n => n.UltimoAckSeq, ack));

            var operacoes = await _db.SyncFila
                .Where(f => f.SeqEntrega != null && f.SeqEntrega > cursor && f.SeqEntrega <= marca
                    && f.NoOrigemId != noAutenticado
                    && (f.FilialDonoId == null || filiaisDono.Contains(f.FilialDonoId.Value)))
                .OrderBy(f => f.SeqEntrega)
                .Take(limite)
                .Select(f => new
                {
                    f.Id, f.SeqEntrega, f.OpUid, f.Tabela, f.Operacao, f.RegistroId, f.RegistroCodigo,
                    f.DadosJson, f.NoOrigemId, f.FilialDonoId, f.CriadoEm
                })
                .ToListAsync();

            // Lote nao encheu = o scan chegou na marca (as demais foram filtradas: eco/escopo) -> o
            // cursor pode ir direto pra marca. Lote cheio = ainda pode haver op entre a ultima servida
            // e a marca -> cursor para na ultima servida. Nunca regride.
            var cursorProximo = operacoes.Count < limite ? marca : operacoes[^1].SeqEntrega!.Value;
            if (cursorProximo < cursor) cursorProximo = cursor;

            await _db.SyncNos.Where(n => n.NoCodigo == noAutenticado)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.UltimoPullEm, DataHoraHelper.Agora()));

            // GAP DO CURSOR: FECHADO na fase 2 (opcao B do synAteAqui §6.1). O cursor e' SeqEntrega,
            // numerada pelo publicador SO' em linha COMMITADA — commit tardio pega numero maior na
            // rodada seguinte e e' entregue. Teste: CursorGapTests. Historico completo: synAteAqui.md.
            return Ok(new { success = true, data = operacoes, cursorProximo, seqMaxNumerado = marca, geracao });
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
    [Authorize(Policy = "SyncAdmin")]
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
                (((q.Motivo == "PrecisaRetry" || q.Motivo == "RelogioSuspeito") && q.Tentativas >= SyncApplicator.MaxTentativasReordenacao) ||
                 (q.Motivo != "PrecisaRetry" && q.Motivo != "RelogioSuspeito" && q.Tentativas >= SyncApplicator.MaxTentativasQuarentena)));

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
    [Authorize(Policy = "SyncAdmin")]
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
    [Authorize(Policy = "SyncAdmin")]
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
                    ((q.Motivo == "PrecisaRetry" || q.Motivo == "RelogioSuspeito") && q.Tentativas >= SyncApplicator.MaxTentativasReordenacao) ||
                    (q.Motivo != "PrecisaRetry" && q.Motivo != "RelogioSuspeito" && q.Tentativas >= SyncApplicator.MaxTentativasQuarentena));

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
    [Authorize(Policy = "SyncAdmin")]
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

            // No hub (no 0), retry que aplicar precisa entrar na fila de redistribuicao + ser numerado.
            _db.AplicandoSync = true;
            int resolvidos;
            try { resolvidos = await SyncApplicator.DrenarQuarentenaAsync(_db, enfileirarAoResolver: _noCodigo == 0); }
            finally { _db.AplicandoSync = false; }
            if (_noCodigo == 0 && resolvidos > 0)
                await SyncPublicador.NumerarEObterMarcaAsync(_db);

            return Ok(new { success = true, data = new { resolvidos } });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SyncController.ReprocessarQuarentena");
            return StatusCode(500, new { success = false, message = "Erro ao reprocessar quarentena." });
        }
    }

    /// <summary>
    /// Descarta um item da quarentena por decisao humana (marca Resolvido, motivo DescartadoManual).
    /// Fase 2b: sem isso, op de tabela aposentada (ex.: Configuracoes) presa no teto poluia o alarme
    /// 'presos' pra sempre — so' saia por SQL manual.
    /// </summary>
    [Authorize(Policy = "SyncAdmin")]
    [HttpPost("quarentena/descartar")]
    public async Task<IActionResult> DescartarQuarentena([FromQuery] long id)
    {
        var atualizados = await _db.SyncQuarentena.Where(q => q.Id == id && !q.Resolvido)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Resolvido, true)
                .SetProperty(x => x.Motivo, "DescartadoManual")
                .SetProperty(x => x.AtualizadoEm, DataHoraHelper.Agora()));
        if (atualizados > 0)
            Log.Warning("Sync: quarentena {Id} DESCARTADA manualmente por {User}.", id, User.Identity?.Name);
        return Ok(new { success = true, descartados = atualizados });
    }

    /// <summary>
    /// Força o envio no próximo ciclo do background service.
    /// </summary>
    [Authorize(Policy = "SyncAdmin")]
    [HttpPost("forcar-envio")]
    public IActionResult ForcarEnvio()
    {
        // The background service will pick up pending items on next cycle
        return Ok(new { success = true, message = "Envio será forçado no próximo ciclo." });
    }

    /// <summary>
    /// Reseta o ponteiro de recebimento para rebuscar tudo do Railway.
    /// FASE 2: o cursor vive em SyncEstadoLocal ('sync.cursor.entrega'), nao mais em Configuracoes.
    /// </summary>
    [Authorize(Policy = "SyncAdmin")]
    [HttpPost("resetar-recebimento")]
    public async Task<IActionResult> ResetarRecebimento()
    {
        try
        {
            var estado = await _db.SyncEstadoLocal.FirstOrDefaultAsync(e => e.Chave == "sync.cursor.entrega");
            if (estado != null)
            {
                estado.Valor = "0";
                estado.AtualizadoEm = DataHoraHelper.Agora();
                await _db.SaveChangesAsync();
            }

            // Fase 2b (achado da revisao): limpar tambem a geracao VISTA — senao, apos um hub
            // legitimamente recriado, o reset zera o cursor mas o guard de geracao trava de novo
            // no primeiro pull ("rebuscar tudo" viraria mentira).
            var geracaoVista = await _db.SyncEstadoLocal.FirstOrDefaultAsync(e => e.Chave == "sync.hub.geracao.vista");
            if (geracaoVista != null)
            {
                _db.SyncEstadoLocal.Remove(geracaoVista);
                await _db.SaveChangesAsync();
            }

            // Cursor legado (pre-fase-2, vivia em Configuracoes) — zera tambem por consistencia.
            var legado = await _db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "sync.ultimo.id.recebido");
            if (legado != null)
            {
                legado.Valor = "0";
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true, message = "Ponteiro de recebimento resetado. Próximo ciclo vai rebuscar tudo. " +
                "ATENÇÃO: se a central já compactou a fila (retenção), o pull vai responder 409 e este nó " +
                "precisará de BOOTSTRAP (runbook fase 5) — o reset não recupera ops já apagadas." });
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
    [Authorize(Policy = "SyncAdmin")]
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

            // FASE 5 — RETENCAO DA FILA CENTRAL, religada com os pre-requisitos que a tentativa
            // revertida nao tinha: (a) registro EXPLICITO de nos (SyncNos) — no cadastrado que nunca
            // puxou tem ack 0 e RETEM TUDO (fail-closed; antes o no invisivel pro MIN perdia o
            // backlog); (b) o ack agora PROVA processamento duravel (cursor persistido apos aplicar);
            // (c) marca de compactacao persistida — pull abaixo dela leva 409, nao lote parcial.
            // No Suspenso/RebootstrapNecessario/Desativado NAO segura a retencao — tirar o no do MIN
            // e' exatamente a ACAO EXPLICITA de mudar o status (auditada no painel).
            long removidosRetencao = 0;
            long marcaRetencao = 0;
            if (_noCodigo == 0)
            {
                var acksAtivos = await _db.SyncNos.AsNoTracking()
                    .Where(n => n.Status == "Ativo")
                    .Select(n => n.UltimoAckSeq)
                    .ToListAsync();
                if (acksAtivos.Count > 0 && acksAtivos.Min() > 0) // sem no Ativo (ou algum sem ack) = nao apaga NADA
                {
                    var minAck = acksAtivos.Min();
                    // FASE 5b (achados A2+M1 da revisao): a marca persistida e' o MAIOR seq REALMENTE
                    // apagado (nao o min-ack inteiro — ops jovens <= min-ack sobrevivem ao corte de
                    // idade e um 409 com marca superestimada 'brickaria' no com dado completo). E ela
                    // e' gravada ANTES do delete (write-ahead): marca adiantada sem delete = 409 a
                    // mais (fail-closed); delete sem marca = lote parcial silencioso (o pior caso).
                    marcaRetencao = await _db.SyncFila
                        .Where(f => f.SeqEntrega != null && f.SeqEntrega <= minAck && f.CriadoEm < corte)
                        .MaxAsync(f => (long?)f.SeqEntrega) ?? 0;
                    if (marcaRetencao > 0)
                    {
                        var estadoMarca = await _db.SyncEstadoLocal.FirstOrDefaultAsync(e => e.Chave == "sync.retencao.marca");
                        if (estadoMarca == null)
                            _db.SyncEstadoLocal.Add(new SyncEstadoLocal { Chave = "sync.retencao.marca", Valor = marcaRetencao.ToString() });
                        else if (long.TryParse(estadoMarca.Valor, out var m) && marcaRetencao > m)
                        { estadoMarca.Valor = marcaRetencao.ToString(); estadoMarca.AtualizadoEm = DataHoraHelper.Agora(); }
                        await _db.SaveChangesAsync();

                        removidosRetencao = await _db.SyncFila
                            .Where(f => f.SeqEntrega != null && f.SeqEntrega <= marcaRetencao && f.CriadoEm < corte)
                            .ExecuteDeleteAsync();
                        Log.Warning("Sync: retencao do hub removeu {N} ops ate' a marca {Marca} (min ack dos Ativos: {MinAck}).",
                            removidosRetencao, marcaRetencao, minAck);
                    }
                }
            }

            return Ok(new { success = true, data = new { removidos, removidosRetencao, marcaRetencao, dias = efetivo } });
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
    // NOTA fase 1: NoOrigemId do body e' IGNORADO no servidor (a origem vem do token do no).
    Guid? OpUid = null
);

/// <summary>Credenciais de maquina do no (fase 1). InstanciaUid identifica a INSTALACAO (anti-gemeo).</summary>
public record SyncHandshakeDto(int NoCodigo, Guid InstanciaUid, string Chave, string? VersaoApp = null);

public record SyncNoCriarDto(int NoCodigo, string? Nome, List<long>? Filiais);

/// <summary>Fase 5: cursor pos-bootstrap (marca e geracao vindos do GET /bootstrap-info da central).</summary>
public record SyncCursorDto(long Cursor, string? Geracao);

public record SyncNoAtualizarDto(string? Nome, string? Status, List<long>? Filiais);
