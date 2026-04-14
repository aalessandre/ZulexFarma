using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.Sngpc;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

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
        var q = _db.Perdas
            .Include(p => p.Produto)
            .Include(p => p.ProdutoLote)
            .Include(p => p.Usuario)
            .AsQueryable();

        if (filialId.HasValue) q = q.Where(p => p.FilialId == filialId.Value);
        if (dataInicio.HasValue) q = q.Where(p => p.DataPerda >= dataInicio.Value);
        if (dataFim.HasValue) q = q.Where(p => p.DataPerda <= dataFim.Value);

        return await q.OrderByDescending(p => p.DataPerda)
            .Select(p => new PerdaListDto
            {
                Id = p.Id,
                FilialId = p.FilialId,
                ProdutoId = p.ProdutoId,
                ProdutoNome = p.Produto.Nome,
                ProdutoLoteId = p.ProdutoLoteId,
                NumeroLote = p.ProdutoLote.NumeroLote,
                DataValidade = p.ProdutoLote.DataValidade,
                Quantidade = p.Quantidade,
                DataPerda = p.DataPerda,
                Motivo = (int)p.Motivo,
                MotivoNome = p.Motivo.ToString(),
                NumeroBoletim = p.NumeroBoletim,
                Observacao = p.Observacao,
                UsuarioNome = p.Usuario != null ? p.Usuario.Login : null
            }).ToListAsync();
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

        // 1. Cria o registro de perda
        var perda = new Perda
        {
            FilialId = dto.FilialId,
            ProdutoId = dto.ProdutoId,
            ProdutoLoteId = dto.ProdutoLoteId,
            Quantidade = dto.Quantidade,
            DataPerda = DateTime.SpecifyKind(dto.DataPerda, DateTimeKind.Utc),
            Motivo = motivo,
            NumeroBoletim = dto.NumeroBoletim,
            Observacao = dto.Observacao,
            UsuarioId = usuarioId
        };
        _db.Perdas.Add(perda);
        await _db.SaveChangesAsync();

        // 2. Baixa o lote via movimento
        await _loteService.RegistrarSaidaAsync(
            produtoLoteId: dto.ProdutoLoteId,
            quantidade: dto.Quantidade,
            tipo: TipoMovimentoLote.Perda,
            usuarioId: usuarioId,
            observacao: $"Perda #{perda.Id} — {motivo}{(string.IsNullOrEmpty(dto.NumeroBoletim) ? "" : $" BO {dto.NumeroBoletim}")}");

        // 3. Baixa estoque do ProdutoDados (mantém sincronizado com lotes)
        var dados = await _db.ProdutosDados
            .FirstOrDefaultAsync(d => d.ProdutoId == dto.ProdutoId && d.FilialId == dto.FilialId);
        if (dados != null)
        {
            dados.EstoqueAtual = Math.Max(0, dados.EstoqueAtual - dto.Quantidade);
            await _db.SaveChangesAsync();
        }

        await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, perda.Id, novo: new Dictionary<string, string?>
        {
            ["Produto"] = dto.ProdutoId.ToString(),
            ["Lote"] = lote.NumeroLote,
            ["Quantidade"] = dto.Quantidade.ToString("N3"),
            ["Motivo"] = motivo.ToString(),
            ["BO"] = dto.NumeroBoletim
        });

        return (await ListarAsync(dto.FilialId)).First(x => x.Id == perda.Id);
    }

    public async Task ExcluirAsync(long id)
    {
        var p = await _db.Perdas.FindAsync(id)
            ?? throw new KeyNotFoundException($"Perda {id} não encontrada.");

        // Reversão: devolve saldo ao lote e ao estoque
        await _loteService.RegistrarEntradaAsync(
            produtoId: p.ProdutoId,
            filialId: p.FilialId,
            numeroLote: (await _db.ProdutosLotes.FindAsync(p.ProdutoLoteId))!.NumeroLote,
            dataFabricacao: null,
            dataValidade: null,
            quantidade: p.Quantidade,
            tipo: TipoMovimentoLote.Estorno,
            observacao: $"Estorno perda #{id}");

        var dados = await _db.ProdutosDados.FirstOrDefaultAsync(d => d.ProdutoId == p.ProdutoId && d.FilialId == p.FilialId);
        if (dados != null)
        {
            dados.EstoqueAtual += p.Quantidade;
        }
        _db.Perdas.Remove(p);
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id);
    }
}
