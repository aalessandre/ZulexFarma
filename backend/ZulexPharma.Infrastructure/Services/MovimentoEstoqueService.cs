using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.Produtos;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class MovimentoEstoqueService : IMovimentoEstoqueService
{
    private readonly AppDbContext _db;
    private readonly FilialContexto _ctx;

    public MovimentoEstoqueService(AppDbContext db, FilialContexto ctx)
    {
        _db = db;
        _ctx = ctx;
    }

    public void Registrar(long produtoId, long filialId, long? variacaoId, decimal delta, decimal saldoApos,
        TipoMovimentoEstoque tipo, string? documento = null, long? pessoaId = null, string? pessoaNome = null,
        long? usuarioId = null, long? compraId = null, long? vendaId = null, string? observacao = null)
    {
        if (delta == 0) return; // movimento nulo (ex.: ajuste que nao mudou nada) nao vira linha
        var uid = usuarioId is > 0 ? usuarioId
                : (_ctx.UsuarioIdAtual > 0 ? _ctx.UsuarioIdAtual : (long?)null);

        _db.MovimentosEstoque.Add(new MovimentoEstoque
        {
            ProdutoId = produtoId,
            FilialId = filialId,
            ProdutoVariacaoId = variacaoId,
            Data = DataHoraHelper.Agora(),
            Tipo = tipo,
            Quantidade = delta,
            SaldoApos = saldoApos,
            Documento = Truncar(documento, 80),
            PessoaId = pessoaId,
            PessoaNome = Truncar(pessoaNome, 200),
            UsuarioId = uid,
            CompraId = compraId,
            VendaId = vendaId,
            Observacao = Truncar(observacao, 500)
        });
    }

    public async Task<List<MovimentoEstoqueDto>> ListarPorProdutoAsync(long produtoId, long? filialId,
        DateTime? dataInicio, DateTime? dataFim)
    {
        var inicio = (dataInicio ?? DataHoraHelper.Agora().AddDays(-30)).Date;
        var fim = (dataFim ?? DataHoraHelper.Agora()).Date.AddDays(1);

        const int MAX_LINHAS = 500; // teto de seguranca (produto de alto giro) — mostra as mais recentes

        var q = _db.MovimentosEstoque.Where(m => m.ProdutoId == produtoId && m.Data >= inicio && m.Data < fim);
        if (filialId is > 0) q = q.Where(m => m.FilialId == filialId);

        var brutos = await (
            from m in q
            join u in _db.Usuarios on m.UsuarioId equals (long?)u.Id into ug
            from u in ug.DefaultIfEmpty()
            orderby m.Data descending, m.Id descending
            select new
            {
                m.Id,
                m.Data,
                m.Tipo,
                m.Quantidade,
                m.SaldoApos,
                m.Documento,
                m.PessoaNome,
                m.ProdutoVariacaoId,
                m.Observacao,
                m.FilialId,
                UsuarioNome = u != null ? u.Nome : null
            }).Take(MAX_LINHAS).ToListAsync();

        // Descricao dos SKUs (grade) num lookup so'.
        var variacaoIds = brutos.Where(x => x.ProdutoVariacaoId != null)
            .Select(x => x.ProdutoVariacaoId!.Value).Distinct().ToList();
        var descVariacao = new Dictionary<long, string>();
        if (variacaoIds.Count > 0)
        {
            var vars = await _db.ProdutosVariacoes
                .Where(v => variacaoIds.Contains(v.Id))
                .Select(v => new { v.Id, Valores = v.Valores.OrderBy(vv => vv.AtributoVariacao.Ordem).Select(vv => vv.ValorAtributo!.Valor).ToList() })
                .ToListAsync();
            foreach (var v in vars)
                descVariacao[v.Id] = string.Join(" / ", v.Valores.Where(x => !string.IsNullOrEmpty(x)));
        }

        return brutos.Select(x => new MovimentoEstoqueDto
        {
            Id = x.Id,
            Data = x.Data,
            Tipo = LabelTipo(x.Tipo),
            Sentido = x.Quantidade >= 0 ? "Entrada" : "Saida",
            Quantidade = Math.Abs(x.Quantidade),
            SaldoApos = x.SaldoApos,
            Documento = x.Documento,
            PessoaNome = x.PessoaNome,
            UsuarioNome = x.UsuarioNome,
            Variacao = x.ProdutoVariacaoId != null && descVariacao.TryGetValue(x.ProdutoVariacaoId.Value, out var d) ? d : null,
            Observacao = x.Observacao,
            FilialId = x.FilialId
        }).ToList();
    }

    private static string? Truncar(string? s, int max)
        => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max));

    private static string LabelTipo(TipoMovimentoEstoque t) => t switch
    {
        TipoMovimentoEstoque.Compra => "Compra",
        TipoMovimentoEstoque.EstornoCompra => "Estorno compra",
        TipoMovimentoEstoque.Venda => "Venda",
        TipoMovimentoEstoque.Transferencia => "Transferência",
        TipoMovimentoEstoque.Perda => "Perda",
        TipoMovimentoEstoque.EstornoPerda => "Estorno perda",
        TipoMovimentoEstoque.Ajuste => "Ajuste",
        TipoMovimentoEstoque.Grade => "Grade",
        _ => t.ToString()
    };
}
