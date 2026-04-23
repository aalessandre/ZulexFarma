using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

/// <summary>
/// Endpoints do programa Farmácia Popular (DATASUS).
/// </summary>
[Authorize]
[ApiController]
[Route("api/farmacia-popular")]
public class FarmaciaPopularController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFarmaciaPopularService _fp;

    public FarmaciaPopularController(AppDbContext db, IFarmaciaPopularService fp) { _db = db; _fp = fp; }

    /// <summary>
    /// Retorna os XMLs request/response de cada fase já persistidos em VendaFarmaciaPopular.
    /// Usado para debug quando a Fase 1 é disparada via Finalizar e dá erro — os XMLs são
    /// salvos antes do throw, mas a UI só mostra a mensagem de erro.
    /// </summary>
    [HttpGet("xmls/{vendaId:long}")]
    public async Task<IActionResult> ObterXmls(long vendaId)
    {
        try
        {
            var fp = await _db.Set<Domain.Entities.VendaFarmaciaPopular>()
                .Include(x => x.Itens)
                .FirstOrDefaultAsync(x => x.VendaId == vendaId);
            if (fp == null) return NotFound(new { success = false, message = "Venda sem VendaFarmaciaPopular." });
            return Ok(new
            {
                success = true,
                data = new
                {
                    vendaId = fp.VendaId,
                    coSolicitacaoFarmacia = fp.CoSolicitacaoFarmacia,
                    nuAutorizacao = fp.NuAutorizacao,
                    status = fp.Status.ToString(),
                    faseAtual = fp.FaseAtual.ToString(),
                    codigoRetornoAtual = fp.CodigoRetornoAtual,
                    mensagemRetornoAtual = fp.MensagemRetornoAtual,
                    dnaEstacao = fp.DnaEstacao,
                    fase1 = new { req = fp.Fase1RequestXml, resp = fp.Fase1ResponseXml, dh = fp.Fase1DataHora },
                    fase2 = new { req = fp.Fase2RequestXml, resp = fp.Fase2ResponseXml, dh = fp.Fase2DataHora },
                    fase3 = new { req = fp.Fase3RequestXml, resp = fp.Fase3ResponseXml, dh = fp.Fase3DataHora },
                    estorno = new { req = fp.EstornoRequestXml, resp = fp.EstornoResponseXml, dh = fp.EstornoDataHora },
                    itens = fp.Itens.Select(i => new { i.CodigoBarraEAN, i.QtSolicitada, i.VlPrecoVenda, i.QtAutorizada, i.CodigoRetornoItem, i.MensagemRetornoItem })
                }
            });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em FarmaciaPopular.ObterXmls"); return StatusCode(500, new { success = false, message = ex.Message }); }
    }

    /// <summary>
    /// Dispara SÓ a Fase 1 (executarSolicitacao) de uma venda em aberto — sem marcar finalizada,
    /// sem emitir NFC-e. Usado para testar a integração DATASUS isoladamente. Retorna o XML
    /// request e response para debug.
    ///
    /// Body opcional { "dnaEstacao": "W1|FPC1..." } pula a invocação do gbasmsb.exe e usa o DNA
    /// fornecido. Diagnóstico — pra testar se o problema é a forma como a API chama o gbasmsb.
    /// </summary>
    [HttpPost("pre-autorizar/{vendaId:long}")]
    [AllowAnonymous] // TEMP diagnóstico: deixa o Swagger chamar sem auth enquanto debugamos 51S.
    public async Task<IActionResult> PreAutorizar(long vendaId, [FromBody] PreAutorizarBodyDto? body = null)
    {
        try
        {
            var ret = await _fp.SolicitarAsync(vendaId, body?.DnaEstacao);
            var fp = await _db.Set<Domain.Entities.VendaFarmaciaPopular>()
                .Include(x => x.Itens)
                .FirstOrDefaultAsync(x => x.VendaId == vendaId);
            return Ok(new
            {
                success = ret.Sucesso,
                codigo = ret.CodigoRetorno,
                mensagem = ret.MensagemRetorno,
                nuAutorizacao = ret.NuAutorizacao,
                noPaciente = ret.NoPaciente,
                coSolicitacaoFarmacia = fp?.CoSolicitacaoFarmacia,
                dnaEstacao = fp?.DnaEstacao,
                status = fp?.Status.ToString(),
                itens = ret.Itens,
                requestXml = ret.RequestXml,
                responseXml = ret.ResponseXml
            });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (Exception ex) { Log.Error(ex, "Erro em FarmaciaPopular.PreAutorizar"); return StatusCode(500, new { success = false, message = ex.Message }); }
    }

    /// <summary>
    /// Smoke test — invoca o gbasmsb.exe com parâmetros dummy pra validar
    /// que o executável está acessível e retorna um dnaEstacao válido.
    /// Não chama o DATASUS.
    /// </summary>
    [HttpPost("testar-conexao")]
    public async Task<IActionResult> TestarConexao()
    {
        try
        {
            var configs = await _db.Set<Domain.Entities.Configuracao>().ToDictionaryAsync(c => c.Chave, c => c.Valor);
            var caminhoGbas = configs.GetValueOrDefault("pbm.fp.caminho.gbasmsb", "");
            if (string.IsNullOrWhiteSpace(caminhoGbas))
                return BadRequest(new { success = false, message = "Caminho do gbasmsb.exe não configurado (pbm.fp.caminho.gbasmsb)." });

            if (!System.IO.File.Exists(caminhoGbas))
                return BadRequest(new { success = false, message = $"gbasmsb.exe não encontrado em: {caminhoGbas}" });

            var cnpj = configs.GetValueOrDefault("pbm.fp.cnpj", "00000000000000");
            var dataStr = DateTime.Now.ToString("dd/MM/yyyy");
            var args = $"--solicitacao 00000000000 {cnpj} 00000 PR {dataStr}";

            var psi = new ProcessStartInfo
            {
                FileName = caminhoGbas,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = System.IO.Path.GetDirectoryName(caminhoGbas)
            };

            using var proc = Process.Start(psi)!;
            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();
            var exited = proc.WaitForExit(5000);

            if (!exited)
            {
                try { proc.Kill(); } catch { }
                return StatusCode(500, new { success = false, message = "Timeout na execução do gbasmsb.exe (5s)." });
            }

            var sucesso = proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(stdout) && stdout.Contains('|');

            var msg = sucesso
                ? $"gbasmsb.exe respondeu OK. Tamanho do dnaEstacao: {stdout.Trim().Length} chars."
                : $"Falha. ExitCode={proc.ExitCode}. stdout={stdout.Substring(0, Math.Min(100, stdout.Length))}. stderr={stderr.Substring(0, Math.Min(200, stderr.Length))}";

            // Atualiza último teste
            await SetConfigAsync("pbm.fp.ultimo.teste.data", DateTime.UtcNow.ToString("o"));
            await SetConfigAsync("pbm.fp.ultimo.teste.mensagem", msg);

            return Ok(new { success = sucesso, message = msg });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FarmaciaPopular.TestarConexao");
            return StatusCode(500, new { success = false, message = $"Erro: {ex.Message}" });
        }
    }

    private async Task SetConfigAsync(string chave, string valor)
    {
        var existente = await _db.Set<Domain.Entities.Configuracao>().FirstOrDefaultAsync(c => c.Chave == chave);
        if (existente != null) existente.Valor = valor;
        else _db.Set<Domain.Entities.Configuracao>().Add(new Domain.Entities.Configuracao { Chave = chave, Valor = valor });
        await _db.SaveChangesAsync();
    }
}

public class PreAutorizarBodyDto
{
    /// <summary>DNA gerado fora da API (ex: copiado do PowerShell). Se presente, pula o gbasmsb interno.</summary>
    public string? DnaEstacao { get; set; }
}
