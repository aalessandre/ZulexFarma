using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Vendas;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class VendaService : IVendaService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Venda";
    private const string ENTIDADE = "Venda";

    public VendaService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<VendaListDto>> ListarAsync(long? filialId = null, string? status = null)
    {
        try
        {
            var query = _db.Set<Venda>()
                .Include(v => v.Cliente).ThenInclude(c => c!.Pessoa)
                .Include(v => v.Colaborador).ThenInclude(c => c!.Pessoa)
                .Include(v => v.TipoPagamento)
                .AsQueryable();

            if (filialId.HasValue) query = query.Where(v => v.FilialId == filialId);
            if (status == "aberta") query = query.Where(v => v.Status == VendaStatus.Aberta);
            else if (status == "finalizada") query = query.Where(v => v.Status == VendaStatus.Finalizada);
            else if (status == "cancelada") query = query.Where(v => v.Status == VendaStatus.Cancelada);

            return await query.OrderByDescending(v => v.CriadoEm)
                .Select(v => new VendaListDto
                {
                    Id = v.Id, Codigo = v.Codigo, NrCesta = v.NrCesta,
                    ClienteNome = v.Cliente != null ? v.Cliente.Pessoa.Nome : null,
                    ColaboradorNome = v.Colaborador != null ? v.Colaborador.Pessoa.Nome : null,
                    TipoPagamentoNome = v.TipoPagamento != null ? v.TipoPagamento.Nome : null,
                    TotalLiquido = v.TotalLiquido, TotalItens = v.TotalItens,
                    Status = v.Status, StatusDescricao = StatusTexto(v.Status),
                    CriadoEm = v.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em VendaService.ListarAsync"); throw; }
    }

    public async Task<VendaDetalheDto?> ObterAsync(long id)
    {
        try
        {
            var v = await _db.Set<Venda>()
                .Include(x => x.Cliente).ThenInclude(c => c!.Pessoa)
                .Include(x => x.Colaborador).ThenInclude(c => c!.Pessoa)
                .Include(x => x.Itens).ThenInclude(i => i.Descontos)
                .Include(x => x.Pagamentos).ThenInclude(p => p.TipoPagamento)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return null;

            var prodIds = v.Itens.Select(i => i.ProdutoId).ToList();
            var dados = await _db.ProdutosDados
                .Where(d => prodIds.Contains(d.ProdutoId) && d.FilialId == v.FilialId)
                .Select(d => new { d.ProdutoId, d.EstoqueAtual })
                .ToListAsync();

            return new VendaDetalheDto
            {
                Id = v.Id, FilialId = v.FilialId, NrCesta = v.NrCesta,
                ClienteId = v.ClienteId, ClienteNome = v.Cliente?.Pessoa?.Nome,
                ColaboradorId = v.ColaboradorId, ColaboradorNome = v.Colaborador?.Pessoa?.Nome,
                TipoPagamentoId = v.TipoPagamentoId, ConvenioId = v.ConvenioId,
                TotalBruto = v.TotalBruto, TotalDesconto = v.TotalDesconto,
                TotalLiquido = v.TotalLiquido, TotalItens = v.TotalItens,
                Status = v.Status, Observacao = v.Observacao, CriadoEm = v.CriadoEm,
                Itens = v.Itens.OrderBy(i => i.Ordem).Select(i =>
                {
                    var d = dados.FirstOrDefault(x => x.ProdutoId == i.ProdutoId);
                    return new VendaItemDto
                    {
                        Id = i.Id, ProdutoId = i.ProdutoId, ProdutoCodigo = i.ProdutoCodigo,
                        ProdutoNome = i.ProdutoNome, Fabricante = i.Fabricante,
                        PrecoVenda = i.PrecoVenda, Quantidade = i.Quantidade,
                        PercentualDesconto = i.PercentualDesconto, PercentualPromocao = i.PercentualPromocao,
                        ValorDesconto = i.ValorDesconto,
                        PrecoUnitario = i.PrecoUnitario, Total = i.Total,
                        EstoqueAtual = d?.EstoqueAtual ?? 0,
                        Descontos = i.Descontos.Select(dd => new VendaItemDescontoDto
                        {
                            Id = dd.Id, Tipo = (int)dd.Tipo, Percentual = dd.Percentual,
                            Origem = dd.Origem, Regra = dd.Regra, OrigemId = dd.OrigemId,
                            LiberadoPorId = dd.LiberadoPorId
                        }).ToList()
                    };
                }).ToList(),
                Pagamentos = v.Pagamentos.Select(p => new VendaPagamentoDto
                {
                    Id = p.Id, TipoPagamentoId = p.TipoPagamentoId,
                    TipoPagamentoNome = p.TipoPagamento?.Nome ?? "",
                    Valor = p.Valor, Troco = p.Troco, TrocoPara = p.TrocoPara
                }).ToList()
            };
        }
        catch (Exception ex) { Log.Error(ex, "Erro em VendaService.ObterAsync | Id: {Id}", id); throw; }
    }

    public async Task<VendaDetalheDto> CriarAsync(VendaFormDto dto)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(dto.NrCesta))
            {
                var cestaEmUso = await _db.Set<Venda>().AnyAsync(v => v.NrCesta == dto.NrCesta.Trim() && v.Status == VendaStatus.Aberta);
                if (cestaEmUso) throw new ArgumentException($"O número de cesta \"{dto.NrCesta.Trim()}\" já está em uso por outra venda em aberto.");
            }

            var venda = new Venda
            {
                FilialId = dto.FilialId, CaixaId = dto.CaixaId, ClienteId = dto.ClienteId, ColaboradorId = dto.ColaboradorId,
                TipoPagamentoId = dto.TipoPagamentoId, ConvenioId = dto.ConvenioId,
                NrCesta = dto.NrCesta, Origem = (VendaOrigem)(dto.Origem ?? 1),
                Observacao = dto.Observacao, Status = VendaStatus.Aberta
            };

            int ordem = 1;
            foreach (var item in dto.Itens)
                venda.Itens.Add(MapearItem(item, ordem++));

            foreach (var pag in dto.Pagamentos.Where(p => p.Valor > 0))
                venda.Pagamentos.Add(new VendaPagamento { TipoPagamentoId = pag.TipoPagamentoId, Valor = pag.Valor, Troco = pag.Troco, TrocoPara = pag.TrocoPara });

            RecalcularTotais(venda);
            _db.Set<Venda>().Add(venda);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, venda.Id, novo: ParaDict(venda));

            return (await ObterAsync(venda.Id))!;
        }
        catch (Exception ex) { Log.Error(ex, "Erro em VendaService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, VendaFormDto dto)
    {
        try
        {
            var venda = await _db.Set<Venda>()
                .Include(v => v.Itens).ThenInclude(i => i.Descontos)
                .Include(v => v.Pagamentos)
                .FirstOrDefaultAsync(v => v.Id == id)
                ?? throw new KeyNotFoundException($"Venda {id} não encontrada.");

            if (venda.Status != VendaStatus.Aberta)
                throw new ArgumentException("Apenas vendas abertas podem ser alteradas.");

            if (!string.IsNullOrWhiteSpace(dto.NrCesta))
            {
                var cestaEmUso = await _db.Set<Venda>().AnyAsync(v => v.NrCesta == dto.NrCesta.Trim() && v.Status == VendaStatus.Aberta && v.Id != id);
                if (cestaEmUso) throw new ArgumentException($"O número de cesta \"{dto.NrCesta.Trim()}\" já está em uso por outra venda em aberto.");
            }

            venda.ClienteId = dto.ClienteId; venda.ColaboradorId = dto.ColaboradorId;
            venda.TipoPagamentoId = dto.TipoPagamentoId; venda.ConvenioId = dto.ConvenioId;
            venda.NrCesta = dto.NrCesta; venda.Observacao = dto.Observacao;

            foreach (var item in venda.Itens) _db.Set<VendaItemDesconto>().RemoveRange(item.Descontos);
            _db.Set<VendaItem>().RemoveRange(venda.Itens);
            _db.Set<VendaPagamento>().RemoveRange(venda.Pagamentos);
            int ordem = 1;
            foreach (var item in dto.Itens)
                venda.Itens.Add(MapearItem(item, ordem++));
            foreach (var pag in dto.Pagamentos.Where(p => p.Valor > 0))
                venda.Pagamentos.Add(new VendaPagamento { TipoPagamentoId = pag.TipoPagamentoId, Valor = pag.Valor, Troco = pag.Troco, TrocoPara = pag.TrocoPara });

            RecalcularTotais(venda);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em VendaService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task FinalizarAsync(long id)
    {
        try
        {
            var venda = await _db.Set<Venda>()
                .Include(v => v.Pagamentos).ThenInclude(p => p.TipoPagamento)
                .Include(v => v.Cliente)
                .FirstOrDefaultAsync(v => v.Id == id)
                ?? throw new KeyNotFoundException($"Venda {id} não encontrada.");
            if (venda.Status != VendaStatus.Aberta) throw new ArgumentException("Venda não está aberta.");
            venda.Status = VendaStatus.Finalizada;

            // Gerar Contas a Receber para cada pagamento
            var agora = Domain.Helpers.DataHoraHelper.Agora();
            foreach (var pag in venda.Pagamentos)
            {
                var modalidade = pag.TipoPagamento?.Modalidade;
                var planoContaId = pag.TipoPagamento?.PlanoContaId;
                var valorLiquido = pag.Valor - pag.Troco;

                // Dinheiro e PIX: já recebido na hora
                var jaRecebido = modalidade == Domain.Enums.ModalidadePagamento.VendaVista
                              || modalidade == Domain.Enums.ModalidadePagamento.VendaPix;

                var cr = new ContaReceber
                {
                    FilialId = venda.FilialId,
                    VendaId = venda.Id,
                    VendaPagamentoId = pag.Id,
                    ClienteId = venda.ClienteId,
                    TipoPagamentoId = pag.TipoPagamentoId,
                    PlanoContaId = planoContaId,
                    Descricao = $"Venda #{venda.Codigo ?? venda.Id.ToString()} - {pag.TipoPagamento?.Nome ?? ""}",
                    Valor = pag.Valor,
                    ValorLiquido = valorLiquido > 0 ? valorLiquido : pag.Valor,
                    DataEmissao = agora,
                    DataVencimento = agora.Date,
                    NumParcela = 1,
                    TotalParcelas = 1,
                    Status = jaRecebido ? Domain.Enums.StatusContaReceber.Recebida : Domain.Enums.StatusContaReceber.Aberta,
                    DataRecebimento = jaRecebido ? agora : null,
                    ValorRecebido = jaRecebido ? valorLiquido : 0
                };
                _db.ContasReceber.Add(cr);
            }

            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "FINALIZAÇÃO", ENTIDADE, id);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em VendaService.FinalizarAsync | Id: {Id}", id); throw; }
    }

    public async Task CancelarAsync(long id)
    {
        try
        {
            var venda = await _db.Set<Venda>().FindAsync(id)
                ?? throw new KeyNotFoundException($"Venda {id} não encontrada.");
            if (venda.Status != VendaStatus.Aberta) throw new ArgumentException("Venda não está aberta.");
            venda.Status = VendaStatus.Cancelada;
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CANCELAMENTO", ENTIDADE, id);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em VendaService.CancelarAsync | Id: {Id}", id); throw; }
    }

    private static VendaItem MapearItem(VendaItemFormDto dto, int ordem)
    {
        var percTotal = dto.PercentualDesconto + dto.PercentualPromocao;
        var valorDesconto = dto.PrecoVenda * dto.Quantidade * percTotal / 100;
        var precoUnit = dto.PrecoVenda * (1 - percTotal / 100);
        var total = precoUnit * dto.Quantidade;
        var item = new VendaItem
        {
            ProdutoId = dto.ProdutoId, ProdutoCodigo = dto.ProdutoCodigo, ProdutoNome = dto.ProdutoNome,
            Fabricante = dto.Fabricante, PrecoVenda = dto.PrecoVenda, Quantidade = dto.Quantidade,
            PercentualDesconto = dto.PercentualDesconto, PercentualPromocao = dto.PercentualPromocao,
            ValorDesconto = Math.Round(valorDesconto, 2),
            PrecoUnitario = Math.Round(precoUnit, 2), Total = Math.Round(total, 2), Ordem = ordem
        };
        foreach (var d in dto.Descontos)
        {
            item.Descontos.Add(new VendaItemDesconto
            {
                Tipo = (TipoDescontoVenda)d.Tipo,
                Percentual = d.Percentual, Origem = d.Origem, Regra = d.Regra,
                OrigemId = d.OrigemId, LiberadoPorId = d.LiberadoPorId
            });
        }
        return item;
    }

    private static void RecalcularTotais(Venda v)
    {
        v.TotalBruto = v.Itens.Sum(i => i.PrecoVenda * i.Quantidade);
        v.TotalDesconto = v.Itens.Sum(i => i.ValorDesconto);
        v.TotalLiquido = v.Itens.Sum(i => i.Total);
        v.TotalItens = v.Itens.Count;
    }

    private static string StatusTexto(VendaStatus s) => s switch
    {
        VendaStatus.Aberta => "Aberta",
        VendaStatus.Finalizada => "Finalizada",
        VendaStatus.Cancelada => "Cancelada",
        _ => ""
    };

    private static Dictionary<string, string?> ParaDict(Venda v) => new()
    {
        ["TotalItens"] = v.TotalItens.ToString(), ["TotalLiquido"] = v.TotalLiquido.ToString("N2"),
        ["Status"] = StatusTexto(v.Status)
    };
}
