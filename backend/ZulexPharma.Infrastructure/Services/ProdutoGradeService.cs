using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.Grade;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Grade de variações de um produto: eixos, geração/sincronia de SKUs
/// (ProdutoVariacao) e estoque/preço por SKU na filial atual (ProdutoDados).
/// Ver docs/specs/multiramo-grade.md (Passo 2).
/// </summary>
public class ProdutoGradeService : IProdutoGradeService
{
    private readonly AppDbContext _db;
    private readonly FilialContexto _ctx;

    public ProdutoGradeService(AppDbContext db, FilialContexto ctx)
    {
        _db = db;
        _ctx = ctx;
    }

    public async Task<ProdutoGradeDto> ObterAsync(long produtoId)
    {
        var filialId = _ctx.FilialIdAtual;

        var produto = await _db.Produtos
            .Include(p => p.Atributos)
            .Include(p => p.Variacoes).ThenInclude(v => v.Valores).ThenInclude(vv => vv.AtributoVariacao)
            .Include(p => p.Variacoes).ThenInclude(v => v.Valores).ThenInclude(vv => vv.ValorAtributo)
            .FirstOrDefaultAsync(p => p.Id == produtoId)
            ?? throw new KeyNotFoundException($"Produto {produtoId} não encontrado.");

        var variacaoIds = produto.Variacoes.Select(v => v.Id).ToList();
        var dados = await _db.ProdutosDados
            .Where(d => d.ProdutoVariacaoId != null && variacaoIds.Contains(d.ProdutoVariacaoId!.Value) && d.FilialId == filialId)
            .ToListAsync();
        var dadosPorVar = dados.ToDictionary(d => d.ProdutoVariacaoId!.Value);

        return new ProdutoGradeDto
        {
            ProdutoId = produto.Id,
            ControlaGrade = produto.ControlaGrade,
            AtributoIds = produto.Atributos.OrderBy(a => a.Ordem).Select(a => a.AtributoVariacaoId).ToList(),
            Variacoes = produto.Variacoes.Select(v => new VariacaoDto
            {
                Id = v.Id,
                CodigoBarras = v.CodigoBarras,
                PrecoProprio = v.PrecoProprio,
                Estoque = dadosPorVar.TryGetValue(v.Id, out var d) ? d.EstoqueAtual : 0,
                Preco = dadosPorVar.TryGetValue(v.Id, out var d2) ? d2.ValorVenda : (v.PrecoProprio ?? 0),
                Valores = v.Valores.Select(vv => new VariacaoValorDto
                {
                    AtributoVariacaoId = vv.AtributoVariacaoId,
                    ValorAtributoId = vv.ValorAtributoId,
                    AtributoNome = vv.AtributoVariacao?.Nome,
                    ValorTexto = vv.ValorAtributo?.Valor
                }).ToList()
            }).ToList()
        };
    }

    public async Task SalvarAsync(long produtoId, SalvarGradeDto dto)
    {
        var filialId = _ctx.FilialIdAtual;
        if (filialId <= 0)
            throw new ArgumentException("Selecione uma filial válida para editar a grade (estoque/preço são por filial).");

        var produto = await _db.Produtos
            .Include(p => p.Atributos)
            .Include(p => p.Variacoes).ThenInclude(v => v.Valores)
            .FirstOrDefaultAsync(p => p.Id == produtoId)
            ?? throw new KeyNotFoundException($"Produto {produtoId} não encontrado.");

        // Se há variações, o produto é vendido por elas — liga o flag mesmo que o
        // toggle não tenha sido marcado (evita venda direta do modelo sem escolher SKU).
        produto.ControlaGrade = dto.ControlaGrade || dto.Variacoes.Any();
        produto.AtualizadoEm = DateTime.UtcNow;

        SincronizarEixos(produto, dto.AtributoIds);
        await SincronizarVariacoesAsync(produto, dto, filialId);
    }

