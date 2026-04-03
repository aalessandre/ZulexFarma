using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Fiscal;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class IcmsUfService : IIcmsUfService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;

    private const string TELA = "ICMS UF";
    private const string ENTIDADE = "IcmsUf";

    public IcmsUfService(AppDbContext db, ILogAcaoService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<List<IcmsUfListDto>> ListarAsync()
    {
        return await _db.IcmsUfs
            .OrderBy(x => x.NomeEstado)
            .Select(x => new IcmsUfListDto
            {
                Id = x.Id,
                Uf = x.Uf,
                NomeEstado = x.NomeEstado,
                AliquotaInterna = x.AliquotaInterna,
                Ativo = x.Ativo,
                CriadoEm = x.CriadoEm
            })
            .ToListAsync();
    }

    public async Task<IcmsUfListDto> ObterAsync(long id)
    {
        var x = await _db.IcmsUfs.FindAsync(id)
            ?? throw new KeyNotFoundException($"ICMS UF {id} não encontrado.");
        return new IcmsUfListDto
        {
            Id = x.Id, Uf = x.Uf, NomeEstado = x.NomeEstado,
            AliquotaInterna = x.AliquotaInterna, Ativo = x.Ativo, CriadoEm = x.CriadoEm
        };
    }

    public async Task<IcmsUfListDto> CriarAsync(IcmsUfFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Uf) || dto.Uf.Trim().Length != 2)
            throw new ArgumentException("UF deve ter 2 caracteres.");

        var uf = dto.Uf.Trim().ToUpper();
        if (await _db.IcmsUfs.AnyAsync(x => x.Uf == uf))
            throw new ArgumentException($"UF '{uf}' já cadastrada.");

        var e = new IcmsUf
        {
            Uf = uf,
            NomeEstado = dto.NomeEstado.Trim().ToUpper(),
            AliquotaInterna = dto.AliquotaInterna,
            Ativo = dto.Ativo
        };
        _db.IcmsUfs.Add(e);
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, e.Id,
            novo: new Dictionary<string, string?> {
                ["UF"] = e.Uf, ["Estado"] = e.NomeEstado, ["Alíquota"] = e.AliquotaInterna.ToString("N2")
            });

        return await ObterAsync(e.Id);
    }

    public async Task AtualizarAsync(long id, IcmsUfFormDto dto)
    {
        var e = await _db.IcmsUfs.FindAsync(id)
            ?? throw new KeyNotFoundException($"ICMS UF {id} não encontrado.");

        var anterior = new Dictionary<string, string?> {
            ["UF"] = e.Uf, ["Estado"] = e.NomeEstado,
            ["Alíquota"] = e.AliquotaInterna.ToString("N2"), ["Ativo"] = e.Ativo ? "Sim" : "Não"
        };

        e.Uf = dto.Uf.Trim().ToUpper();
        e.NomeEstado = dto.NomeEstado.Trim().ToUpper();
        e.AliquotaInterna = dto.AliquotaInterna;
        e.Ativo = dto.Ativo;
        await _db.SaveChangesAsync();

        var novo = new Dictionary<string, string?> {
            ["UF"] = e.Uf, ["Estado"] = e.NomeEstado,
            ["Alíquota"] = e.AliquotaInterna.ToString("N2"), ["Ativo"] = e.Ativo ? "Sim" : "Não"
        };

        if (!anterior.All(kv => novo.TryGetValue(kv.Key, out var v) && v == kv.Value))
            await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
    }

    public async Task<string> ExcluirAsync(long id)
    {
        var e = await _db.IcmsUfs.FindAsync(id)
            ?? throw new KeyNotFoundException($"ICMS UF {id} não encontrado.");

        _db.IcmsUfs.Remove(e);
        try
        {
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id,
                anterior: new Dictionary<string, string?> { ["UF"] = e.Uf, ["Estado"] = e.NomeEstado });
            return "excluido";
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            var recarregado = await _db.IcmsUfs.FirstAsync(x => x.Id == id);
            recarregado.Ativo = false;
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
            return "desativado";
        }
    }
}
