using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

/// <summary>
/// Endpoints do programa Farmácia Popular (DATASUS).
/// Implementação em fases — esse controller hoje só expõe "testar-conexao" (smoke test do gbasmsb.exe).
/// Fases 1/2/3 + estorno + SONDA entram em commits futuros.
/// </summary>
[Authorize]
[ApiController]
[Route("api/farmacia-popular")]
public class FarmaciaPopularController : ControllerBase
{
    private readonly AppDbContext _db;

    public FarmaciaPopularController(AppDbContext db) => _db = db;

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