    // ── Eixos (ProdutoAtributo) ────────────────────────────────────
    private void SincronizarEixos(Produto produto, List<long> atributoIds)
    {
        var eixos = atributoIds.Distinct().ToList();

        foreach (var rem in produto.Atributos.Where(a => !eixos.Contains(a.AtributoVariacaoId)).ToList())
            _db.ProdutosAtributos.Remove(rem);

        for (int i = 0; i < eixos.Count; i++)
        {
            var existente = produto.Atributos.FirstOrDefault(a => a.AtributoVariacaoId == eixos[i]);
            if (existente == null)
                produto.Atributos.Add(new ProdutoAtributo { AtributoVariacaoId = eixos[i], Ordem = i + 1 });
            else
                existente.Ordem = i + 1;
        }
    }

    // ── Variações (SKUs) + estoque/preço ───────────────────────────
    private async Task SincronizarVariacoesAsync(Produto produto, SalvarGradeDto dto, long filialId)
    {
        var idsForm = dto.Variacoes.Where(v => v.Id.HasValue).Select(v => v.Id!.Value).ToHashSet();

        // Remove variações que sumiram. Se referenciada por venda, apenas desativa.
        foreach (var vrem in produto.Variacoes.Where(v => !idsForm.Contains(v.Id)).ToList())
        {
            var usada = await _db.VendaItens.AnyAsync(vi => vi.ProdutoVariacaoId == vrem.Id);
            if (usada)
            {
                vrem.Ativo = false;
            }
            else
            {
                var dadosVar = await _db.ProdutosDados.Where(d => d.ProdutoVariacaoId == vrem.Id).ToListAsync();
                _db.ProdutosDados.RemoveRange(dadosVar);
                _db.ProdutosVariacoes.Remove(vrem);
            }
        }

        // Upsert das variações. A combinação de valores (o SKU) é imutável — só
        // se define ao criar; edições mexem em barras/preço/estoque.
        var mapa = new List<(ProdutoVariacao entidade, SalvarVariacaoDto dto)>();
        foreach (var vf in dto.Variacoes)
        {
            ProdutoVariacao variacao;
            if (vf.Id.HasValue)
            {
                variacao = produto.Variacoes.First(v => v.Id == vf.Id.Value);
                variacao.CodigoBarras = string.IsNullOrWhiteSpace(vf.CodigoBarras) ? null : vf.CodigoBarras.Trim();
                variacao.PrecoProprio = vf.PrecoProprio;
                variacao.AtualizadoEm = DateTime.UtcNow;
            }
            else
            {
                variacao = new ProdutoVariacao
                {
                    ProdutoId = produto.Id,
                    CodigoBarras = string.IsNullOrWhiteSpace(vf.CodigoBarras) ? null : vf.CodigoBarras.Trim(),
                    PrecoProprio = vf.PrecoProprio,
                    Valores = vf.Valores.Select(x => new ProdutoVariacaoValor
                    {
                        AtributoVariacaoId = x.AtributoVariacaoId,
                        ValorAtributoId = x.ValorAtributoId
                    }).ToList()
                };
                produto.Variacoes.Add(variacao);
            }
            mapa.Add((variacao, vf));
        }

        await _db.SaveChangesAsync();   // atribui Ids às variações novas

        // Estoque/preço por SKU na filial atual (ProdutoDados).
        foreach (var (entidade, vf) in mapa)
        {
            var d = await _db.ProdutosDados
                .FirstOrDefaultAsync(x => x.ProdutoVariacaoId == entidade.Id && x.FilialId == filialId);
            if (d == null)
            {
                d = new ProdutoDados { ProdutoId = produto.Id, FilialId = filialId, ProdutoVariacaoId = entidade.Id };
                _db.ProdutosDados.Add(d);
            }
            d.EstoqueAtual = vf.Estoque;
            d.ValorVenda = vf.Preco;
            d.AtualizadoEm = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }
}
