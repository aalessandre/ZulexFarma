using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Fabricantes;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class FabricanteService : IFabricanteService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Fabricantes";
    private const string ENTIDADE = "Fabricante";

    public FabricanteService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<FabricanteListDto>> ListarAsync()
    {
        try
        {
            return await _db.Fabricantes.OrderBy(f => f.Nome)
                .Select(f => new FabricanteListDto { Id = f.Id, Codigo = f.Codigo, Nome = f.Nome, CriadoEm = f.CriadoEm, Ativo = f.Ativo, DescontoMinimo = f.DescontoMinimo, DescontoMaximo = f.DescontoMaximo, DescontoMaximoComSenha = f.DescontoMaximoComSenha })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em FabricanteService.ListarAsync"); throw; }
    }

    public async Task<FabricanteListDto> CriarAsync(FabricanteFormDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
            var fab = new Fabricante { Nome = dto.Nome.Trim().ToUpper(), Ativo = dto.Ativo, DescontoMinimo = dto.DescontoMinimo, DescontoMaximo = dto.DescontoMaximo, DescontoMaximoComSenha = dto.DescontoMaximoComSenha };
            _db.Fabricantes.Add(fab);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, fab.Id, novo: ParaDict(fab));
            return new FabricanteListDto { Id = fab.Id, Codigo = fab.Codigo, Nome = fab.Nome, CriadoEm = fab.CriadoEm, Ativo = fab.Ativo, DescontoMinimo = fab.DescontoMinimo, DescontoMaximo = fab.DescontoMaximo, DescontoMaximoComSenha = fab.DescontoMaximoComSenha };
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em FabricanteService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, FabricanteFormDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new ArgumentException("Nome é obrigatório.");
            var fab = await _db.Fabricantes.FindAsync(id) ?? throw new KeyNotFoundException($"Fabricante {id} não encontrado.");
            var anterior = ParaDict(fab);
            fab.Nome = dto.Nome.Trim().ToUpper();
            fab.Ativo = dto.Ativo;
            fab.DescontoMinimo = dto.DescontoMinimo;
            fab.DescontoMaximo = dto.DescontoMaximo;
            fab.DescontoMaximoComSenha = dto.DescontoMaximoComSenha;
            await _db.SaveChangesAsync();
            var novo = ParaDict(fab);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em FabricanteService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var fab = await _db.Fabricantes.FindAsync(id) ?? throw new KeyNotFoundException($"Fabricante {id} não encontrado.");
            var dados = ParaDict(fab);
            _db.Fabricantes.Remove(fab);
            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var recarregado = await _db.Fabricantes.FindAsync(id);
                recarregado!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException) { Log.Error(ex, "Erro em FabricanteService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    private static Dictionary<string, string?> ParaDict(Fabricante f) => new() { ["Nome"] = f.Nome, ["Ativo"] = f.Ativo ? "Sim" : "Não", ["DescontoMinimo"] = f.DescontoMinimo.ToString("F2"), ["DescontoMaximo"] = f.DescontoMaximo.ToString("F2"), ["DescontoMaximoComSenha"] = f.DescontoMaximoComSenha.ToString("F2") };
    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b) => a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
