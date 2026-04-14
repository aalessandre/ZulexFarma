using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Implementação do serviço de rastreabilidade por lote.
///
/// Princípios:
///  • Cada lote (<c>ProdutoLote</c>) é identificado pela tupla (filial, produto, número, validade).
///    Ao chegar uma nova entrada, se já existe um lote com essa tupla, o saldo é incrementado
///    (não se cria um segundo lote duplicado).
///  • Todo movimento é registrado em <c>MovimentoLote</c> com snapshot do saldo pós-movimento
///    para auditoria e reconstrução histórica.
///  • Lotes fictícios (<c>EhLoteFicticio=true</c>) são gerados automaticamente ao ativar o controle
///    retroativo de um grupo ou do SNGPC, usando o saldo atual do <c>ProdutoDados</c> como ponto zero.
/// </summary>
public class ProdutoLoteService : IProdutoLoteService
{
    private readonly AppDbContext _db;

    public ProdutoLoteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProdutoLote> RegistrarEntradaAsync(
        long produtoId,
        long filialId,
        string numeroLote,
        DateTime? dataFabricacao,
        DateTime? dataValidade,
        decimal quantidade,
        TipoMovimentoLote tipo,
        string? registroMs = null,
        long? fornecedorId = null,
        long? compraId = null,
        long? compraProdutoLoteId = null,
        long? usuarioId = null,
        string? observacao = null,
        bool ehLoteFicticio = false)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(numeroLote))
            numeroLote = "S/L";

        // Procura lote existente com mesma chave (permite incrementar)
        var agora = DateTime.UtcNow;
        var lote = await _db.ProdutosLotes
            .FirstOrDefaultAsync(l =>
                l.FilialId == filialId &&
                l.ProdutoId == produtoId &&
                l.NumeroLote == numeroLote &&
                l.DataValidade == dataValidade);

        if (lote == null)
        {
            lote = new ProdutoLote
            {
                ProdutoId = produtoId,
                FilialId = filialId,
                NumeroLote = numeroLote,
                DataFabricacao = dataFabricacao,
                DataValidade = dataValidade,
                SaldoAtual = 0,
                RegistroMs = registroMs,
                FornecedorId = fornecedorId,
                CompraId = compraId,
                EhLoteFicticio = ehLoteFicticio,
                Observacao = observacao,
                PrimeiraEntradaEm = agora,
                UltimaMovimentacaoEm = agora
            };
            _db.ProdutosLotes.Add(lote);
            await _db.SaveChangesAsync();
        }
        else
        {
            // Atualiza dados que podem ter chegado novos (ex: fabricação não tinha antes)
            if (lote.DataFabricacao == null && dataFabricacao != null)
                lote.DataFabricacao = dataFabricacao;
            if (string.IsNullOrEmpty(lote.RegistroMs) && !string.IsNullOrEmpty(registroMs))
                lote.RegistroMs = registroMs;
            lote.UltimaMovimentacaoEm = agora;
        }

        lote.SaldoAtual += quantidade;

        var mov = new MovimentoLote
        {
            ProdutoLoteId = lote.Id,
            Tipo = tipo,
            Quantidade = quantidade,
            DataMovimento = agora,
            UsuarioId = usuarioId,
            CompraId = compraId,
            CompraProdutoLoteId = compraProdutoLoteId,
            SaldoAposMovimento = lote.SaldoAtual,
            Observacao = observacao
        };
        _db.MovimentosLote.Add(mov);

        await _db.SaveChangesAsync();
        return lote;
    }

    public async Task<MovimentoLote> RegistrarSaidaAsync(
        long produtoLoteId,
        decimal quantidade,
        TipoMovimentoLote tipo,
        long? vendaId = null,
        long? usuarioId = null,
        string? observacao = null)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.");

        var lote = await _db.ProdutosLotes.FindAsync(produtoLoteId)
            ?? throw new KeyNotFoundException($"ProdutoLote {produtoLoteId} não encontrado.");

        if (lote.SaldoAtual < quantidade)
            throw new InvalidOperationException(
                $"Saldo insuficiente no lote {lote.NumeroLote}: disponível {lote.SaldoAtual}, solicitado {quantidade}.");

        var agora = DateTime.UtcNow;
        lote.SaldoAtual -= quantidade;
        lote.UltimaMovimentacaoEm = agora;

        var mov = new MovimentoLote
        {
            ProdutoLoteId = lote.Id,
            Tipo = tipo,
            Quantidade = quantidade,
            DataMovimento = agora,
            UsuarioId = usuarioId,
            VendaId = vendaId,
            SaldoAposMovimento = lote.SaldoAtual,
            Observacao = observacao
        };
        _db.MovimentosLote.Add(mov);

        await _db.SaveChangesAsync();
        return mov;
    }

    public async Task<List<ProdutoLote>> ListarLotesAtivosAsync(long produtoId, long filialId)
    {
        return await _db.ProdutosLotes
            .Where(l => l.ProdutoId == produtoId && l.FilialId == filialId && l.SaldoAtual > 0)
            // FEFO: lotes com validade mais próxima primeiro. Lotes sem validade no fim.
            .OrderBy(l => l.DataValidade == null ? 1 : 0)
            .ThenBy(l => l.DataValidade)
            .ThenBy(l => l.PrimeiraEntradaEm)
            .ToListAsync();
    }

    public async Task<int> GerarLotesFicticiosDoGrupoAsync(long grupoProdutoId, long? usuarioId = null)
    {
        // Busca todos os produtos desse grupo que têm saldo > 0 em alguma filial
        var produtos = await _db.Produtos
            .Where(p => p.GrupoProdutoId == grupoProdutoId && !p.Eliminado)
            .Select(p => p.Id)
            .ToListAsync();

        return await GerarLotesFicticiosParaProdutosAsync(produtos, usuarioId,
            "Lote inicial gerado automaticamente — ativação de controle por grupo");
    }

    public async Task<int> GerarLotesFicticiosSngpcAsync(long? usuarioId = null)
    {
        var produtos = await _db.Produtos
            .Where(p => !p.Eliminado &&
                (p.ClasseTerapeutica == ProdutoControleHelper.CLASSE_PSICOTROPICOS ||
                 p.ClasseTerapeutica == ProdutoControleHelper.CLASSE_ANTIMICROBIANO))
            .Select(p => p.Id)
            .ToListAsync();

        return await GerarLotesFicticiosParaProdutosAsync(produtos, usuarioId,
            "Lote inicial gerado automaticamente — ativação SNGPC");
    }

    private async Task<int> GerarLotesFicticiosParaProdutosAsync(
        List<long> produtoIds,
        long? usuarioId,
        string observacao)
    {
        int criados = 0;
        foreach (var produtoId in produtoIds)
        {
            // Pega os saldos por filial
            var dadosFiliais = await _db.ProdutosDados
                .Where(d => d.ProdutoId == produtoId && d.EstoqueAtual > 0)
                .Select(d => new { d.FilialId, d.EstoqueAtual })
                .ToListAsync();

            foreach (var d in dadosFiliais)
            {
                // Só cria se ainda não tem NENHUM lote rastreado pra esse produto nessa filial
                bool jaTemLote = await _db.ProdutosLotes
                    .AnyAsync(l => l.ProdutoId == produtoId && l.FilialId == d.FilialId);
                if (jaTemLote) continue;

                await RegistrarEntradaAsync(
                    produtoId: produtoId,
                    filialId: d.FilialId,
                    numeroLote: "S/L",
                    dataFabricacao: null,
                    dataValidade: null,
                    quantidade: d.EstoqueAtual,
                    tipo: TipoMovimentoLote.AjusteInicial,
                    usuarioId: usuarioId,
                    observacao: observacao,
                    ehLoteFicticio: true);
                criados++;
            }
        }
        Log.Information("ProdutoLoteService.GerarLotesFicticios — {Criados} lotes criados", criados);
        return criados;
    }
}
