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

    public async Task<List<VendaListDto>> ListarAsync(long? filialId = null, string? status = null, long? caixaId = null)
    {
        try
        {
            var query = _db.Set<Venda>()
                .Include(v => v.Cliente).ThenInclude(c => c!.Pessoa)
                .Include(v => v.Colaborador).ThenInclude(c => c!.Pessoa)
                .Include(v => v.TipoPagamento)
                .AsQueryable();

            if (filialId.HasValue) query = query.Where(v => v.FilialId == filialId);
            if (caixaId.HasValue) query = query.Where(v => v.CaixaId == caixaId);
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
                    ConvenioNome = v.ConvenioId != null ? _db.Set<Convenio>().Where(c => c.Id == v.ConvenioId).Select(c => c.Pessoa.Nome).FirstOrDefault() : null,
                    TotalBruto = v.TotalBruto, TotalDesconto = v.TotalDesconto,
                    TotalLiquido = v.TotalLiquido, TotalItens = v.TotalItens,
                    Status = v.Status, StatusDescricao = StatusTexto(v.Status),
                    CriadoEm = v.CriadoEm,
                    DataPreVenda = v.DataPreVenda, DataFinalizacao = v.DataFinalizacao, DataEmissaoCupom = v.DataEmissaoCupom
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
                DataPreVenda = v.DataPreVenda, DataFinalizacao = v.DataFinalizacao, DataEmissaoCupom = v.DataEmissaoCupom,
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

    public async Task<VendaPrazoValidacaoDto> ValidarVendaPrazoAsync(ValidarPrazoRequestDto request)
    {
        var result = new VendaPrazoValidacaoDto();
        var cliente = await _db.Set<Cliente>()
            .Include(c => c.Bloqueios)
            .FirstOrDefaultAsync(c => c.Id == request.ClienteId);
        if (cliente == null) throw new KeyNotFoundException("Cliente não encontrado.");

        Convenio? convenio = null;
        if (request.ConvenioId.HasValue)
            convenio = await _db.Set<Convenio>()
                .Include(c => c.Bloqueios)
                .FirstOrDefaultAsync(c => c.Id == request.ConvenioId);

        // 1. Bloqueado
        if (cliente.Bloqueado) { result.ClienteBloqueado = true; result.MensagemBloqueio = "Cliente está bloqueado."; }
        if (convenio?.Bloqueado == true) { result.ConvenioBloqueado = true; result.MensagemBloqueio = "Convênio está bloqueado."; }

        // 2. Tipo pagamento bloqueado
        var tpBloqueadoCliente = cliente.Bloqueios.Any(b => b.TipoPagamentoId == request.TipoPagamentoId);
        var tpBloqueadoConvenio = convenio?.Bloqueios.Any(b => b.TipoPagamentoId == request.TipoPagamentoId) ?? false;
        if (tpBloqueadoCliente || tpBloqueadoConvenio)
        {
            result.TipoPagamentoBloqueado = true;
            result.MensagemTipoBloqueado = tpBloqueadoCliente
                ? "Condição de pagamento bloqueada para este cliente."
                : "Condição de pagamento bloqueada para este convênio.";
        }

        // 3. Limite de crédito
        var limite = cliente.LimiteCredito > 0 ? cliente.LimiteCredito : convenio?.LimiteCredito ?? 0;
        result.LimiteCredito = limite;
        if (limite > 0)
        {
            var saldoUtilizado = await _db.ContasReceber
                .Include(cr => cr.TipoPagamento)
                .Where(cr => cr.ClienteId == request.ClienteId
                    && cr.Status == StatusContaReceber.Aberta
                    && cr.TipoPagamento != null && cr.TipoPagamento.Modalidade == ModalidadePagamento.VendaPrazo)
                .SumAsync(cr => cr.ValorLiquido);
            result.SaldoUtilizado = saldoUtilizado;
            result.SaldoDisponivel = limite - saldoUtilizado;
            result.ExcedeLimite = (saldoUtilizado + request.ValorVenda) > limite;
        }

        // 4. Parcelamento
        var permiteParcelada = cliente.PermiteVendaParcelada && !(convenio?.BloquearVendaParcelada ?? false);
        var maxParcelas = permiteParcelada
            ? Math.Min(cliente.QtdeMaxParcelas, convenio?.MaximoParcelas ?? int.MaxValue)
            : 1;
        result.PermiteParcelada = permiteParcelada;
        result.MaxParcelas = Math.Max(1, maxParcelas);

        // 5. Bloquear desconto
        result.BloquearDescontoParcelada = cliente.BloquearDescontoParcelada || (convenio?.BloquearDescontoParcelada ?? false);

        // 6. Exige senha
        result.ExigeSenha = cliente.VenderSomenteComSenha || (convenio?.VenderSomenteComSenha ?? false);

        return result;
    }

    public async Task FinalizarAsync(long id, FinalizarVendaDto? opcoes = null)
    {
        try
        {
            var venda = await _db.Set<Venda>()
                .Include(v => v.Pagamentos).ThenInclude(p => p.TipoPagamento)
                .Include(v => v.Cliente)
                .FirstOrDefaultAsync(v => v.Id == id)
                ?? throw new KeyNotFoundException($"Venda {id} não encontrada.");
            if (venda.Status != VendaStatus.Aberta) throw new ArgumentException("Venda não está aberta.");

            var agora = Domain.Helpers.DataHoraHelper.Agora();
            var temPrazo = venda.Pagamentos.Any(p => p.TipoPagamento?.Modalidade == ModalidadePagamento.VendaPrazo);

            // ── Validações de venda a prazo ────────────────────────
            Cliente? cliente = null;
            Convenio? convenio = null;

            if (temPrazo)
            {
                if (!venda.ClienteId.HasValue)
                    throw new ArgumentException("Cliente obrigatório para venda a prazo.");

                cliente = await _db.Set<Cliente>()
                    .Include(c => c.Bloqueios)
                    .FirstOrDefaultAsync(c => c.Id == venda.ClienteId);
                if (cliente == null) throw new ArgumentException("Cliente não encontrado.");

                if (venda.ConvenioId.HasValue)
                    convenio = await _db.Set<Convenio>()
                        .Include(c => c.Bloqueios)
                        .FirstOrDefaultAsync(c => c.Id == venda.ConvenioId);

                // Bloqueado
                if (cliente.Bloqueado) throw new ArgumentException("Cliente está bloqueado.");
                if (convenio?.Bloqueado == true) throw new ArgumentException("Convênio está bloqueado.");

                // Tipo pagamento bloqueado
                foreach (var pag in venda.Pagamentos.Where(p => p.TipoPagamento?.Modalidade == ModalidadePagamento.VendaPrazo))
                {
                    if (cliente.Bloqueios.Any(b => b.TipoPagamentoId == pag.TipoPagamentoId))
                        throw new ArgumentException("Condição de pagamento bloqueada para este cliente.");
                    if (convenio?.Bloqueios.Any(b => b.TipoPagamentoId == pag.TipoPagamentoId) == true)
                        throw new ArgumentException("Condição de pagamento bloqueada para este convênio.");
                }

                // Limite de crédito
                var limite = cliente.LimiteCredito > 0 ? cliente.LimiteCredito : convenio?.LimiteCredito ?? 0;
                if (limite > 0)
                {
                    var valorPrazo = venda.Pagamentos
                        .Where(p => p.TipoPagamento?.Modalidade == ModalidadePagamento.VendaPrazo)
                        .Sum(p => p.Valor - p.Troco);
                    var saldoUtilizado = await _db.ContasReceber
                        .Include(cr => cr.TipoPagamento)
                        .Where(cr => cr.ClienteId == venda.ClienteId
                            && cr.Status == StatusContaReceber.Aberta
                            && cr.TipoPagamento != null && cr.TipoPagamento.Modalidade == ModalidadePagamento.VendaPrazo)
                        .SumAsync(cr => cr.ValorLiquido);
                    if ((saldoUtilizado + valorPrazo) > limite)
                    {
                        // Verificar token de liberação
                        if (string.IsNullOrWhiteSpace(opcoes?.TokenLiberacaoCredito))
                            throw new ArgumentException($"Limite de crédito excedido. Limite: {limite:N2}, Utilizado: {saldoUtilizado:N2}. Requer liberação.");
                        // Token válido (já validado no frontend via auth/liberar)
                    }
                }

                // Senha do cliente
                var exigeSenha = cliente.VenderSomenteComSenha || (convenio?.VenderSomenteComSenha ?? false);
                if (exigeSenha)
                {
                    var senhaEsperada = !string.IsNullOrEmpty(cliente.SenhaVendaPrazo) ? cliente.SenhaVendaPrazo : convenio?.SenhaVenda;
                    if (string.IsNullOrWhiteSpace(opcoes?.SenhaCliente) || opcoes.SenhaCliente != senhaEsperada)
                        throw new ArgumentException("Senha do cliente inválida.");
                }
            }

            // ── Finalizar e gerar contas a receber ─────────────────
            venda.Status = VendaStatus.Finalizada;
            venda.DataFinalizacao = agora;

            foreach (var pag in venda.Pagamentos)
            {
                var modalidade = pag.TipoPagamento?.Modalidade;
                var planoContaId = pag.TipoPagamento?.PlanoContaId;
                var valorLiquido = pag.Valor - pag.Troco;
                var jaRecebido = modalidade == ModalidadePagamento.VendaVista
                              || modalidade == ModalidadePagamento.VendaPix;

                if (modalidade == ModalidadePagamento.VendaPrazo && cliente != null)
                {
                    // Gerar parcelas com vencimento calculado
                    var numParcelas = Math.Max(1, opcoes?.NumeroParcelas ?? 1);

                    // Hierarquia: usar modo do cliente; se DiasCorridos sem QtdeDias configurado, cai no convênio
                    var modo = cliente.PrazoPagamento;
                    int? qtdeDias = cliente.QtdeDias;
                    int? diaFech = cliente.DiaFechamento;
                    int? diaVenc = cliente.DiaVencimento;
                    int? qtdeMeses = cliente.QtdeMeses;

                    // Se o cliente não tem parâmetros configurados, usar do convênio
                    if (convenio != null)
                    {
                        if (modo == ModoFechamento.DiasCorridos && qtdeDias == null && convenio.ModoFechamento == ModoFechamento.PorFechamento)
                            modo = convenio.ModoFechamento;
                        qtdeDias ??= convenio.DiasCorridos;
                        diaFech ??= convenio.DiaFechamento;
                        diaVenc ??= convenio.DiaVencimento;
                        qtdeMeses ??= convenio.MesesParaVencimento;
                    }

                    var datas = Domain.Helpers.VencimentoHelper.CalcularVencimentoParcelas(
                        modo, agora, qtdeDias, diaFech, diaVenc, qtdeMeses, numParcelas);

                    var valorParcela = Math.Round(valorLiquido / numParcelas, 2);
                    var valorResto = valorLiquido - (valorParcela * (numParcelas - 1)); // última parcela ajusta centavos

                    for (int i = 0; i < numParcelas; i++)
                    {
                        var vlr = (i == numParcelas - 1) ? valorResto : valorParcela;
                        _db.ContasReceber.Add(new ContaReceber
                        {
                            FilialId = venda.FilialId, VendaId = venda.Id, VendaPagamentoId = pag.Id,
                            ClienteId = venda.ClienteId, TipoPagamentoId = pag.TipoPagamentoId,
                            PlanoContaId = planoContaId,
                            Descricao = numParcelas > 1
                                ? $"Venda #{venda.Codigo ?? venda.Id.ToString()} - {pag.TipoPagamento?.Nome ?? ""} ({i + 1}/{numParcelas})"
                                : $"Venda #{venda.Codigo ?? venda.Id.ToString()} - {pag.TipoPagamento?.Nome ?? ""}",
                            Valor = vlr, ValorLiquido = vlr,
                            DataEmissao = agora, DataVencimento = datas[i],
                            NumParcela = i + 1, TotalParcelas = numParcelas,
                            Status = StatusContaReceber.Aberta
                        });
                    }
                }
                else
                {
                    _db.ContasReceber.Add(new ContaReceber
                    {
                        FilialId = venda.FilialId, VendaId = venda.Id, VendaPagamentoId = pag.Id,
                        ClienteId = venda.ClienteId, TipoPagamentoId = pag.TipoPagamentoId,
                        PlanoContaId = planoContaId,
                        Descricao = $"Venda #{venda.Codigo ?? venda.Id.ToString()} - {pag.TipoPagamento?.Nome ?? ""}",
                        Valor = pag.Valor, ValorLiquido = valorLiquido > 0 ? valorLiquido : pag.Valor,
                        DataEmissao = agora, DataVencimento = agora.Date,
                        NumParcela = 1, TotalParcelas = 1,
                        Status = jaRecebido ? StatusContaReceber.Recebida : StatusContaReceber.Aberta,
                        DataRecebimento = jaRecebido ? agora : null,
                        ValorRecebido = jaRecebido ? valorLiquido : 0
                    });
                }
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
