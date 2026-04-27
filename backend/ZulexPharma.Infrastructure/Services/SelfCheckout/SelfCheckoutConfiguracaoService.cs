using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.SelfCheckout;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities.SelfCheckout;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services.SelfCheckout;

public class SelfCheckoutConfiguracaoService : ISelfCheckoutConfiguracaoService
{
    private readonly AppDbContext _db;

    public SelfCheckoutConfiguracaoService(AppDbContext db) { _db = db; }

    public async Task<SelfCheckoutConfiguracaoDto?> ObterPorFilialAsync(long filialId, CancellationToken ct = default)
    {
        var cfg = await _db.SelfCheckoutConfiguracoes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.FilialId == filialId, ct);

        return cfg == null ? null : ToDto(cfg);
    }

    public async Task<SelfCheckoutConfiguracaoDto> SalvarAsync(long filialId, SelfCheckoutConfiguracaoFormDto form, CancellationToken ct = default)
    {
        ValidarForm(form, criando: false);

        var existe = await _db.Filiais.AnyAsync(f => f.Id == filialId, ct);
        if (!existe) throw new InvalidOperationException($"Filial {filialId} não encontrada.");

        var cfg = await _db.SelfCheckoutConfiguracoes
            .FirstOrDefaultAsync(c => c.FilialId == filialId, ct);

        if (cfg == null)
        {
            // Criação: senha obrigatória
            if (string.IsNullOrWhiteSpace(form.SenhaBanco))
                throw new InvalidOperationException("Senha do banco do ERP origem é obrigatória.");

            cfg = new SelfCheckoutConfiguracao
            {
                FilialId = filialId,
                ErpOrigem = form.ErpOrigem,
                HostBanco = form.HostBanco.Trim(),
                NomeBanco = form.NomeBanco.Trim(),
                UsuarioBanco = form.UsuarioBanco.Trim(),
                SenhaBancoCriptografada = CriptografiaHelper.Encrypt(form.SenhaBanco) ?? string.Empty,
                FilialErpOrigem = form.FilialErpOrigem.Trim(),
                CodigoNaturezaOperacaoNfce = form.CodigoNaturezaOperacaoNfce,
                UsuarioVirtualId = form.UsuarioVirtualId,
                Ativo = form.Ativo
            };
            _db.SelfCheckoutConfiguracoes.Add(cfg);
        }
        else
        {
            cfg.ErpOrigem = form.ErpOrigem;
            cfg.HostBanco = form.HostBanco.Trim();
            cfg.NomeBanco = form.NomeBanco.Trim();
            cfg.UsuarioBanco = form.UsuarioBanco.Trim();
            cfg.FilialErpOrigem = form.FilialErpOrigem.Trim();
            cfg.CodigoNaturezaOperacaoNfce = form.CodigoNaturezaOperacaoNfce;
            cfg.UsuarioVirtualId = form.UsuarioVirtualId;
            cfg.Ativo = form.Ativo;
            cfg.AtualizadoEm = DataHoraHelper.Agora();

            // Senha vazia mantém a antiga; preenchida substitui.
            if (!string.IsNullOrWhiteSpace(form.SenhaBanco))
                cfg.SenhaBancoCriptografada = CriptografiaHelper.Encrypt(form.SenhaBanco) ?? string.Empty;
        }

        await _db.SaveChangesAsync(ct);
        return ToDto(cfg);
    }

    public async Task<List<SelfCheckoutTerminalDto>> ListarTerminaisAsync(long filialId, CancellationToken ct = default)
    {
        return await _db.SelfCheckoutTerminais
            .AsNoTracking()
            .Where(t => t.FilialId == filialId)
            .OrderBy(t => t.Numero)
            .Select(t => new SelfCheckoutTerminalDto
            {
                Id = t.Id, FilialId = t.FilialId, Numero = t.Numero,
                Apelido = t.Apelido, Ativo = t.Ativo, UltimaAtividade = t.UltimaAtividade
            })
            .ToListAsync(ct);
    }

    public async Task<SelfCheckoutTerminalDto> CriarTerminalAsync(long filialId, SelfCheckoutTerminalFormDto form, CancellationToken ct = default)
    {
        if (form.Numero <= 0) throw new InvalidOperationException("Número do terminal deve ser positivo.");

        var duplicado = await _db.SelfCheckoutTerminais
            .AnyAsync(t => t.FilialId == filialId && t.Numero == form.Numero, ct);
        if (duplicado)
            throw new InvalidOperationException($"Já existe terminal com número {form.Numero} nesta filial.");

        var terminal = new SelfCheckoutTerminal
        {
            FilialId = filialId,
            Numero = form.Numero,
            Apelido = form.Apelido?.Trim(),
            Ativo = form.Ativo
        };
        _db.SelfCheckoutTerminais.Add(terminal);
        await _db.SaveChangesAsync(ct);
        return ToDto(terminal);
    }

    public async Task<SelfCheckoutTerminalDto> AtualizarTerminalAsync(long terminalId, SelfCheckoutTerminalFormDto form, CancellationToken ct = default)
    {
        var terminal = await _db.SelfCheckoutTerminais.FirstOrDefaultAsync(t => t.Id == terminalId, ct)
            ?? throw new InvalidOperationException($"Terminal {terminalId} não encontrado.");

        if (form.Numero <= 0) throw new InvalidOperationException("Número do terminal deve ser positivo.");

        if (form.Numero != terminal.Numero)
        {
            var duplicado = await _db.SelfCheckoutTerminais
                .AnyAsync(t => t.FilialId == terminal.FilialId && t.Numero == form.Numero && t.Id != terminalId, ct);
            if (duplicado)
                throw new InvalidOperationException($"Já existe terminal com número {form.Numero} nesta filial.");
        }

        terminal.Numero = form.Numero;
        terminal.Apelido = form.Apelido?.Trim();
        terminal.Ativo = form.Ativo;
        terminal.AtualizadoEm = DataHoraHelper.Agora();

        await _db.SaveChangesAsync(ct);
        return ToDto(terminal);
    }

    public async Task RemoverTerminalAsync(long terminalId, CancellationToken ct = default)
    {
        var terminal = await _db.SelfCheckoutTerminais.FirstOrDefaultAsync(t => t.Id == terminalId, ct)
            ?? throw new InvalidOperationException($"Terminal {terminalId} não encontrado.");
        _db.SelfCheckoutTerminais.Remove(terminal);
        await _db.SaveChangesAsync(ct);
    }

    // ── helpers ───────────────────────────────────────────────────
    private static void ValidarForm(SelfCheckoutConfiguracaoFormDto form, bool criando)
    {
        if (string.IsNullOrWhiteSpace(form.HostBanco))
            throw new InvalidOperationException("Host do banco é obrigatório.");
        if (string.IsNullOrWhiteSpace(form.NomeBanco))
            throw new InvalidOperationException("Nome do banco é obrigatório.");
        if (string.IsNullOrWhiteSpace(form.UsuarioBanco))
            throw new InvalidOperationException("Usuário do banco é obrigatório.");
        if (string.IsNullOrWhiteSpace(form.FilialErpOrigem))
            throw new InvalidOperationException("Filial do ERP origem é obrigatória.");
    }

    private static SelfCheckoutConfiguracaoDto ToDto(SelfCheckoutConfiguracao c) => new()
    {
        Id = c.Id, FilialId = c.FilialId, ErpOrigem = c.ErpOrigem,
        HostBanco = c.HostBanco, NomeBanco = c.NomeBanco, UsuarioBanco = c.UsuarioBanco,
        FilialErpOrigem = c.FilialErpOrigem,
        CodigoNaturezaOperacaoNfce = c.CodigoNaturezaOperacaoNfce,
        UsuarioVirtualId = c.UsuarioVirtualId, Ativo = c.Ativo,
        TemSenhaCadastrada = !string.IsNullOrEmpty(c.SenhaBancoCriptografada)
    };

    private static SelfCheckoutTerminalDto ToDto(SelfCheckoutTerminal t) => new()
    {
        Id = t.Id, FilialId = t.FilialId, Numero = t.Numero,
        Apelido = t.Apelido, Ativo = t.Ativo, UltimaAtividade = t.UltimaAtividade
    };
}
