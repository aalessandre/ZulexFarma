using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.Sngpc;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class EstoqueSngpcService : IEstoqueSngpcService
{
    private readonly AppDbContext _db;

    public EstoqueSngpcService(AppDbContext db) { _db = db; }

    public async Task<List<EstoqueSngpcLinhaDto>> ListarAsync(long? filialId = null, bool incluirVencidos = true)
    {
        var q = _db.ProdutosLotes
            .Include(l => l.Produto)
            .Where(l => l.SaldoAtual > 0
                && (l.Produto.ClasseTerapeutica == ProdutoControleHelper.CLASSE_PSICOTROPICOS
                 || l.Produto.ClasseTerapeutica == ProdutoControleHelper.CLASSE_ANTIMICROBIANO));

        if (filialId.HasValue) q = q.Where(l => l.FilialId == filialId.Value);

        var lista = await q
            .OrderBy(l => l.DataValidade == null ? 1 : 0)
            .ThenBy(l => l.DataValidade)
            .ThenBy(l => l.Produto.Nome)
            .ToListAsync();

        var hoje = DateTime.UtcNow.Date;
        var resultado = lista.Select(l => new EstoqueSngpcLinhaDto
        {
            ProdutoId = l.ProdutoId,
            ProdutoNome = l.Produto.Nome,
            ProdutoCodigoBarras = l.Produto.CodigoBarras,
            ClasseTerapeutica = l.Produto.ClasseTerapeutica,
            ProdutoLoteId = l.Id,
            NumeroLote = l.NumeroLote,
            DataFabricacao = l.DataFabricacao,
            DataValidade = l.DataValidade,
            SaldoAtual = l.SaldoAtual,
            EhLoteFicticio = l.EhLoteFicticio,
            DiasParaVencer = l.DataValidade.HasValue
                ? (int)(l.DataValidade.Value.Date - hoje).TotalDays
                : int.MaxValue
        }).ToList();

        if (!incluirVencidos)
            resultado = resultado.Where(x => x.DiasParaVencer >= 0).ToList();

        return resultado;
    }
}
