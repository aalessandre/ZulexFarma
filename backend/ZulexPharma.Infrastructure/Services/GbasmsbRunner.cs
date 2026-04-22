using System.Diagnostics;
using Serilog;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Invoca o gbasmsb.exe (DATASUS) no servidor do ERP para gerar o dnaEstacao
/// exigido pela Fase 1 do Farmácia Popular. Sempre roda pontualmente — o dna
/// muda a cada execução (não-cacheável). Timeout duro de 5s; em falha joga
/// InvalidOperationException com mensagem acionável.
/// </summary>
public static class GbasmsbRunner
{
    public static async Task<string> ExecutarSolicitacaoAsync(string caminhoExe, string cpf, string cnpj, string crm, string uf, DateOnly dtEmissao, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(caminhoExe))
            throw new InvalidOperationException("Caminho do gbasmsb.exe não configurado (pbm.fp.caminho.gbasmsb).");
        if (!System.IO.File.Exists(caminhoExe))
            throw new InvalidOperationException($"gbasmsb.exe não encontrado em: {caminhoExe}");

        // Formato literal dd/MM/yyyy independente de culture do processo.
        var dataStr = $"{dtEmissao.Day:D2}/{dtEmissao.Month:D2}/{dtEmissao.Year:D4}";
        var gbasmArgs = $"--solicitacao {cpf} {cnpj} {crm} {uf} {dataStr}";
        // Invocamos via `cmd /c` em vez de Process.Start direto no exe. Motivo: o gbasmsb
        // aparentemente se comporta diferente quando filho direto do dotnet (sem console
        // ancestral) vs quando filho de um shell — gerando DNAs inválidos no primeiro caso.
        // Passando por cmd /c replicamos o ambiente do teste manual.
        var cmdArgs = $"/c \"\"{caminhoExe}\" {gbasmArgs}\"";

        Log.Information("gbasmsb.exe chamada | exe={Exe} | args=[{Args}]", caminhoExe, gbasmArgs);

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = cmdArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = System.IO.Path.GetDirectoryName(caminhoExe)
        };

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Falha ao iniciar gbasmsb.exe");
        var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = proc.StandardError.ReadToEndAsync(ct);

        var exited = await Task.Run(() => proc.WaitForExit(5000), ct);
        if (!exited)
        {
            try { proc.Kill(); } catch { }
            throw new InvalidOperationException("Timeout (5s) na execução do gbasmsb.exe. Verifique a instalação no servidor.");
        }

        var stdout = (await stdoutTask).Trim();
        var stderr = (await stderrTask).Trim();
        Log.Information("gbasmsb.exe resposta | exit={Exit} | stdoutLen={Len} | stdout={Out}", proc.ExitCode, stdout.Length, stdout);
        if (!string.IsNullOrEmpty(stderr)) Log.Warning("gbasmsb.exe stderr={Err}", stderr);
        if (proc.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout) || !stdout.Contains('|'))
        {
            Log.Error("gbasmsb.exe falhou. ExitCode={Code} stdout={Out} stderr={Err}", proc.ExitCode, stdout, stderr);
            throw new InvalidOperationException($"gbasmsb.exe retornou erro (exit={proc.ExitCode}). Detalhes: {stderr}");
        }
        return stdout;
    }
}
