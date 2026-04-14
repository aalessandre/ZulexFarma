using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Sngpc;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Gerencia inventários SNGPC. Fluxo: criar rascunho → adicionar itens → finalizar.
/// Ao finalizar, cada item vira um ProdutoLote + MovimentoLote tipo AjusteInicial.
/// </summary>
public class InventarioSngpcService : IInventarioSngpcService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private readonly IProdutoLoteService _loteService;
    private const string TELA = "Inventário SNGPC";
    private const string ENTIDADE = "InventarioSngpc";

    public InventarioSngpcService(AppDbContext db, ILogAcaoService log, IProdutoLoteService loteService)
    {
        _db = db;
        _log = log;
        _loteService = loteService;
    }

    public async Task<List<InventarioSngpcListDto>> ListarAsync(long? filialId = null)
    {
        var q = _db.InventariosSngpc.Include(i => i.Itens).AsQueryable();
        if (filialId.HasValue) q = q.Where(i => i.FilialId == filialId.Value);

        var lista = await q.OrderByDescending(i => i.DataInventario).ToListAsync();
        var filialMap = await _db.Filiais.ToDictionaryAsync(f => f.Id, f => f.NomeFilial);

        return lista.Select(i => new InventarioSngpcListDto
        {
            Id = i.Id,
            FilialId = i.FilialId,
            FilialNome = filialMap.GetValueOrDefault(i.FilialId),
            DataInventario = i.DataInventario,
            Descricao = i.Descricao,
            Status = (int)i.Status,
            StatusNome = i.Status.ToString(),
            DataFinalizacao = i.DataFinalizacao,
            TotalItens = i.Itens.Count,
            QuantidadeTotal = i.Itens.Sum(it => it.Quantidade)
        }).ToList();
    }

    public async Task<InventarioSngpcDetalheDto> ObterAsync(long id)
    {
        var inv = await _db.InventariosSngpc
            .Include(i => i.Itens).ThenInclude(it => it.Produto)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new KeyNotFoundException($"Inventário {id} não encontrado.");

        return new InventarioSngpcDetalheDto
        {
            Id = inv.Id,
            FilialId = inv.FilialId,
            DataInventario = inv.DataInventario,
            Descricao = inv.Descricao,
            Status = (int)inv.Status,
            DataFinalizacao = inv.DataFinalizacao,
            Observacao = inv.Observacao,
            Itens = inv.Itens.Select(it => new InventarioSngpcItemDto
            {
                Id = it.Id,
                ProdutoId = it.ProdutoId,
                ProdutoNome = it.Produto?.Nome,
                ProdutoCodigoBarras = it.Produto?.CodigoBarras,
                ClasseTerapeutica = it.Produto?.ClasseTerapeutica,
                NumeroLote = it.NumeroLote,
                DataFabricacao = it.DataFabricacao,
                DataValidade = it.DataValidade,
                Quantidade = it.Quantidade,
                RegistroMs = it.RegistroMs,
                Observacao = it.Observacao
            }).ToList()
        };
    }

    public async Task<InventarioSngpcListDto> CriarAsync(InventarioSngpcFormDto dto, long? usuarioId)
    {
        if (dto.FilialId <= 0) throw new ArgumentException("Filial é obrigatória.");

        var inv = new InventarioSngpc
        {
            FilialId = dto.FilialId,
            DataInventario = DateTime.SpecifyKind(dto.DataInventario, DateTimeKind.Utc),
            Descricao = dto.Descricao,
            Observacao = dto.Observacao,
            Status = StatusInventarioSngpc.Rascunho,
            UsuarioId = usuarioId
        };
        foreach (var item in dto.Itens)
        {
            inv.Itens.Add(MapItem(item));
        }
        _db.InventariosSngpc.Add(inv);
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, inv.Id, novo: new Dictionary<string, string?>
        {
            ["Data"] = inv.DataInventario.ToString("dd/MM/yyyy"),
            ["Itens"] = inv.Itens.Count.ToString(),
            ["Descrição"] = inv.Descricao
        });

        return (await ListarAsync(inv.FilialId)).First(x => x.Id == inv.Id);
    }

    public async Task AtualizarAsync(long id, InventarioSngpcFormDto dto)
    {
        var inv = await _db.InventariosSngpc.Include(i => i.Itens)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new KeyNotFoundException($"Inventário {id} não encontrado.");

        if (inv.Status != StatusInventarioSngpc.Rascunho)
            throw new InvalidOperationException("Só é possível editar inventário em rascunho.");

        inv.DataInventario = DateTime.SpecifyKind(dto.DataInventario, DateTimeKind.Utc);
        inv.Descricao = dto.Descricao;
        inv.Observacao = dto.Observacao;

        // Substituição completa dos itens (simples — usuário edita em UI toda a lista)
        _db.InventariosSngpcItens.RemoveRange(inv.Itens);
        inv.Itens.Clear();
        foreach (var item in dto.Itens)
        {
            inv.Itens.Add(MapItem(item));
        }
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, novo: new Dictionary<string, string?>
        {
            ["Itens"] = inv.Itens.Count.ToString()
        });
    }

    public async Task<int> FinalizarAsync(long id, long? usuarioId)
    {
        var inv = await _db.InventariosSngpc.Include(i => i.Itens).ThenInclude(it => it.Produto)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new KeyNotFoundException($"Inventário {id} não encontrado.");

        if (inv.Status != StatusInventarioSngpc.Rascunho)
            throw new InvalidOperationException("Este inventário já foi finalizado.");

        if (inv.Itens.Count == 0)
            throw new InvalidOperationException("Inventário sem itens. Adicione ao menos um produto antes de finalizar.");

        int lotesCriados = 0;
        foreach (var item in inv.Itens)
        {
            if (item.Quantidade <= 0) continue;

            await _loteService.RegistrarEntradaAsync(
                produtoId: item.ProdutoId,
                filialId: inv.FilialId,
                numeroLote: string.IsNullOrWhiteSpace(item.NumeroLote) ? "S/L" : item.NumeroLote,
                dataFabricacao: item.DataFabricacao,
                dataValidade: item.DataValidade,
                quantidade: item.Quantidade,
                tipo: TipoMovimentoLote.AjusteInicial,
                registroMs: item.RegistroMs,
                usuarioId: usuarioId,
                observacao: $"Inventário SNGPC #{inv.Id} em {inv.DataInventario:dd/MM/yyyy}");
            lotesCriados++;
        }

        inv.Status = StatusInventarioSngpc.Finalizado;
        inv.DataFinalizacao = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync(TELA, "FINALIZAÇÃO", ENTIDADE, id, novo: new Dictionary<string, string?>
        {
            ["Lotes criados"] = lotesCriados.ToString(),
            ["Data"] = inv.DataInventario.ToString("dd/MM/yyyy")
        });

        return lotesCriados;
    }

    public async Task ExcluirAsync(long id)
    {
        var inv = await _db.InventariosSngpc.FindAsync(id)
            ?? throw new KeyNotFoundException($"Inventário {id} não encontrado.");
        if (inv.Status == StatusInventarioSngpc.Finalizado)
            throw new InvalidOperationException("Não é possível excluir inventário já finalizado.");

        _db.InventariosSngpc.Remove(inv);
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id);
    }

    private static InventarioSngpcItem MapItem(InventarioSngpcItemDto dto) => new()
    {
        ProdutoId = dto.ProdutoId,
        NumeroLote = string.IsNullOrWhiteSpace(dto.NumeroLote) ? "S/L" : dto.NumeroLote.Trim(),
        DataFabricacao = dto.DataFabricacao.HasValue ? DateTime.SpecifyKind(dto.DataFabricacao.Value, DateTimeKind.Utc) : null,
        DataValidade = dto.DataValidade.HasValue ? DateTime.SpecifyKind(dto.DataValidade.Value, DateTimeKind.Utc) : null,
        Quantidade = dto.Quantidade,
        RegistroMs = dto.RegistroMs,
        Observacao = dto.Observacao
    };
}
