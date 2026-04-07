using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.PreVendas;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class PreVendaService : IPreVendaService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Pré-Venda";
    private const string ENTIDADE = "PreVenda";

    public PreVendaService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<PreVendaListDto>> ListarAsync(long? filialId = null)
    {
        try
        {
            var query = _db.Set<PreVenda>()
                .Include(v => v.Cliente).ThenInclude(c => c!.Pessoa)
                .Include(v => v.Colaborador).ThenInclude(c => c!.Pessoa)
                .Include(v => v.TipoPagamento)
                .AsQueryable();

            if (filialId.HasValue) query = query.Where(v => v.FilialId == filialId);

            return await query.OrderByDescending(v => v.CriadoEm)
                .Select(v => new PreVendaListDto
                {
                    Id = v.Id, Codigo = v.Codigo,
                    ClienteNome = v.Cliente != null ? v.Cliente.Pessoa.Nome : null,
                    ColaboradorNome = v.Colaborador != null ? v.Colaborador.Pessoa.Nome : null,
                    TipoPagamentoNome = v.TipoPagamento != null ? v.TipoPagamento.Nome : null,
                    TotalLiquido = v.TotalLiquido, TotalItens = v.TotalItens,
                    Status = v.Status, StatusDescricao = StatusTexto(v.Status),
                    CriadoEm = v.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em PreVendaService.ListarAsync"); throw; }
    }

    public async Task<PreVendaDetalheDto?> ObterAsync(long id)
    {
        try
        {
            var v = await _db.Set<PreVenda>()
                .Include(x => x.Cliente).ThenInclude(c => c!.Pessoa)
                .Include(x => x.Colaborador).ThenInclude(c => c!.Pessoa)
                .Include(x => x.Itens)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return null;

            // Buscar dados extras dos produtos (estoque, grupo)
            var prodIds = v.Itens.Select(i => i.ProdutoId).ToList();
            var dados = await _db.ProdutosDados
                .Where(d => prodIds.Contains(d.ProdutoId) && d.FilialId == v.FilialId)
                .Select(d => new { d.ProdutoId, d.EstoqueAtual })
                .ToListAsync();

            return new PreVendaDetalheDto
            {
                Id = v.Id, FilialId = v.FilialId,
                ClienteId = v.ClienteId, ClienteNome = v.Cliente?.Pessoa?.Nome,
                ColaboradorId = v.ColaboradorId, ColaboradorNome = v.Colaborador?.Pessoa?.Nome,
                TipoPagamentoId = v.TipoPagamentoId, ConvenioId = v.ConvenioId,
                TotalBruto = v.TotalBruto, TotalDesconto = v.TotalDesconto,
                TotalLiquido = v.TotalLiquido, TotalItens = v.TotalItens,
                Status = v.Status, Observacao = v.Observacao, CriadoEm = v.CriadoEm,
                Itens = v.Itens.OrderBy(i => i.Ordem).Select(i =>
                {
                    var d = dados.FirstOrDefault(x => x.ProdutoId == i.ProdutoId);
                    return new PreVendaItemDto
                    {
                        Id = i.Id, ProdutoId = i.ProdutoId, ProdutoCodigo = i.ProdutoCodigo,
                        ProdutoNome = i.ProdutoNome, Fabricante = i.Fabricante,
                        PrecoVenda = i.PrecoVenda, Quantidade = i.Quantidade,
                        PercentualDesconto = i.PercentualDesconto, ValorDesconto = i.ValorDesconto,
                        PrecoUnitario = i.PrecoUnitario, Total = i.Total,
                        EstoqueAtual = d?.EstoqueAtual ?? 0
                    };
                }).ToList()
            };
        }
        catch (Exception ex) { Log.Error(ex, "Erro em PreVendaService.ObterAsync | Id: {Id}", id); throw; }
    }

    public async Task<PreVendaDetalheDto> CriarAsync(PreVendaFormDto dto)
    {
        try
        {
            var pv = new PreVenda
            {
                FilialId = dto.FilialId, ClienteId = dto.ClienteId, ColaboradorId = dto.ColaboradorId,
                TipoPagamentoId = dto.TipoPagamentoId, ConvenioId = dto.ConvenioId,
                Observacao = dto.Observacao, Status = PreVendaStatus.Aberta
            };

            int ordem = 1;
            foreach (var item in dto.Itens)
            {
                var i = MapearItem(item, ordem++);
                pv.Itens.Add(i);
            }

            RecalcularTotais(pv);
            _db.Set<PreVenda>().Add(pv);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, pv.Id, novo: ParaDict(pv));

            return (await ObterAsync(pv.Id))!;
        }
        catch (Exception ex) { Log.Error(ex, "Erro em PreVendaService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, PreVendaFormDto dto)
    {
        try
        {
            var pv = await _db.Set<PreVenda>()
                .Include(v => v.Itens)
                .FirstOrDefaultAsync(v => v.Id == id)
                ?? throw new KeyNotFoundException($"Pré-venda {id} não encontrada.");

            if (pv.Status != PreVendaStatus.Aberta)
                throw new ArgumentException("Apenas pré-vendas abertas podem ser alteradas.");

            pv.ClienteId = dto.ClienteId; pv.ColaboradorId = dto.ColaboradorId;
            pv.TipoPagamentoId = dto.TipoPagamentoId; pv.ConvenioId = dto.ConvenioId;
            pv.Observacao = dto.Observacao;

            _db.Set<PreVendaItem>().RemoveRange(pv.Itens);
            int ordem = 1;
            foreach (var item in dto.Itens)
                pv.Itens.Add(MapearItem(item, ordem++));

            RecalcularTotais(pv);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em PreVendaService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task FinalizarAsync(long id)
    {
        try
        {
            var pv = await _db.Set<PreVenda>().FindAsync(id)
                ?? throw new KeyNotFoundException($"Pré-venda {id} não encontrada.");
            if (pv.Status != PreVendaStatus.Aberta) throw new ArgumentException("Pré-venda não está aberta.");
            pv.Status = PreVendaStatus.Finalizada;
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "FINALIZAÇÃO", ENTIDADE, id);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em PreVendaService.FinalizarAsync | Id: {Id}", id); throw; }
    }

    public async Task CancelarAsync(long id)
    {
        try
        {
            var pv = await _db.Set<PreVenda>().FindAsync(id)
                ?? throw new KeyNotFoundException($"Pré-venda {id} não encontrada.");
            if (pv.Status != PreVendaStatus.Aberta) throw new ArgumentException("Pré-venda não está aberta.");
            pv.Status = PreVendaStatus.Cancelada;
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CANCELAMENTO", ENTIDADE, id);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em PreVendaService.CancelarAsync | Id: {Id}", id); throw; }
    }

    private static PreVendaItem MapearItem(PreVendaItemFormDto dto, int ordem)
    {
        var valorDesconto = dto.PrecoVenda * dto.Quantidade * dto.PercentualDesconto / 100;
        var precoUnit = dto.PrecoVenda * (1 - dto.PercentualDesconto / 100);
        var total = precoUnit * dto.Quantidade;
        return new PreVendaItem
        {
            ProdutoId = dto.ProdutoId, ProdutoCodigo = dto.ProdutoCodigo, ProdutoNome = dto.ProdutoNome,
            Fabricante = dto.Fabricante, PrecoVenda = dto.PrecoVenda, Quantidade = dto.Quantidade,
            PercentualDesconto = dto.PercentualDesconto, ValorDesconto = Math.Round(valorDesconto, 2),
            PrecoUnitario = Math.Round(precoUnit, 2), Total = Math.Round(total, 2), Ordem = ordem
        };
    }

    private static void RecalcularTotais(PreVenda pv)
    {
        pv.TotalBruto = pv.Itens.Sum(i => i.PrecoVenda * i.Quantidade);
        pv.TotalDesconto = pv.Itens.Sum(i => i.ValorDesconto);
        pv.TotalLiquido = pv.Itens.Sum(i => i.Total);
        pv.TotalItens = pv.Itens.Count;
    }

    private static string StatusTexto(PreVendaStatus s) => s switch
    {
        PreVendaStatus.Aberta => "Aberta",
        PreVendaStatus.Finalizada => "Finalizada",
        PreVendaStatus.Cancelada => "Cancelada",
        _ => ""
    };

    private static Dictionary<string, string?> ParaDict(PreVenda v) => new()
    {
        ["TotalItens"] = v.TotalItens.ToString(), ["TotalLiquido"] = v.TotalLiquido.ToString("N2"),
        ["Status"] = StatusTexto(v.Status)
    };
}
