using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Sngpc;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class VendaReceitaService : IVendaReceitaService
{
    private readonly AppDbContext _db;
    private readonly IProdutoLoteService _loteService;
    private readonly ILogAcaoService _log;
    private const string TELA = "SNGPC Venda";

    public VendaReceitaService(AppDbContext db, IProdutoLoteService loteService, ILogAcaoService log)
    {
        _db = db;
        _loteService = loteService;
        _log = log;
    }

    public async Task<List<ItemControladoDto>> ListarItensControladosPreviewAsync(ItensControladosPreviewRequest request)
    {
        try
        {
            if (request?.Itens == null || request.Itens.Count == 0)
                return new List<ItemControladoDto>();

            var produtoIds = request.Itens.Select(i => i.ProdutoId).Distinct().ToList();
            var produtos = await _db.Produtos
                .Where(p => produtoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var resultado = new List<ItemControladoDto>();
            foreach (var item in request.Itens)
            {
                if (!produtos.TryGetValue(item.ProdutoId, out var produto)) continue;
                if (!ProdutoControleHelper.IsProdutoSngpc(produto)) continue;

                var lotes = await _loteService.ListarLotesAtivosAsync(item.ProdutoId, request.FilialId);
                resultado.Add(new ItemControladoDto
                {
                    VendaItemId = 0, // ainda não existe — frontend usa ProdutoId como chave temporária
                    ProdutoId = item.ProdutoId,
                    ProdutoNome = produto.Nome,
                    ClasseTerapeutica = produto.ClasseTerapeutica,
                    Quantidade = item.Quantidade,
                    LotesDisponiveis = lotes.Select(l => new LoteDisponivelDto
                    {
                        ProdutoLoteId = l.Id,
                        NumeroLote = l.NumeroLote,
                        DataValidade = l.DataValidade,
                        SaldoAtual = l.SaldoAtual
                    }).ToList()
                });
            }
            return resultado;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em VendaReceitaService.ListarItensControladosPreviewAsync");
            throw;
        }
    }

    public async Task<List<ItemControladoDto>> ListarItensControladosAsync(long vendaId)
    {
        try
        {
            var venda = await _db.Vendas
                .Include(v => v.Itens).ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(v => v.Id == vendaId)
                ?? throw new KeyNotFoundException($"Venda {vendaId} não encontrada.");

            var resultado = new List<ItemControladoDto>();
            foreach (var item in venda.Itens)
            {
                if (item.Produto == null) continue;
                if (!ProdutoControleHelper.IsProdutoSngpc(item.Produto)) continue;

                var lotes = await _loteService.ListarLotesAtivosAsync(item.ProdutoId, venda.FilialId);
                resultado.Add(new ItemControladoDto
                {
                    VendaItemId = item.Id,
                    ProdutoId = item.ProdutoId,
                    ProdutoNome = item.ProdutoNome,
                    ClasseTerapeutica = item.Produto.ClasseTerapeutica,
                    Quantidade = item.Quantidade,
                    LotesDisponiveis = lotes.Select(l => new LoteDisponivelDto
                    {
                        ProdutoLoteId = l.Id,
                        NumeroLote = l.NumeroLote,
                        DataValidade = l.DataValidade,
                        SaldoAtual = l.SaldoAtual
                    }).ToList()
                });
            }
            return resultado;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            Log.Error(ex, "Erro em VendaReceitaService.ListarItensControladosAsync | VendaId: {VendaId}", vendaId);
            throw;
        }
    }

    public async Task RegistrarReceitasAsync(long vendaId, List<VendaReceitaFormDto> receitas, long? usuarioId = null)
    {
        try
        {
            if (receitas == null || receitas.Count == 0)
                throw new ArgumentException("Informe ao menos uma receita.");

            var venda = await _db.Vendas
                .Include(v => v.Itens).ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(v => v.Id == vendaId)
                ?? throw new KeyNotFoundException($"Venda {vendaId} não encontrada.");

            // Itens controlados que precisam estar cobertos por alguma receita
            var itensControlados = venda.Itens
                .Where(i => i.Produto != null && ProdutoControleHelper.IsProdutoSngpc(i.Produto))
                .ToDictionary(i => i.Id, i => i);

            var itensCobertos = new HashSet<long>();
            foreach (var rec in receitas)
                foreach (var it in rec.Itens)
                    itensCobertos.Add(it.VendaItemId);

            var faltando = itensControlados.Keys.Where(k => !itensCobertos.Contains(k)).ToList();
            if (faltando.Count > 0)
                throw new ArgumentException(
                    $"Faltam {faltando.Count} item(ns) controlado(s) sem receita atribuída.");

            foreach (var rec in receitas)
            {
                var prescritorId = await ResolverPrescritorAsync(rec);
                ValidarReceita(rec);

                var receita = new VendaReceita
                {
                    VendaId = vendaId,
                    FilialId = venda.FilialId,
                    Tipo = rec.Tipo,
                    NumeroNotificacao = string.IsNullOrWhiteSpace(rec.NumeroNotificacao) ? null : rec.NumeroNotificacao.Trim(),
                    DataEmissao = rec.DataEmissao,
                    DataValidade = rec.DataValidade,
                    Cid = string.IsNullOrWhiteSpace(rec.Cid) ? null : rec.Cid.Trim().ToUpper(),
                    PrescritorId = prescritorId,
                    PacienteNome = rec.PacienteNome.Trim().ToUpper(),
                    PacienteCpf = SoDigitosOuNull(rec.PacienteCpf),
                    PacienteRg = SoDigitosOuNull(rec.PacienteRg),
                    PacienteNascimento = rec.PacienteNascimento,
                    PacienteSexo = rec.PacienteSexo?.Trim().ToUpper(),
                    PacienteEndereco = rec.PacienteEndereco?.Trim().ToUpper(),
                    PacienteNumero = rec.PacienteNumero?.Trim(),
                    PacienteBairro = rec.PacienteBairro?.Trim().ToUpper(),
                    PacienteCidade = rec.PacienteCidade?.Trim().ToUpper(),
                    PacienteUf = rec.PacienteUf?.Trim().ToUpper(),
                    PacienteCep = rec.PacienteCep?.Trim(),
                    PacienteTelefone = rec.PacienteTelefone?.Trim(),
                    CompradorMesmoPaciente = rec.CompradorMesmoPaciente,
                    CompradorNome = rec.CompradorMesmoPaciente ? null : rec.CompradorNome?.Trim().ToUpper(),
                    CompradorCpf = rec.CompradorMesmoPaciente ? null : SoDigitosOuNull(rec.CompradorCpf),
                    CompradorRg = rec.CompradorMesmoPaciente ? null : SoDigitosOuNull(rec.CompradorRg),
                    CompradorEndereco = rec.CompradorMesmoPaciente ? null : rec.CompradorEndereco?.Trim().ToUpper(),
                    CompradorNumero = rec.CompradorMesmoPaciente ? null : rec.CompradorNumero?.Trim(),
                    CompradorBairro = rec.CompradorMesmoPaciente ? null : rec.CompradorBairro?.Trim().ToUpper(),
                    CompradorCidade = rec.CompradorMesmoPaciente ? null : rec.CompradorCidade?.Trim().ToUpper(),
                    CompradorUf = rec.CompradorMesmoPaciente ? null : rec.CompradorUf?.Trim().ToUpper(),
                    CompradorCep = rec.CompradorMesmoPaciente ? null : rec.CompradorCep?.Trim(),
                    CompradorTelefone = rec.CompradorMesmoPaciente ? null : rec.CompradorTelefone?.Trim()
                };
                _db.VendaReceitas.Add(receita);
                await _db.SaveChangesAsync();

                foreach (var itForm in rec.Itens)
                {
                    if (!itensControlados.TryGetValue(itForm.VendaItemId, out var vendaItem))
                        throw new ArgumentException($"Item {itForm.VendaItemId} não é controlado nessa venda.");

                    var lote = await _db.ProdutosLotes.FindAsync(itForm.ProdutoLoteId)
                        ?? throw new ArgumentException($"Lote {itForm.ProdutoLoteId} não encontrado.");
                    if (lote.ProdutoId != vendaItem.ProdutoId || lote.FilialId != venda.FilialId)
                        throw new ArgumentException($"Lote {lote.NumeroLote} não pertence ao produto {vendaItem.ProdutoNome}.");
                    if (lote.SaldoAtual < itForm.Quantidade)
                        throw new ArgumentException($"Saldo insuficiente no lote {lote.NumeroLote}: {lote.SaldoAtual} disponível.");

                    // Baixa o lote
                    await _loteService.RegistrarSaidaAsync(
                        produtoLoteId: lote.Id,
                        quantidade: itForm.Quantidade,
                        tipo: TipoMovimentoLote.Saida,
                        vendaId: vendaId,
                        usuarioId: usuarioId,
                        observacao: $"Venda {venda.Codigo ?? vendaId.ToString()} — SNGPC");

                    _db.VendaReceitaItens.Add(new VendaReceitaItem
                    {
                        VendaReceitaId = receita.Id,
                        VendaItemId = vendaItem.Id,
                        ProdutoId = vendaItem.ProdutoId,
                        ProdutoLoteId = lote.Id,
                        Quantidade = itForm.Quantidade
                    });
                }
                await _db.SaveChangesAsync();

                await _log.RegistrarAsync(TELA, "LANÇAMENTO RECEITA", "VendaReceita", receita.Id, novo: new Dictionary<string, string?>
                {
                    ["Venda"] = venda.Codigo ?? venda.Id.ToString(),
                    ["Tipo"] = rec.Tipo.ToString(),
                    ["Prescritor"] = receita.PacienteNome,
                    ["Itens"] = rec.Itens.Count.ToString()
                });
            }

            // Desmarca pendência se estava marcada
            if (venda.SngpcPendente)
            {
                venda.SngpcPendente = false;
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception ex) when (ex is not ArgumentException and not KeyNotFoundException)
        {
            Log.Error(ex, "Erro em VendaReceitaService.RegistrarReceitasAsync | VendaId: {VendaId}", vendaId);
            throw;
        }
    }

    public async Task<List<VendaSngpcPendenteDto>> ListarVendasSngpcAsync(string? filtro = null, long? filialId = null, DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        try
        {
            var f = (filtro ?? "todas").ToLower();

            // ── 1) Vendas finalizadas com controlados ─────────────
            var resultado = new List<VendaSngpcPendenteDto>();

            if (f != "manuais")
            {
                var q = _db.Vendas
                    .Include(v => v.Cliente).ThenInclude(c => c!.Pessoa)
                    .Include(v => v.Itens).ThenInclude(i => i.Produto)
                    .Include(v => v.Receitas)
                    .Where(v => v.Status == Domain.Enums.VendaStatus.Finalizada
                        && v.Itens.Any(i => i.Produto != null
                            && (i.Produto.ClasseTerapeutica == ProdutoControleHelper.CLASSE_PSICOTROPICOS
                             || i.Produto.ClasseTerapeutica == ProdutoControleHelper.CLASSE_ANTIMICROBIANO)));

                if (f == "pendentes") q = q.Where(v => v.SngpcPendente || !v.Receitas.Any());
                else if (f == "lancadas") q = q.Where(v => !v.SngpcPendente && v.Receitas.Any());

                if (filialId.HasValue) q = q.Where(v => v.FilialId == filialId.Value);
                if (dataInicio.HasValue) q = q.Where(v => v.DataFinalizacao >= dataInicio.Value.Date);
                if (dataFim.HasValue) { var fim = dataFim.Value.Date.AddDays(1); q = q.Where(v => v.DataFinalizacao < fim); }

                var vendas = await q.OrderByDescending(v => v.DataFinalizacao ?? v.DataPreVenda).ToListAsync();

                resultado.AddRange(vendas.Select(v =>
                {
                    var controlados = v.Itens.Where(i => i.Produto != null && ProdutoControleHelper.IsProdutoSngpc(i.Produto)).ToList();
                    var temReceita = v.Receitas.Any();
                    return new VendaSngpcPendenteDto
                    {
                        VendaId = v.Id,
                        ReceitaId = null,
                        Codigo = v.Codigo,
                        DataFinalizacao = v.DataFinalizacao,
                        ClienteNome = v.Cliente?.Pessoa?.Nome,
                        QtdeItensControlados = controlados.Count,
                        QtdeTotal = controlados.Sum(i => (decimal)i.Quantidade),
                        Status = temReceita ? "Lançada" : "Pendente",
                        QtdeReceitas = v.Receitas.Count
                    };
                }));
            }

            // ── 2) Receitas manuais (sem venda) — linhas amarelas ──
            if (f != "pendentes" && f != "lancadas")
            {
                var qm = _db.VendaReceitas
                    .Include(r => r.Itens).ThenInclude(i => i.Produto)
                    .Where(r => r.VendaId == null);

                if (filialId.HasValue) qm = qm.Where(r => r.FilialId == filialId.Value);
                if (dataInicio.HasValue) qm = qm.Where(r => r.DataEmissao >= dataInicio.Value.Date);
                if (dataFim.HasValue) { var fimM = dataFim.Value.Date.AddDays(1); qm = qm.Where(r => r.DataEmissao < fimM); }

                var manuais = await qm.OrderByDescending(r => r.DataEmissao).ToListAsync();

                resultado.AddRange(manuais.Select(r => new VendaSngpcPendenteDto
                {
                    VendaId = null,
                    ReceitaId = r.Id,
                    Codigo = $"M-{r.Id}",
                    DataFinalizacao = r.DataEmissao,
                    ClienteNome = r.PacienteNome,
                    QtdeItensControlados = r.Itens.Count,
                    QtdeTotal = r.Itens.Sum(i => i.Quantidade),
                    Status = "Manual",
                    QtdeReceitas = 1
                }));
            }

            return resultado.OrderByDescending(x => x.DataFinalizacao).ToList();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em VendaReceitaService.ListarVendasSngpcAsync"); throw; }
    }

    public async Task<List<VendaReceitaListDto>> ListarReceitasAsync(long vendaId)
    {
        try
        {
            return await _db.VendaReceitas
                .Include(r => r.Prescritor)
                .Include(r => r.Itens)
                .Where(r => r.VendaId == vendaId)
                .OrderBy(r => r.Id)
                .Select(r => new VendaReceitaListDto
                {
                    Id = r.Id,
                    VendaId = r.VendaId,
                    Tipo = r.Tipo,
                    NumeroNotificacao = r.NumeroNotificacao,
                    DataEmissao = r.DataEmissao,
                    DataValidade = r.DataValidade,
                    PrescritorNome = r.Prescritor.Nome,
                    PacienteNome = r.PacienteNome,
                    QtdeItens = r.Itens.Count,
                    CriadoEm = r.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em VendaReceitaService.ListarReceitasAsync | VendaId: {VendaId}", vendaId); throw; }
    }

    // ─── Receita Manual (sem venda) ──────────────────────────────

    public async Task<long> RegistrarReceitaManualAsync(VendaReceitaFormDto rec, long filialId, long? usuarioId = null)
    {
        try
        {
            if (rec == null) throw new ArgumentException("Informe os dados da receita.");
            ValidarReceita(rec);

            var prescritorId = await ResolverPrescritorAsync(rec);

            var receita = new VendaReceita
            {
                VendaId = null,
                FilialId = filialId,
                Tipo = rec.Tipo,
                NumeroNotificacao = string.IsNullOrWhiteSpace(rec.NumeroNotificacao) ? null : rec.NumeroNotificacao.Trim(),
                DataEmissao = rec.DataEmissao,
                DataValidade = rec.DataValidade,
                Cid = string.IsNullOrWhiteSpace(rec.Cid) ? null : rec.Cid.Trim().ToUpper(),
                PrescritorId = prescritorId,
                PacienteNome = rec.PacienteNome.Trim().ToUpper(),
                PacienteCpf = SoDigitosOuNull(rec.PacienteCpf),
                PacienteRg = SoDigitosOuNull(rec.PacienteRg),
                PacienteNascimento = rec.PacienteNascimento,
                PacienteSexo = rec.PacienteSexo?.Trim().ToUpper(),
                PacienteEndereco = rec.PacienteEndereco?.Trim().ToUpper(),
                PacienteNumero = rec.PacienteNumero?.Trim(),
                PacienteBairro = rec.PacienteBairro?.Trim().ToUpper(),
                PacienteCidade = rec.PacienteCidade?.Trim().ToUpper(),
                PacienteUf = rec.PacienteUf?.Trim().ToUpper(),
                PacienteCep = rec.PacienteCep?.Trim(),
                PacienteTelefone = rec.PacienteTelefone?.Trim(),
                CompradorMesmoPaciente = rec.CompradorMesmoPaciente,
                CompradorNome = rec.CompradorMesmoPaciente ? null : rec.CompradorNome?.Trim().ToUpper(),
                CompradorCpf = rec.CompradorMesmoPaciente ? null : SoDigitosOuNull(rec.CompradorCpf),
                CompradorRg = rec.CompradorMesmoPaciente ? null : SoDigitosOuNull(rec.CompradorRg),
                CompradorEndereco = rec.CompradorMesmoPaciente ? null : rec.CompradorEndereco?.Trim().ToUpper(),
                CompradorNumero = rec.CompradorMesmoPaciente ? null : rec.CompradorNumero?.Trim(),
                CompradorBairro = rec.CompradorMesmoPaciente ? null : rec.CompradorBairro?.Trim().ToUpper(),
                CompradorCidade = rec.CompradorMesmoPaciente ? null : rec.CompradorCidade?.Trim().ToUpper(),
                CompradorUf = rec.CompradorMesmoPaciente ? null : rec.CompradorUf?.Trim().ToUpper(),
                CompradorCep = rec.CompradorMesmoPaciente ? null : rec.CompradorCep?.Trim(),
                CompradorTelefone = rec.CompradorMesmoPaciente ? null : rec.CompradorTelefone?.Trim()
            };
            _db.VendaReceitas.Add(receita);
            await _db.SaveChangesAsync();

            foreach (var itForm in rec.Itens)
            {
                if (!itForm.ProdutoId.HasValue || itForm.ProdutoId.Value <= 0)
                    throw new ArgumentException("Item da receita manual precisa de ProdutoId.");

                var produto = await _db.Produtos.FindAsync(itForm.ProdutoId.Value)
                    ?? throw new ArgumentException($"Produto {itForm.ProdutoId} não encontrado.");
                if (!ProdutoControleHelper.IsProdutoSngpc(produto))
                    throw new ArgumentException($"Produto {produto.Nome} não é controlado SNGPC.");

                var lote = await _db.ProdutosLotes.FindAsync(itForm.ProdutoLoteId)
                    ?? throw new ArgumentException($"Lote {itForm.ProdutoLoteId} não encontrado.");
                if (lote.ProdutoId != produto.Id || lote.FilialId != filialId)
                    throw new ArgumentException($"Lote {lote.NumeroLote} não pertence ao produto {produto.Nome} nessa filial.");
                if (lote.SaldoAtual < itForm.Quantidade)
                    throw new ArgumentException($"Saldo insuficiente no lote {lote.NumeroLote}: {lote.SaldoAtual} disponível.");

                await _loteService.RegistrarSaidaAsync(
                    produtoLoteId: lote.Id,
                    quantidade: itForm.Quantidade,
                    tipo: TipoMovimentoLote.Saida,
                    vendaId: null,
                    usuarioId: usuarioId,
                    observacao: $"Receita manual #{receita.Id} — SNGPC");

                _db.VendaReceitaItens.Add(new VendaReceitaItem
                {
                    VendaReceitaId = receita.Id,
                    VendaItemId = null,
                    ProdutoId = produto.Id,
                    ProdutoLoteId = lote.Id,
                    Quantidade = itForm.Quantidade
                });
            }
            await _db.SaveChangesAsync();

            await _log.RegistrarAsync(TELA, "LANÇAMENTO RECEITA MANUAL", "VendaReceita", receita.Id, novo: new Dictionary<string, string?>
            {
                ["Tipo"] = rec.Tipo.ToString(),
                ["Paciente"] = receita.PacienteNome,
                ["Itens"] = rec.Itens.Count.ToString()
            });
            return receita.Id;
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            Log.Error(ex, "Erro em VendaReceitaService.RegistrarReceitaManualAsync");
            throw;
        }
    }

    public async Task<List<DetalheReceitaItemDto>> ObterDetalhesAsync(long? vendaId, long? receitaId)
    {
        try
        {
            var resultado = new List<DetalheReceitaItemDto>();

            if (vendaId.HasValue)
            {
                // Linha de venda — mostra os itens controlados da venda + lotes baixados (se houver receita)
                var venda = await _db.Vendas
                    .Include(v => v.Itens).ThenInclude(i => i.Produto)
                    .Include(v => v.Receitas).ThenInclude(r => r.Itens).ThenInclude(i => i.ProdutoLote)
                    .FirstOrDefaultAsync(v => v.Id == vendaId.Value);
                if (venda == null) return resultado;

                var temReceita = venda.Receitas.Any();
                // Map produtoId → lote dispensado (primeiro encontrado) e quantidade acumulada
                var lotesPorProduto = new Dictionary<long, (string? num, DateTime? val, decimal qtde)>();
                foreach (var r in venda.Receitas)
                    foreach (var it in r.Itens)
                    {
                        var key = it.ProdutoId;
                        if (lotesPorProduto.TryGetValue(key, out var atual))
                            lotesPorProduto[key] = (atual.num ?? it.ProdutoLote.NumeroLote, atual.val ?? it.ProdutoLote.DataValidade, atual.qtde + it.Quantidade);
                        else
                            lotesPorProduto[key] = (it.ProdutoLote.NumeroLote, it.ProdutoLote.DataValidade, it.Quantidade);
                    }

                foreach (var item in venda.Itens.Where(i => i.Produto != null && ProdutoControleHelper.IsProdutoSngpc(i.Produto)))
                {
                    lotesPorProduto.TryGetValue(item.ProdutoId, out var dados);
                    resultado.Add(new DetalheReceitaItemDto
                    {
                        ProdutoNome = item.ProdutoNome,
                        ClasseTerapeutica = item.Produto!.ClasseTerapeutica,
                        Quantidade = item.Quantidade,
                        NumeroLote = dados.num,
                        DataValidade = dados.val,
                        Origem = temReceita ? "Na receita" : "Pendente"
                    });
                }
            }
            else if (receitaId.HasValue)
            {
                // Receita manual — pega os itens direto
                var receita = await _db.VendaReceitas
                    .Include(r => r.Itens).ThenInclude(i => i.Produto)
                    .Include(r => r.Itens).ThenInclude(i => i.ProdutoLote)
                    .FirstOrDefaultAsync(r => r.Id == receitaId.Value);
                if (receita == null) return resultado;

                resultado.AddRange(receita.Itens.Select(i => new DetalheReceitaItemDto
                {
                    ProdutoNome = i.Produto.Nome,
                    ClasseTerapeutica = i.Produto.ClasseTerapeutica,
                    Quantidade = i.Quantidade,
                    NumeroLote = i.ProdutoLote.NumeroLote,
                    DataValidade = i.ProdutoLote.DataValidade,
                    Origem = "Manual"
                }));
            }

            return resultado;
        }
        catch (Exception ex) { Log.Error(ex, "Erro em VendaReceitaService.ObterDetalhesAsync"); throw; }
    }

    public async Task<List<ItemControladoDto>> PesquisarProdutosSngpcAsync(string termo, long filialId)
    {
        try
        {
            var termoNorm = (termo ?? "").Trim().ToUpper();
            if (termoNorm.Length < 2) return new List<ItemControladoDto>();

            var produtos = await _db.Produtos
                .Where(p => p.Ativo
                    && (p.ClasseTerapeutica == ProdutoControleHelper.CLASSE_PSICOTROPICOS
                     || p.ClasseTerapeutica == ProdutoControleHelper.CLASSE_ANTIMICROBIANO)
                    && (p.Nome.ToUpper().Contains(termoNorm) || (p.Codigo != null && p.Codigo.Contains(termoNorm))))
                .OrderBy(p => p.Nome)
                .Take(30)
                .ToListAsync();

            var resultado = new List<ItemControladoDto>();
            foreach (var p in produtos)
            {
                var lotes = await _loteService.ListarLotesAtivosAsync(p.Id, filialId);
                resultado.Add(new ItemControladoDto
                {
                    VendaItemId = 0,
                    ProdutoId = p.Id,
                    ProdutoNome = p.Nome,
                    ClasseTerapeutica = p.ClasseTerapeutica,
                    Quantidade = 1,
                    LotesDisponiveis = lotes.Select(l => new LoteDisponivelDto
                    {
                        ProdutoLoteId = l.Id,
                        NumeroLote = l.NumeroLote,
                        DataValidade = l.DataValidade,
                        SaldoAtual = l.SaldoAtual
                    }).ToList()
                });
            }
            return resultado;
        }
        catch (Exception ex) { Log.Error(ex, "Erro em VendaReceitaService.PesquisarProdutosSngpcAsync"); throw; }
    }

    // ─── Helpers ──────────────────────────────────────────────────

    private async Task<long> ResolverPrescritorAsync(VendaReceitaFormDto rec)
    {
        if (rec.PrescritorId.HasValue && rec.PrescritorId.Value > 0)
        {
            var existe = await _db.Prescritores.AnyAsync(p => p.Id == rec.PrescritorId.Value);
            if (!existe) throw new ArgumentException($"Prescritor {rec.PrescritorId} não encontrado.");
            return rec.PrescritorId.Value;
        }

        if (rec.PrescritorNovo == null || string.IsNullOrWhiteSpace(rec.PrescritorNovo.Nome))
            throw new ArgumentException("Informe o prescritor da receita.");

        var novo = rec.PrescritorNovo;
        var nomeUp = novo.Nome.Trim().ToUpper();
        var conselhoUp = novo.TipoConselho.Trim().ToUpper();
        var numConselho = novo.NumeroConselho.Trim();
        var ufUp = novo.Uf.Trim().ToUpper();

        // Tenta encontrar um prescritor existente com mesmo conselho+numero+UF
        var existente = await _db.Prescritores.FirstOrDefaultAsync(p =>
            p.TipoConselho == conselhoUp && p.NumeroConselho == numConselho && p.Uf == ufUp);
        if (existente != null) return existente.Id;

        var entity = new Prescritor
        {
            Nome = nomeUp,
            TipoConselho = conselhoUp,
            NumeroConselho = numConselho,
            Uf = ufUp,
            Especialidade = string.IsNullOrWhiteSpace(novo.Especialidade) ? null : novo.Especialidade.Trim().ToUpper()
        };
        _db.Prescritores.Add(entity);
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync("Prescritores", "CRIAÇÃO", "Prescritor", entity.Id, novo: new Dictionary<string, string?>
        {
            ["Nome"] = entity.Nome,
            ["Tipo Conselho"] = entity.TipoConselho,
            ["Nº Conselho"] = entity.NumeroConselho,
            ["UF"] = entity.Uf,
            ["Origem"] = "Auto-cadastro via finalização de venda"
        });

        return entity.Id;
    }

    private static void ValidarReceita(VendaReceitaFormDto rec)
    {
        if (string.IsNullOrWhiteSpace(rec.PacienteNome))
            throw new ArgumentException("Nome do paciente é obrigatório.");
        if (rec.DataEmissao == default)
            throw new ArgumentException("Data de emissão da receita é obrigatória.");
        if (rec.DataValidade == default || rec.DataValidade < rec.DataEmissao)
            throw new ArgumentException("Validade da receita inválida.");
        if ((rec.Tipo == TipoReceitaSngpc.NotificacaoA
             || rec.Tipo == TipoReceitaSngpc.NotificacaoB1
             || rec.Tipo == TipoReceitaSngpc.NotificacaoB2)
            && string.IsNullOrWhiteSpace(rec.NumeroNotificacao))
            throw new ArgumentException("Número da notificação é obrigatório para receitas A/B1/B2.");
        if (rec.Itens == null || rec.Itens.Count == 0)
            throw new ArgumentException("A receita precisa cobrir ao menos 1 item.");
        if (!rec.CompradorMesmoPaciente && string.IsNullOrWhiteSpace(rec.CompradorNome))
            throw new ArgumentException("Informe os dados do comprador quando diferente do paciente.");
    }

    private static string? SoDigitosOuNull(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        var s = new string(v.Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(s) ? null : s;
    }
}
