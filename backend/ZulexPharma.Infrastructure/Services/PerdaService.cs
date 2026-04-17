using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.Sngpc;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Registra perdas de estoque como Vendas com <see cref="TipoOperacao.Perda"/>.
/// O lote afetado é rastreado via <see cref="MovimentoLote.VendaId"/> (criado pelo IProdutoLoteService).
/// </summary>
public class PerdaService : IPerdaService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private readonly IProdutoLoteService _loteService;
    private const string TELA = "Perdas";
    private const string ENTIDADE = "Perda";

    public PerdaService(AppDbContext db, ILogAcaoService log, IProdutoLoteService loteService)
    {
        _db = db;
        _log = log;
        _loteService = loteService;
    }

    public async Task<List<PerdaListDto>> ListarAsync(long? filialId = null, DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        var vendasQ = _db.Vendas
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Where(v => v.TipoOperacao == TipoOperacao.Perda);

        if (filialId.HasValue) vendasQ = vendasQ.Where(v => v.FilialId == filialId.Value);
        if (dataInicio.HasValue) vendasQ = vendasQ.Where(v => v.DataFinalizacao >= dataInicio.Value);
        if (dataFim.HasValue) vendasQ = vendasQ.Where(v => v.DataFinalizacao <= dataFim.Value);

        var vendas = await vendasQ.OrderByDescending(v => v.DataFinalizacao).ToListAsync();
        if (vendas.Count == 0) return new();

        var vendaIds = vendas.Select(v => v.Id).ToList();

        // MovimentoLote rastreia o lote baixado e o usuário que registrou a perda
        var movs = await _db.MovimentosLote
            .Include(m => m.ProdutoLote)
            .Include(m => m.Usuario)
            .Where(m => m.VendaId != null && vendaIds.Contains(m.VendaId.Value) && m.Tipo == TipoMovimentoLote.Perda)
            .ToListAsync();

        var movPorVenda = movs.GroupBy(m => m.VendaId!.Value).ToDictionary(g => g.Key, g => g.First());

        return vendas.Select(v =>
        {
            var item = v.Itens.FirstOrDefault();
            var mov = movPorVenda.GetValueOrDefault(v.Id);
            var lote = mov?.ProdutoLote;
            return new PerdaListDto
            {
                Id = v.Id,
                FilialId = v.FilialId,
                ProdutoId = item?.ProdutoId ?? 0,
                ProdutoNome = item?.Produto?.Nome,
                ProdutoLoteId = lote?.Id ?? 0,
                NumeroLote = lote?.NumeroLote,
                DataValidade = lote?.DataValidade,
                Quantidade = mov?.Quantidade ?? (item?.Quantidade ?? 0),
                DataPerda = v.DataFinalizacao ?? v.CriadoEm,
                Motivo = (int)(v.Motivo ?? MotivoPerda.Outro),
                MotivoNome = (v.Motivo ?? MotivoPerda.Outro).ToString(),
                NumeroBoletim = v.NumeroBoletim,
                Observacao = v.Observacao,
                UsuarioNome = mov?.Usuario?.Login
            };
        }).ToList();
    }

    public async Task<PerdaListDto> CriarAsync(PerdaFormDto dto, long? usuarioId)
    {
        if (dto.Quantidade <= 0) throw new ArgumentException("Quantidade deve ser maior que zero.");
        if (dto.ProdutoLoteId <= 0) throw new ArgumentException("Selecione um lote.");

        var lote = await _db.ProdutosLotes.FirstOrDefaultAsync(l => l.Id == dto.ProdutoLoteId)
            ?? throw new KeyNotFoundException($"Lote {dto.ProdutoLoteId} não encontrado.");

        if (lote.SaldoAtual < dto.Quantidade)
            throw new InvalidOperationException($"Saldo insuficiente no lote {lote.NumeroLote}: disponível {lote.SaldoAtual}, solicitado {dto.Quantidade}.");

        var motivo = (MotivoPerda)dto.Motivo;
        if ((motivo == MotivoPerda.Furto || motivo == MotivoPerda.Roubo)
            && string.IsNullOrWhiteSpace(dto.NumeroBoletim))
        {
            throw new ArgumentException("Informe o número do Boletim de Ocorrência para Furto ou Roubo.");
        }

        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == dto.ProdutoId)
            ?? throw new KeyNotFoundException($"Produto {dto.ProdutoId} não encontrado.");

        var dataPerda = DateTime.SpecifyKind(dto.DataPerda, DateTimeKind.Utc);

        // 1. Cria a Venda com TipoOperacao=Perda (cabeçalho) + VendaItem (produto perdido)
        var venda = new Venda
        {
            FilialId = dto.FilialId,
            TipoOperacao = TipoOperacao.Perda,
            ModeloDocumento = ModeloDocumento.SemDocumento,
            StatusFiscal = StatusFiscal.NaoEmitido,
            Status = VendaStatus.Finalizada,
            Origem = VendaOrigem.Caixa,
            DataPreVenda = DataHoraHelper.Agora(),
            DataFinalizacao = dataPerda,
            Motivo = motivo,
            NumeroBoletim = dto.NumeroBoletim,
            Observacao = dto.Observacao,
            TotalBruto = 0,
            TotalDesconto = 0,
            TotalLiquido = 0,
            TotalItens = 1
        };
        venda.Itens.Add(new VendaItem
        {
            ProdutoId = dto.ProdutoId,
            ProdutoCodigo = produto.Codigo ?? string.Empty,
            ProdutoNome = produto.Nome,
            Quantidade = (int)dto.Quantidade,
            PrecoVenda = 0,
            PrecoUnitario = 0,
            Total = 0,
            Ordem = 1
        });
        _db.Vendas.Add(venda);
        await _db.SaveChangesAsync();

        // 2. Baixa o lote via movimento (MovimentoLote rastreia VendaId + UsuarioId)
        await _loteService.RegistrarSaidaAsync(
            produtoLoteId: dto.ProdutoLoteId,
            quantidade: dto.Quantidade,
            tipo: TipoMovimentoLote.Perda,
            vendaId: venda.Id,
            usuarioId: usuarioId,
            observacao: $"Perda #{venda.Id} — {motivo}{(string.IsNullOrEmpty(dto.NumeroBoletim) ? "" : $" BO {dto.NumeroBoletim}")}");

        // 3. Baixa estoque do ProdutoDados (mantém sincronizado com lotes)
        var dados = await _db.ProdutosDados
            .FirstOrDefaultAsync(d => d.ProdutoId == dto.ProdutoId && d.FilialId == dto.FilialId);
        if (dados != null)
        {
            dados.EstoqueAtual = Math.Max(0, dados.EstoqueAtual - dto.Quantidade);
            await _db.SaveChangesAsync();
        }

        await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, venda.Id, novo: new Dictionary<string, string?>
        {
            ["Produto"] = dto.ProdutoId.ToString(),
            ["Lote"] = lote.NumeroLote,
            ["Quantidade"] = dto.Quantidade.ToString("N3"),
            ["Motivo"] = motivo.ToString(),
            ["BO"] = dto.NumeroBoletim
        });

        return (await ListarAsync(dto.FilialId)).First(x => x.Id == venda.Id);
    }

    public async Task ExcluirAsync(long id)
    {
        var venda = await _db.Vendas
            .Include(v => v.Itens)
            .FirstOrDefaultAsync(v => v.Id == id && v.TipoOperacao == TipoOperacao.Perda)
            ?? throw new KeyNotFoundException($"Perda {id} não encontrada.");

        // Recupera movimento de lote (pra saber qual lote estornar)
        var mov = await _db.MovimentosLote
            .Include(m => m.ProdutoLote)
            .FirstOrDefaultAsync(m => m.VendaId == id && m.Tipo == TipoMovimentoLote.Perda)
            ?? throw new InvalidOperationException($"Movimento de lote da perda {id} não encontrado.");

        var item = venda.Itens.FirstOrDefault()
            ?? throw new InvalidOperationException($"Perda {id} sem item associado.");

        // Reversão: devolve saldo ao lote e ao estoque
        await _loteService.RegistrarEntradaAsync(
            produtoId: item.ProdutoId,
            filialId: venda.FilialId,
            numeroLote: mov.ProdutoLote.NumeroLote,
            dataFabricacao: null,
            dataValidade: null,
            quantidade: mov.Quantidade,
            tipo: TipoMovimentoLote.Estorno,
            observacao: $"Estorno perda #{id}");

        var dados = await _db.ProdutosDados.FirstOrDefaultAsync(d => d.ProdutoId == item.ProdutoId && d.FilialId == venda.FilialId);
        if (dados != null)
        {
            dados.EstoqueAtual += mov.Quantidade;
        }
        _db.Vendas.Remove(venda);
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id);
    }
}
