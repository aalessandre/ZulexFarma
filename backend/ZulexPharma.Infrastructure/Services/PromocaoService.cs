using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Promocoes;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class PromocaoService : IPromocaoService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Promoções";
    private const string ENTIDADE = "Promocao";

    public PromocaoService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<PromocaoListDto>> ListarAsync()
    {
        try
        {
            return await _db.Set<Promocao>()
                .Include(p => p.Produtos)
                .OrderByDescending(p => p.DataHoraInicio)
                .Select(p => new PromocaoListDto
                {
                    Id = p.Id, Nome = p.Nome, Tipo = p.Tipo,
                    DataHoraInicio = p.DataHoraInicio, DataHoraFim = p.DataHoraFim,
                    DiaSemana = p.DiaSemana, TotalProdutos = p.Produtos.Count,
                    LancarPorQuantidade = p.LancarPorQuantidade,
                    Ativo = p.Ativo, CriadoEm = p.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em PromocaoService.ListarAsync"); throw; }
    }

    public async Task<PromocaoDetalheDto?> ObterAsync(long id)
    {
        try
        {
            var p = await _db.Set<Promocao>()
                .Include(x => x.Filiais)
                .Include(x => x.Pagamentos)
                .Include(x => x.Convenios)
                .Include(x => x.Faixas)
                .Include(x => x.Produtos).ThenInclude(pp => pp.Produto)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return null;

            // Buscar dados dos produtos (preço, custo, estoque) da primeira filial
            var produtoIds = p.Produtos.Select(pp => pp.ProdutoId).ToList();
            var filialId = p.Filiais.FirstOrDefault()?.FilialId ?? 1;
            var dadosProdutos = await _db.ProdutosDados
                .Where(d => produtoIds.Contains(d.ProdutoId) && d.FilialId == filialId)
                .ToListAsync();

            return new PromocaoDetalheDto
            {
                Id = p.Id, Nome = p.Nome, Tipo = p.Tipo,
                DataHoraInicio = p.DataHoraInicio, DataHoraFim = p.DataHoraFim,
                DiaSemana = p.DiaSemana, PermitirMudarPreco = p.PermitirMudarPreco,
                GerarComissao = p.GerarComissao, ExclusivaConvenio = p.ExclusivaConvenio,
                ReducaoVendaPrazo = p.ReducaoVendaPrazo, QtdeMaxPorVenda = p.QtdeMaxPorVenda,
                LancarPorQuantidade = p.LancarPorQuantidade, DataInicioContagem = p.DataInicioContagem,
                Intersabores = p.Intersabores,
                Ativo = p.Ativo, CriadoEm = p.CriadoEm,
                FilialIds = p.Filiais.Select(f => f.FilialId).ToList(),
                PagamentoIds = p.Pagamentos.Select(pg => pg.TipoPagamentoId).ToList(),
                ConvenioIds = p.Convenios.Select(c => c.ConvenioId).ToList(),
                Faixas = p.Faixas.OrderBy(f => f.Quantidade).Select(f => new PromocaoFaixaDto { Quantidade = f.Quantidade, PercentualDesconto = f.PercentualDesconto }).ToList(),
                Produtos = p.Produtos.Select(pp =>
                {
                    var dados = dadosProdutos.FirstOrDefault(d => d.ProdutoId == pp.ProdutoId);
                    return new PromocaoProdutoDto
                    {
                        Id = pp.Id, ProdutoId = pp.ProdutoId,
                        ProdutoCodigo = pp.Produto?.Codigo,
                        ProdutoNome = pp.Produto?.Nome ?? "",
                        Fabricante = "", // será preenchido se necessário
                        PrecoVenda = dados?.ValorVenda ?? 0,
                        CustoMedio = dados?.CustoMedio ?? 0,
                        EstoqueAtual = dados?.EstoqueAtual ?? 0,
                        Curva = dados?.CurvaAbc,
                        PercentualPromocao = pp.PercentualPromocao,
                        ValorPromocao = pp.ValorPromocao,
                        PercentualLucro = pp.PercentualLucro,
                        QtdeLimite = pp.QtdeLimite, QtdeVendida = pp.QtdeVendida,
                        PercentualAposLimite = pp.PercentualAposLimite,
                        ValorAposLimite = pp.ValorAposLimite
                    };
                }).ToList()
            };
        }
        catch (Exception ex) { Log.Error(ex, "Erro em PromocaoService.ObterAsync | Id: {Id}", id); throw; }
    }

    public async Task<PromocaoListDto> CriarAsync(PromocaoFormDto dto)
    {
        try
        {
            Validar(dto);
            var promo = new Promocao
            {
                Nome = dto.Nome.Trim().ToUpper(),
                Tipo = dto.Tipo,
                DataHoraInicio = dto.DataHoraInicio,
                DataHoraFim = dto.DataHoraFim,
                DiaSemana = dto.DiaSemana,
                PermitirMudarPreco = dto.PermitirMudarPreco,
                GerarComissao = dto.GerarComissao,
                ExclusivaConvenio = dto.ExclusivaConvenio,
                ReducaoVendaPrazo = dto.ReducaoVendaPrazo,
                QtdeMaxPorVenda = dto.QtdeMaxPorVenda,
                LancarPorQuantidade = dto.LancarPorQuantidade,
                DataInicioContagem = dto.DataInicioContagem,
                Intersabores = dto.Intersabores,
                Ativo = dto.Ativo
            };

            foreach (var fid in dto.FilialIds.Distinct())
                promo.Filiais.Add(new PromocaoFilial { FilialId = fid });
            foreach (var pid in dto.PagamentoIds.Distinct())
                promo.Pagamentos.Add(new PromocaoPagamento { TipoPagamentoId = pid });
            foreach (var cid in dto.ConvenioIds.Distinct())
                promo.Convenios.Add(new PromocaoConvenio { ConvenioId = cid });
            foreach (var faixa in dto.Faixas)
                promo.Faixas.Add(new PromocaoFaixa { Quantidade = faixa.Quantidade, PercentualDesconto = faixa.PercentualDesconto });
            foreach (var prod in dto.Produtos)
                promo.Produtos.Add(new PromocaoProduto
                {
                    ProdutoId = prod.ProdutoId,
                    PercentualPromocao = prod.PercentualPromocao,
                    ValorPromocao = prod.ValorPromocao,
                    PercentualLucro = prod.PercentualLucro,
                    QtdeLimite = prod.QtdeLimite,
                    PercentualAposLimite = prod.PercentualAposLimite,
                    ValorAposLimite = prod.ValorAposLimite
                });

            _db.Set<Promocao>().Add(promo);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, promo.Id, novo: ParaDict(promo));

            return new PromocaoListDto
            {
                Id = promo.Id, Nome = promo.Nome, Tipo = promo.Tipo,
                DataHoraInicio = promo.DataHoraInicio, DataHoraFim = promo.DataHoraFim,
                DiaSemana = promo.DiaSemana, TotalProdutos = promo.Produtos.Count,
                Ativo = promo.Ativo, CriadoEm = promo.CriadoEm
            };
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em PromocaoService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, PromocaoFormDto dto)
    {
        try
        {
            Validar(dto);
            var promo = await _db.Set<Promocao>()
                .Include(p => p.Filiais).Include(p => p.Pagamentos)
                .Include(p => p.Convenios).Include(p => p.Faixas).Include(p => p.Produtos)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException($"Promoção {id} não encontrada.");

            var anterior = ParaDict(promo);

            promo.Nome = dto.Nome.Trim().ToUpper();
            promo.Tipo = dto.Tipo;
            promo.DataHoraInicio = dto.DataHoraInicio;
            promo.DataHoraFim = dto.DataHoraFim;
            promo.DiaSemana = dto.DiaSemana;
            promo.PermitirMudarPreco = dto.PermitirMudarPreco;
            promo.GerarComissao = dto.GerarComissao;
            promo.ExclusivaConvenio = dto.ExclusivaConvenio;
            promo.ReducaoVendaPrazo = dto.ReducaoVendaPrazo;
            promo.QtdeMaxPorVenda = dto.QtdeMaxPorVenda;
            promo.LancarPorQuantidade = dto.LancarPorQuantidade;
            promo.DataInicioContagem = dto.DataInicioContagem;
            promo.Ativo = dto.Ativo;

            // Recriar sub-tabelas
            _db.Set<PromocaoFilial>().RemoveRange(promo.Filiais);
            _db.Set<PromocaoPagamento>().RemoveRange(promo.Pagamentos);
            _db.Set<PromocaoConvenio>().RemoveRange(promo.Convenios);
            _db.Set<PromocaoFaixa>().RemoveRange(promo.Faixas);
            _db.Set<PromocaoProduto>().RemoveRange(promo.Produtos);

            foreach (var fid in dto.FilialIds.Distinct())
                promo.Filiais.Add(new PromocaoFilial { FilialId = fid });
            foreach (var pid in dto.PagamentoIds.Distinct())
                promo.Pagamentos.Add(new PromocaoPagamento { TipoPagamentoId = pid });
            foreach (var cid in dto.ConvenioIds.Distinct())
                promo.Convenios.Add(new PromocaoConvenio { ConvenioId = cid });
            foreach (var faixa in dto.Faixas)
                promo.Faixas.Add(new PromocaoFaixa { Quantidade = faixa.Quantidade, PercentualDesconto = faixa.PercentualDesconto });
            foreach (var prod in dto.Produtos)
                promo.Produtos.Add(new PromocaoProduto
                {
                    ProdutoId = prod.ProdutoId,
                    PercentualPromocao = prod.PercentualPromocao,
                    ValorPromocao = prod.ValorPromocao,
                    PercentualLucro = prod.PercentualLucro,
                    QtdeLimite = prod.QtdeLimite,
                    PercentualAposLimite = prod.PercentualAposLimite,
                    ValorAposLimite = prod.ValorAposLimite
                });

            await _db.SaveChangesAsync();
            var novo = ParaDict(promo);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em PromocaoService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var promo = await _db.Set<Promocao>()
                .Include(p => p.Filiais).Include(p => p.Pagamentos)
                .Include(p => p.Convenios).Include(p => p.Faixas).Include(p => p.Produtos)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException($"Promoção {id} não encontrada.");
            var dados = ParaDict(promo);
            _db.Set<PromocaoFilial>().RemoveRange(promo.Filiais);
            _db.Set<PromocaoPagamento>().RemoveRange(promo.Pagamentos);
            _db.Set<PromocaoConvenio>().RemoveRange(promo.Convenios);
            _db.Set<PromocaoFaixa>().RemoveRange(promo.Faixas);
            _db.Set<PromocaoProduto>().RemoveRange(promo.Produtos);
            _db.Set<Promocao>().Remove(promo);
            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var rec = await _db.Set<Promocao>().FindAsync(id);
                rec!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException) { Log.Error(ex, "Erro em PromocaoService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    private static void Validar(PromocaoFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
        if (dto.DiaSemana <= 0 || dto.DiaSemana > 127) throw new ArgumentException("Selecione ao menos um dia da semana.");
        if (dto.FilialIds.Count == 0) throw new ArgumentException("Selecione ao menos uma filial.");
        if (dto.PagamentoIds.Count == 0) throw new ArgumentException("Selecione ao menos uma forma de pagamento.");
        if (dto.ReducaoVendaPrazo < 0 || dto.ReducaoVendaPrazo > 90) throw new ArgumentException("Redução venda a prazo deve ser entre 0% e 90%.");
    }

    private static Dictionary<string, string?> ParaDict(Promocao p) => new()
    {
        ["Nome"] = p.Nome,
        ["Tipo"] = p.Tipo == TipoPromocao.Fixa ? "Fixa" : "Progressiva",
        ["DataHoraInicio"] = p.DataHoraInicio.ToString("dd/MM/yyyy HH:mm"),
        ["DataHoraFim"] = p.DataHoraFim?.ToString("dd/MM/yyyy HH:mm"),
        ["DiaSemana"] = p.DiaSemana.ToString(),
        ["Produtos"] = p.Produtos.Count.ToString(),
        ["Ativo"] = p.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b) =>
        a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
