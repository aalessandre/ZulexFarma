using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Prescritores;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class PrescritorService : IPrescritorService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Prescritores";
    private const string ENTIDADE = "Prescritor";

    public PrescritorService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<PrescritorListDto>> ListarAsync()
    {
        try
        {
            return await _db.Prescritores.OrderBy(p => p.Nome)
                .Select(p => ToList(p))
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em PrescritorService.ListarAsync"); throw; }
    }

    public async Task<PrescritorListDto?> ObterAsync(long id)
    {
        try
        {
            var p = await _db.Prescritores.FindAsync(id);
            return p == null ? null : ToList(p);
        }
        catch (Exception ex) { Log.Error(ex, "Erro em PrescritorService.ObterAsync | Id: {Id}", id); throw; }
    }

    public async Task<PrescritorListDto> CriarAsync(PrescritorFormDto dto)
    {
        try
        {
            Validar(dto);
            var p = new Prescritor
            {
                Nome = dto.Nome.Trim().ToUpper(),
                TipoConselho = dto.TipoConselho.Trim().ToUpper(),
                NumeroConselho = dto.NumeroConselho.Trim(),
                Uf = dto.Uf.Trim().ToUpper(),
                Cpf = SoDigitosOuNull(dto.Cpf),
                Especialidade = string.IsNullOrWhiteSpace(dto.Especialidade) ? null : dto.Especialidade.Trim().ToUpper(),
                Telefone = string.IsNullOrWhiteSpace(dto.Telefone) ? null : dto.Telefone.Trim(),
                Ativo = dto.Ativo
            };
            _db.Prescritores.Add(p);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, p.Id, novo: ToDict(p));
            return ToList(p);
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em PrescritorService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, PrescritorFormDto dto)
    {
        try
        {
            Validar(dto);
            var p = await _db.Prescritores.FindAsync(id) ?? throw new KeyNotFoundException($"Prescritor {id} não encontrado.");
            var anterior = ToDict(p);
            p.Nome = dto.Nome.Trim().ToUpper();
            p.TipoConselho = dto.TipoConselho.Trim().ToUpper();
            p.NumeroConselho = dto.NumeroConselho.Trim();
            p.Uf = dto.Uf.Trim().ToUpper();
            p.Cpf = SoDigitosOuNull(dto.Cpf);
            p.Especialidade = string.IsNullOrWhiteSpace(dto.Especialidade) ? null : dto.Especialidade.Trim().ToUpper();
            p.Telefone = string.IsNullOrWhiteSpace(dto.Telefone) ? null : dto.Telefone.Trim();
            p.Ativo = dto.Ativo;
            await _db.SaveChangesAsync();
            var novo = ToDict(p);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em PrescritorService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var p = await _db.Prescritores.FindAsync(id) ?? throw new KeyNotFoundException($"Prescritor {id} não encontrado.");
            var dados = ToDict(p);
            _db.Prescritores.Remove(p);
            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var recarregado = await _db.Prescritores.FindAsync(id);
                recarregado!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException) { Log.Error(ex, "Erro em PrescritorService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    private static void Validar(PrescritorFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(dto.TipoConselho)) throw new ArgumentException("Tipo do conselho é obrigatório.");
        if (string.IsNullOrWhiteSpace(dto.NumeroConselho)) throw new ArgumentException("Número do conselho é obrigatório.");
        if (string.IsNullOrWhiteSpace(dto.Uf) || dto.Uf.Trim().Length != 2) throw new ArgumentException("UF inválida.");
    }

    private static string? SoDigitosOuNull(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        var s = new string(v.Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static PrescritorListDto ToList(Prescritor p) => new()
    {
        Id = p.Id,
        Codigo = p.Codigo,
        Nome = p.Nome,
        TipoConselho = p.TipoConselho,
        NumeroConselho = p.NumeroConselho,
        Uf = p.Uf,
        Cpf = p.Cpf,
        Especialidade = p.Especialidade,
        Telefone = p.Telefone,
        CriadoEm = p.CriadoEm,
        Ativo = p.Ativo
    };

    private static Dictionary<string, string?> ToDict(Prescritor p) => new()
    {
        ["Nome"] = p.Nome,
        ["Tipo Conselho"] = p.TipoConselho,
        ["Nº Conselho"] = p.NumeroConselho,
        ["UF"] = p.Uf,
        ["CPF"] = p.Cpf,
        ["Especialidade"] = p.Especialidade,
        ["Telefone"] = p.Telefone,
        ["Ativo"] = p.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b)
        => a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
