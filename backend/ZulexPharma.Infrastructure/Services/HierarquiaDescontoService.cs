using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.HierarquiaDescontos;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class HierarquiaDescontoService : IHierarquiaDescontoService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Hierarquia de Descontos";
    private const string ENTIDADE = "HierarquiaDesconto";

    public HierarquiaDescontoService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<HierarquiaDescontoListDto>> ListarAsync()
    {
        try
        {
            return await _db.Set<HierarquiaDesconto>()
                .Include(h => h.Itens)
                .OrderBy(h => h.Nome)
                .Select(h => new HierarquiaDescontoListDto
                {
                    Id = h.Id, Nome = h.Nome, Padrao = h.Padrao,
                    AplicarAutomatico = h.AplicarAutomatico, TotalItens = h.Itens.Count,
                    Ativo = h.Ativo, CriadoEm = h.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em HierarquiaDescontoService.ListarAsync"); throw; }
    }

    public async Task<HierarquiaDescontoDetalheDto?> ObterAsync(long id)
    {
        try
        {
            var h = await _db.Set<HierarquiaDesconto>()
                .Include(x => x.Itens).ThenInclude(i => i.Secoes)
                .Include(x => x.Colaboradores)
                .Include(x => x.Convenios)
                .Include(x => x.Clientes)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (h == null) return null;

            return new HierarquiaDescontoDetalheDto
            {
                Id = h.Id, Nome = h.Nome, Padrao = h.Padrao,
                AplicarAutomatico = h.AplicarAutomatico, DescontoAutoTipo = h.DescontoAutoTipo,
                BuscarMenorValorPromocao = h.BuscarMenorValorPromocao,
                Ativo = h.Ativo, CriadoEm = h.CriadoEm,
                Itens = h.Itens.OrderBy(i => i.Ordem).Select(i => new HierarquiaItemDto
                {
                    Ordem = i.Ordem, Componente = i.Componente,
                    SecaoIds = i.Secoes.Select(s => s.SecaoId).ToList()
                }).ToList(),
                ColaboradorIds = h.Colaboradores.Select(c => c.ColaboradorId).ToList(),
                ConvenioIds = h.Convenios.Select(c => c.ConvenioId).ToList(),
                ClienteIds = h.Clientes.Select(c => c.ClienteId).ToList()
            };
        }
        catch (Exception ex) { Log.Error(ex, "Erro em HierarquiaDescontoService.ObterAsync | Id: {Id}", id); throw; }
    }

    public async Task<HierarquiaDescontoListDto> CriarAsync(HierarquiaDescontoFormDto dto)
    {
        try
        {
            Validar(dto);
            if (dto.Padrao) await DesmarcarPadraoExistente();

            var h = new HierarquiaDesconto
            {
                Nome = dto.Nome.Trim().ToUpper(), Padrao = dto.Padrao,
                AplicarAutomatico = dto.AplicarAutomatico, DescontoAutoTipo = dto.DescontoAutoTipo,
                BuscarMenorValorPromocao = dto.BuscarMenorValorPromocao, Ativo = dto.Ativo
            };

            MapearSubTabelas(h, dto);
            _db.Set<HierarquiaDesconto>().Add(h);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, h.Id, novo: ParaDict(h));

            return new HierarquiaDescontoListDto { Id = h.Id, Nome = h.Nome, Padrao = h.Padrao, AplicarAutomatico = h.AplicarAutomatico, TotalItens = h.Itens.Count, Ativo = h.Ativo, CriadoEm = h.CriadoEm };
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em HierarquiaDescontoService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, HierarquiaDescontoFormDto dto)
    {
        try
        {
            Validar(dto);
            var h = await _db.Set<HierarquiaDesconto>()
                .Include(x => x.Itens).ThenInclude(i => i.Secoes)
                .Include(x => x.Colaboradores).Include(x => x.Convenios).Include(x => x.Clientes)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException($"Hierarquia {id} não encontrada.");

            if (dto.Padrao && !h.Padrao) await DesmarcarPadraoExistente();

            var anterior = ParaDict(h);
            h.Nome = dto.Nome.Trim().ToUpper(); h.Padrao = dto.Padrao;
            h.AplicarAutomatico = dto.AplicarAutomatico; h.DescontoAutoTipo = dto.DescontoAutoTipo;
            h.BuscarMenorValorPromocao = dto.BuscarMenorValorPromocao; h.Ativo = dto.Ativo;

            // Limpar e recriar
            foreach (var item in h.Itens) _db.Set<HierarquiaDescontoSecao>().RemoveRange(item.Secoes);
            _db.Set<HierarquiaDescontoItem>().RemoveRange(h.Itens);
            _db.Set<HierarquiaDescontoColaborador>().RemoveRange(h.Colaboradores);
            _db.Set<HierarquiaDescontoConvenio>().RemoveRange(h.Convenios);
            _db.Set<HierarquiaDescontoCliente>().RemoveRange(h.Clientes);

            MapearSubTabelas(h, dto);
            await _db.SaveChangesAsync();

            var novo = ParaDict(h);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em HierarquiaDescontoService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var h = await _db.Set<HierarquiaDesconto>()
                .Include(x => x.Itens).ThenInclude(i => i.Secoes)
                .Include(x => x.Colaboradores).Include(x => x.Convenios).Include(x => x.Clientes)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException($"Hierarquia {id} não encontrada.");
            var dados = ParaDict(h);
            foreach (var item in h.Itens) _db.Set<HierarquiaDescontoSecao>().RemoveRange(item.Secoes);
            _db.Set<HierarquiaDescontoItem>().RemoveRange(h.Itens);
            _db.Set<HierarquiaDescontoColaborador>().RemoveRange(h.Colaboradores);
            _db.Set<HierarquiaDescontoConvenio>().RemoveRange(h.Convenios);
            _db.Set<HierarquiaDescontoCliente>().RemoveRange(h.Clientes);
            _db.Set<HierarquiaDesconto>().Remove(h);
            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var rec = await _db.Set<HierarquiaDesconto>().FindAsync(id);
                rec!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException) { Log.Error(ex, "Erro em HierarquiaDescontoService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    private static void MapearSubTabelas(HierarquiaDesconto h, HierarquiaDescontoFormDto dto)
    {
        foreach (var item in dto.Itens)
        {
            var hi = new HierarquiaDescontoItem { Ordem = item.Ordem, Componente = item.Componente };
            if (item.Componente == ComponenteDesconto.SecaoEscolhida)
                foreach (var sid in item.SecaoIds)
                    hi.Secoes.Add(new HierarquiaDescontoSecao { SecaoId = sid });
            h.Itens.Add(hi);
        }
        foreach (var cid in dto.ColaboradorIds.Distinct()) h.Colaboradores.Add(new HierarquiaDescontoColaborador { ColaboradorId = cid });
        foreach (var cid in dto.ConvenioIds.Distinct()) h.Convenios.Add(new HierarquiaDescontoConvenio { ConvenioId = cid });
        foreach (var cid in dto.ClienteIds.Distinct()) h.Clientes.Add(new HierarquiaDescontoCliente { ClienteId = cid });
    }

    private async Task DesmarcarPadraoExistente()
    {
        var existente = await _db.Set<HierarquiaDesconto>().Where(h => h.Padrao).ToListAsync();
        foreach (var h in existente) h.Padrao = false;
    }

    private static void Validar(HierarquiaDescontoFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
        if (dto.Itens.Count == 0) throw new ArgumentException("Adicione ao menos um componente na hierarquia.");
    }

    private static Dictionary<string, string?> ParaDict(HierarquiaDesconto h) => new()
    {
        ["Nome"] = h.Nome, ["Padrao"] = h.Padrao ? "Sim" : "Não",
        ["AplicarAutomatico"] = h.AplicarAutomatico ? "Sim" : "Não",
        ["Itens"] = h.Itens.Count.ToString(), ["Ativo"] = h.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b) =>
        a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
