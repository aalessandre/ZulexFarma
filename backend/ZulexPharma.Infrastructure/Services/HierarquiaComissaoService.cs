using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.HierarquiaComissoes;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class HierarquiaComissaoService : IHierarquiaComissaoService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Hierarquia de Comissão";
    private const string ENTIDADE = "HierarquiaComissao";

    public HierarquiaComissaoService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<HierarquiaComissaoListDto>> ListarAsync()
    {
        try
        {
            return await _db.Set<HierarquiaComissao>()
                .Include(h => h.Itens)
                .OrderBy(h => h.Nome)
                .Select(h => new HierarquiaComissaoListDto
                {
                    Id = h.Id, Nome = h.Nome, Padrao = h.Padrao,
                    TotalItens = h.Itens.Count,
                    Ativo = h.Ativo, CriadoEm = h.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em HierarquiaComissaoService.ListarAsync"); throw; }
    }

    public async Task<HierarquiaComissaoDetalheDto?> ObterAsync(long id)
    {
        try
        {
            var h = await _db.Set<HierarquiaComissao>()
                .Include(x => x.Itens).ThenInclude(i => i.Secoes)
                .Include(x => x.Colaboradores)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (h == null) return null;

            return new HierarquiaComissaoDetalheDto
            {
                Id = h.Id, Nome = h.Nome, Padrao = h.Padrao,
                Ativo = h.Ativo, CriadoEm = h.CriadoEm,
                Itens = h.Itens.OrderBy(i => i.Ordem).Select(i => new HierarquiaComissaoItemDto
                {
                    Ordem = i.Ordem, Componente = i.Componente,
                    SecaoIds = i.Secoes.Select(s => s.SecaoId).ToList()
                }).ToList(),
                ColaboradorIds = h.Colaboradores.Select(c => c.ColaboradorId).ToList()
            };
        }
        catch (Exception ex) { Log.Error(ex, "Erro em HierarquiaComissaoService.ObterAsync | Id: {Id}", id); throw; }
    }

    public async Task<HierarquiaComissaoListDto> CriarAsync(HierarquiaComissaoFormDto dto)
    {
        try
        {
            Validar(dto);
            if (dto.Padrao) await DesmarcarPadraoExistente();

            var h = new HierarquiaComissao
            {
                Nome = dto.Nome.Trim().ToUpper(), Padrao = dto.Padrao,
                Ativo = dto.Ativo
            };

            MapearSubTabelas(h, dto);
            _db.Set<HierarquiaComissao>().Add(h);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, h.Id, novo: ParaDict(h));

            return new HierarquiaComissaoListDto { Id = h.Id, Nome = h.Nome, Padrao = h.Padrao, TotalItens = h.Itens.Count, Ativo = h.Ativo, CriadoEm = h.CriadoEm };
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em HierarquiaComissaoService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, HierarquiaComissaoFormDto dto)
    {
        try
        {
            Validar(dto);
            var h = await _db.Set<HierarquiaComissao>()
                .Include(x => x.Itens).ThenInclude(i => i.Secoes)
                .Include(x => x.Colaboradores)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException($"Hierarquia {id} não encontrada.");

            if (dto.Padrao && !h.Padrao) await DesmarcarPadraoExistente();

            var anterior = ParaDict(h);
            h.Nome = dto.Nome.Trim().ToUpper(); h.Padrao = dto.Padrao;
            h.Ativo = dto.Ativo;

            // Limpar e recriar
            foreach (var item in h.Itens) _db.Set<HierarquiaComissaoSecao>().RemoveRange(item.Secoes);
            _db.Set<HierarquiaComissaoItem>().RemoveRange(h.Itens);
            _db.Set<HierarquiaComissaoColaborador>().RemoveRange(h.Colaboradores);

            MapearSubTabelas(h, dto);
            await _db.SaveChangesAsync();

            var novo = ParaDict(h);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em HierarquiaComissaoService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var h = await _db.Set<HierarquiaComissao>()
                .Include(x => x.Itens).ThenInclude(i => i.Secoes)
                .Include(x => x.Colaboradores)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException($"Hierarquia {id} não encontrada.");
            var dados = ParaDict(h);
            foreach (var item in h.Itens) _db.Set<HierarquiaComissaoSecao>().RemoveRange(item.Secoes);
            _db.Set<HierarquiaComissaoItem>().RemoveRange(h.Itens);
            _db.Set<HierarquiaComissaoColaborador>().RemoveRange(h.Colaboradores);
            _db.Set<HierarquiaComissao>().Remove(h);
            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var rec = await _db.Set<HierarquiaComissao>().FindAsync(id);
                rec!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException) { Log.Error(ex, "Erro em HierarquiaComissaoService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    private static void MapearSubTabelas(HierarquiaComissao h, HierarquiaComissaoFormDto dto)
    {
        foreach (var item in dto.Itens)
        {
            var hi = new HierarquiaComissaoItem { Ordem = item.Ordem, Componente = item.Componente };
            if (item.Componente == ComponenteComissao.SecaoEscolhida)
                foreach (var sid in item.SecaoIds)
                    hi.Secoes.Add(new HierarquiaComissaoSecao { SecaoId = sid });
            h.Itens.Add(hi);
        }
        foreach (var cid in dto.ColaboradorIds.Distinct()) h.Colaboradores.Add(new HierarquiaComissaoColaborador { ColaboradorId = cid });
    }

    private async Task DesmarcarPadraoExistente()
    {
        var existente = await _db.Set<HierarquiaComissao>().Where(h => h.Padrao).ToListAsync();
        foreach (var h in existente) h.Padrao = false;
    }

    private static void Validar(HierarquiaComissaoFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
        if (dto.Itens.Count == 0) throw new ArgumentException("Adicione ao menos um componente na hierarquia.");
    }

    private static Dictionary<string, string?> ParaDict(HierarquiaComissao h) => new()
    {
        ["Nome"] = h.Nome, ["Padrao"] = h.Padrao ? "Sim" : "Não",
        ["Itens"] = h.Itens.Count.ToString(), ["Ativo"] = h.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b) =>
        a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
