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
        var pasta = System.IO.Path.GetDirectoryName(caminhoExe)!;

        // Log do ambiente para comparação com execução manual no PowerShell.
        Log.Information("gbasmsb.exe ambiente | USER={User} | SID={Sid} | APPDATA={AppData} | LOCALAPPDATA={LocalAppData} | USERPROFILE={UserProfile} | PWD={Pwd}",
            Environment.UserName,
            System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value ?? "?",
            Environment.GetEnvironmentVariable("APPDATA") ?? "?",
            Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "?",
            Environment.GetEnvironmentVariable("USERPROFILE") ?? "?",
            Environment.CurrentDirectory);

        // Estratégia: gera um .bat temporário na pasta do gbasmsb com `cd /d` + chamada
        // + redirect para arquivo. Executa o .bat via cmd /c. Isso dá ao gbasmsb um
        // console próprio ancestral + working dir correto + stdout via arquivo (ao invés
        // de pipe, que aparentemente altera o comportamento do exe).
        var tempId = Guid.NewGuid().ToString("N")[..8];
        var outFile = System.IO.Path.Combine(pasta, $"gbasmsb-out-{tempId}.txt");
        var batFile = System.IO.Path.Combine(pasta, $"gbasmsb-run-{tempId}.bat");
        var batContent = $"@echo off\r\ncd /d \"{pasta}\"\r\n\"{caminhoExe}\" {gbasmArgs} > \"{outFile}\"\r\n";
        await System.IO.File.WriteAllTextAsync(batFile, batContent, System.Text.Encoding.ASCII, ct);

        Log.Information("gbasmsb.exe chamada | exe={Exe} | args=[{Args}] | bat={Bat}", caminhoExe, gbasmArgs, batFile);

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{batFile}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = pasta,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };

        try
        {
            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Falha ao iniciar gbasmsb.exe");
            var exited = await Task.Run(() => proc.WaitForExit(10000), ct);
            if (!exited)
            {
                try { proc.Kill(); } catch { }
                throw new InvalidOperationException("Timeout (10s) na execução do gbasmsb.exe. Verifique a instalação no servidor.");
            }

            if (!System.IO.File.Exists(outFile))
                throw new InvalidOperationException($"gbasmsb.exe não produziu output (exit={proc.ExitCode}).");

            var stdout = (await System.IO.File.ReadAllTextAsync(outFile, ct)).Trim();
            Log.Information("gbasmsb.exe resposta | exit={Exit} | stdoutLen={Len} | stdout={Out}", proc.ExitCode, stdout.Length, stdout);

            if (proc.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout) || !stdout.Contains('|'))
            {
                Log.Error("gbasmsb.exe falhou. ExitCode={Code} stdout={Out}", proc.ExitCode, stdout);
                throw new InvalidOperationException($"gbasmsb.exe retornou erro (exit={proc.ExitCode}).");
            }
            return stdout;
        }
        finally
        {
            try { if (System.IO.File.Exists(batFile)) System.IO.File.Delete(batFile); } catch { }
            try { if (System.IO.File.Exists(outFile)) System.IO.File.Delete(outFile); } catch { }
        }
    }
}
