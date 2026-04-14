using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.Sngpc;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class CompraSngpcService : ICompraSngpcService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private readonly IProdutoLoteService _loteService;
    private const string TELA = "Compras SNGPC";

    public CompraSngpcService(AppDbContext db, ILogAcaoService log, IProdutoLoteService loteService)
    {
        _db = db;
        _log = log;
        _loteService = loteService;
    }

    public async Task<List<CompraSngpcListDto>> ListarAsync(long? filialId = null, DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        // Busca compras finalizadas que tenham pelo menos 1 produto com ClasseTerapeutica controlada
        var q = _db.Compras
            .Include(c => c.Fornecedor).ThenInclude(f => f.Pessoa)
            .Include(c => c.Produtos).ThenInclude(p => p.Produto)
            .Include(c => c.Produtos).ThenInclude(p => p.Lotes)
            .Where(c => c.Status == CompraStatus.Finalizada
                && c.Produtos.Any(p => p.Produto != null
                    && (p.Produto.ClasseTerapeutica == ProdutoControleHelper.CLASSE_PSICOTROPICOS
                     || p.Produto.ClasseTerapeutica == ProdutoControleHelper.CLASSE_ANTIMICROBIANO)));

        if (filialId.HasValue) q = q.Where(c => c.FilialId == filialId.Value);
        if (dataInicio.HasValue) q = q.Where(c => c.DataEmissao >= dataInicio.Value);
        if (dataFim.HasValue) q = q.Where(c => c.DataEmissao <= dataFim.Value);

        var compras = await q.OrderByDescending(c => c.DataFinalizacao ?? c.DataEmissao).ToListAsync();

        return compras.Select(c =>
        {
            var sngpcItens = c.Produtos.Where(p => p.Produto != null && ProdutoControleHelper.IsProdutoSngpc(p.Produto)).ToList();
            var totalLotes = sngpcItens.Sum(p => p.Lotes.Count);
            var qtdeTotal = sngpcItens.Sum(p => p.Lotes.Count > 0
                ? p.Lotes.Sum(l => l.Quantidade)
                : p.Quantidade);
            var optOut = c.SngpcOptOut == true;
            return new CompraSngpcListDto
            {
                CompraId = c.Id,
                Codigo = null, // Compra não tem Codigo no momento; deixa null
                NumeroNf = c.NumeroNf,
                FornecedorNome = c.Fornecedor?.Pessoa?.Nome ?? "",
                DataEmissao = c.DataEmissao,
                DataFinalizacao = c.DataFinalizacao,
                QtdeProdutosSngpc = sngpcItens.Count,
                QtdeLotesCriados = totalLotes,
                QuantidadeTotal = qtdeTotal,
                SngpcOptOut = optOut,
                StatusSngpc = optOut ? "Opt-out" : "Lançada"
            };
        }).ToList();
    }

    public async Task<CompraSngpcDetalheDto> ObterAsync(long compraId)
    {
        var compra = await _db.Compras
            .Include(c => c.Fornecedor).ThenInclude(f => f.Pessoa)
            .Include(c => c.Produtos).ThenInclude(p => p.Produto)
            .Include(c => c.Produtos).ThenInclude(p => p.Lotes)
            .FirstOrDefaultAsync(c => c.Id == compraId)
            ?? throw new KeyNotFoundException($"Compra {compraId} não encontrada.");

        var dto = new CompraSngpcDetalheDto
        {
            CompraId = compra.Id,
            NumeroNf = compra.NumeroNf,
            FornecedorNome = compra.Fornecedor?.Pessoa?.Nome ?? "",
            DataEmissao = compra.DataEmissao,
            DataFinalizacao = compra.DataFinalizacao,
            SngpcOptOut = compra.SngpcOptOut == true
        };

        foreach (var item in compra.Produtos.Where(p => p.Produto != null && ProdutoControleHelper.IsProdutoSngpc(p.Produto)))
        {
            if (item.Lotes.Count > 0)
            {
                foreach (var lote in item.Lotes)
                {
                    dto.Itens.Add(new CompraSngpcItemDto
                    {
                        ProdutoId = item.ProdutoId ?? 0,
                        ProdutoNome = item.Produto!.Nome,
                        ClasseTerapeutica = item.Produto.ClasseTerapeutica,
                        NumeroLote = lote.NumeroLote,
                        DataFabricacao = lote.DataFabricacao,
                        DataValidade = lote.DataValidade,
                        Quantidade = lote.Quantidade,
                        DataEntrada = compra.DataFinalizacao ?? DateTime.UtcNow
                    });
                }
            }
            else
            {
                dto.Itens.Add(new CompraSngpcItemDto
                {
                    ProdutoId = item.ProdutoId ?? 0,
                    ProdutoNome = item.Produto!.Nome,
                    ClasseTerapeutica = item.Produto.ClasseTerapeutica,
                    NumeroLote = item.Lote ?? "S/L",
                    DataFabricacao = item.DataFabricacao,
                    DataValidade = item.DataValidade,
                    Quantidade = item.Quantidade,
                    DataEntrada = compra.DataFinalizacao ?? DateTime.UtcNow
                });
            }
        }

        return dto;
    }

    public async Task<int> LancarRetroativoAsync(long compraId, long? usuarioId)
    {
        var compra = await _db.Compras
            .Include(c => c.Produtos).ThenInclude(p => p.Produto).ThenInclude(pr => pr!.RegistrosMs)
            .Include(c => c.Produtos).ThenInclude(p => p.Lotes)
            .FirstOrDefaultAsync(c => c.Id == compraId)
            ?? throw new KeyNotFoundException($"Compra {compraId} não encontrada.");

        if (compra.Status != CompraStatus.Finalizada)
            throw new InvalidOperationException("Só é possível lançar retroativamente compras finalizadas.");

        int lotesCriados = 0;
        foreach (var item in compra.Produtos.Where(p => p.Produto != null && ProdutoControleHelper.IsProdutoSngpc(p.Produto) && p.ProdutoId.HasValue))
        {
            var registroMs = item.Produto!.RegistrosMs.FirstOrDefault()?.NumeroMs;
            var fracao = item.Fracao > 0 ? item.Fracao : (short)1;

            if (item.Lotes.Count > 0)
            {
                foreach (var lote in item.Lotes)
                {
                    // Verifica se já não foi lançado (evita duplicar)
                    bool jaLancado = await _db.MovimentosLote.AnyAsync(m =>
                        m.CompraProdutoLoteId == lote.Id && m.Tipo == TipoMovimentoLote.Entrada);
                    if (jaLancado) continue;

                    var qtde = lote.Quantidade * fracao;
                    if (qtde <= 0) continue;

                    await _loteService.RegistrarEntradaAsync(
                        produtoId: item.ProdutoId!.Value,
                        filialId: compra.FilialId,
                        numeroLote: lote.NumeroLote,
                        dataFabricacao: lote.DataFabricacao,
                        dataValidade: lote.DataValidade,
                        quantidade: qtde,
                        tipo: TipoMovimentoLote.Entrada,
                        registroMs: lote.RegistroMs ?? registroMs,
                        fornecedorId: compra.FornecedorId,
                        compraId: compra.Id,
                        compraProdutoLoteId: lote.Id,
                        usuarioId: usuarioId,
                        observacao: $"Lançamento retroativo NF {compra.NumeroNf}");
                    lotesCriados++;
                }
            }
            else if (!string.IsNullOrWhiteSpace(item.Lote))
            {
                // Verifica se já foi lançado (por CompraId + produto + lote)
                bool jaLancado = await _db.MovimentosLote.AnyAsync(m =>
                    m.CompraId == compra.Id && m.ProdutoLote.ProdutoId == item.ProdutoId);
                if (jaLancado) continue;

                await _loteService.RegistrarEntradaAsync(
                    produtoId: item.ProdutoId!.Value,
                    filialId: compra.FilialId,
                    numeroLote: item.Lote,
                    dataFabricacao: item.DataFabricacao,
                    dataValidade: item.DataValidade,
                    quantidade: item.Quantidade * fracao,
                    tipo: TipoMovimentoLote.Entrada,
                    registroMs: registroMs,
                    fornecedorId: compra.FornecedorId,
                    compraId: compra.Id,
                    usuarioId: usuarioId,
                    observacao: $"Lançamento retroativo NF {compra.NumeroNf}");
                lotesCriados++;
            }
        }

        // Remove o opt-out já que foi lançado
        compra.SngpcOptOut = false;
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync(TELA, "LANÇAMENTO RETROATIVO", "Compra", compra.Id, novo: new Dictionary<string, string?>
        {
            ["NF"] = compra.NumeroNf,
            ["Lotes criados"] = lotesCriados.ToString()
        });

        return lotesCriados;
    }
}
