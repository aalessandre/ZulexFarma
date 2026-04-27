using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Fiscal;
using ZulexPharma.Application.DTOs.SelfCheckout;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Entities.SelfCheckout;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services.SelfCheckout;

public class SelfCheckoutVendaService : ISelfCheckoutVendaService
{
    private readonly AppDbContext _db;
    private readonly IErpConnectorFactory _connectorFactory;
    private readonly IVendaFiscalService _vendaFiscal;

    public SelfCheckoutVendaService(AppDbContext db, IErpConnectorFactory connectorFactory, IVendaFiscalService vendaFiscal)
    {
        _db = db;
        _connectorFactory = connectorFactory;
        _vendaFiscal = vendaFiscal;
    }

    public async Task<IniciarVendaKioskResultDto> IniciarAsync(long filialId, IniciarVendaKioskDto input, CancellationToken ct = default)
    {
        if (input.Itens == null || input.Itens.Count == 0)
            throw new InvalidOperationException("Carrinho vazio.");

        var filial = await _db.Filiais.FindAsync(new object[] { filialId }, ct)
            ?? throw new InvalidOperationException($"Filial {filialId} não encontrada.");

        var terminal = await _db.SelfCheckoutTerminais
            .FirstOrDefaultAsync(t => t.Id == input.TerminalId && t.FilialId == filialId, ct)
            ?? throw new InvalidOperationException($"Terminal {input.TerminalId} não encontrado para a filial.");

        await using var connector = await _connectorFactory.CriarParaFilialAsync(filialId, ct)
            ?? throw new InvalidOperationException("Self-Checkout não configurado para esta filial.");

        // Regime tributário (1=Simples, 2=Excesso, 3=Lucro Presumido, 4=Lucro Real)
        var regimeStr = await _db.Configuracoes
            .Where(c => c.Chave == "fiscal.regime.tributario")
            .Select(c => c.Valor)
            .FirstOrDefaultAsync(ct);
        var regime = int.TryParse(regimeStr, out var r) && r >= 1 ? r : 1;

        // ── 1) Resolve produto + preço + snapshot fiscal de cada item ──
        var resolvidos = new List<(VendaKioskItemDto entrada, ProdutoSelfCheckoutDto produto, ProdutoFiscalSnapshotDto fiscal)>();
        foreach (var entrada in input.Itens)
        {
            if (entrada.Quantidade <= 0)
                throw new InvalidOperationException($"Quantidade inválida para o item {entrada.CodigoExterno}.");

            // O kiosk já bipou/buscou o produto e tem o CodigoExterno (CodigoProduto interno
            // do Inovafarma). Aqui revalidamos preço/estoque atuais via busca pelo código.
            var produto = await connector.BuscarProdutoPorCodigoAsync(entrada.CodigoExterno, ct);
            if (produto == null)
                throw new InvalidOperationException($"Produto {entrada.CodigoExterno} não localizado no ERP origem.");

            var fiscal = await connector.ObterFiscalAsync(produto.CodigoExterno, filial.Uf, regime, ct);
            if (fiscal == null)
                throw new InvalidOperationException(
                    $"Não foi possível resolver dados fiscais do produto {produto.Nome}. Verifique o cadastro fiscal no ERP origem.");

            // Validações específicas por regime
            if ((regime == 3 || regime == 4) && string.IsNullOrWhiteSpace(fiscal.CstIcms))
                throw new InvalidOperationException(
                    $"CST ICMS não resolvido para o produto {produto.Nome}. Configure a Natureza de Operação no accordion Self-Checkout.");
            if ((regime == 1 || regime == 2) && string.IsNullOrWhiteSpace(fiscal.Csosn))
                throw new InvalidOperationException(
                    $"CSOSN não resolvido para o produto {produto.Nome}. Verifique o cadastro do produto no ERP origem.");

            resolvidos.Add((entrada, produto, fiscal));
        }

        // ── 2) Cria a Venda + itens + snapshot fiscal numa transação ──
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var agora = DataHoraHelper.Agora();
        var venda = new Venda
        {
            FilialId = filialId,
            Origem = VendaOrigem.SelfCheckout,
            SelfCheckoutTerminalId = terminal.Id,
            TipoOperacao = TipoOperacao.Venda,
            ModeloDocumento = ModeloDocumento.Nfce,
            StatusFiscal = StatusFiscal.NaoEmitido,
            Status = VendaStatus.Aberta,
            DataPreVenda = agora,
            CriadoEm = agora,
            // Sem caixa aberto (RN-02). Pagamento pendente até atendente confirmar.
            PagamentoRecebido = false,
        };
        _db.Vendas.Add(venda);
        await _db.SaveChangesAsync(ct);

        decimal totalBruto = 0;
        decimal totalDesconto = 0;
        var resultadoItens = new List<VendaKioskItemResultDto>(resolvidos.Count);
        int ordem = 1;
        int numeroItem = 1;

        foreach (var (entrada, produto, fiscal) in resolvidos)
        {
            var precoUn = produto.PrecoFinal;
            var qtd = entrada.Quantidade;
            var bruto = Math.Round(produto.PrecoCheio * qtd, 2);
            var liquido = Math.Round(precoUn * qtd, 2);
            var desconto = Math.Round(bruto - liquido, 2);

            var vi = new VendaItem
            {
                VendaId = venda.Id,
                ProdutoId = null, // RN-22: produto totalmente externo
                ProdutoCodigo = produto.CodigoExterno,
                ProdutoNome = produto.Nome,
                Fabricante = null,
                PrecoVenda = produto.PrecoCheio,
                Quantidade = qtd,
                PercentualPromocao = produto.EmPromocao && produto.PrecoCheio > 0
                    ? Math.Round((1 - precoUn / produto.PrecoCheio) * 100, 4)
                    : 0,
                ValorDesconto = desconto,
                PrecoUnitario = precoUn,
                Total = liquido,
                Ordem = ordem++,
            };
            _db.VendaItens.Add(vi);
            await _db.SaveChangesAsync(ct);

            // Snapshot fiscal do item (consumido pelo MontarXmlNfce via item.Fiscal)
            var vif = new VendaItemFiscal
            {
                VendaItemId = vi.Id,
                NumeroItem = numeroItem++,
                CodigoProduto = produto.CodigoExterno,
                CodigoBarras = string.IsNullOrWhiteSpace(produto.CodigoBarras) ? "SEM GTIN" : produto.CodigoBarras!,
                DescricaoProduto = produto.Nome,
                Ncm = fiscal.Ncm,
                Cest = fiscal.Cest,
                Cfop = fiscal.Cfop,
                Unidade = fiscal.Unidade,
                IndicadorTotal = 1,

                OrigemMercadoria = fiscal.OrigemMercadoria,
                CstIcms = fiscal.CstIcms,
                Csosn = fiscal.Csosn,
                BaseIcms = (regime == 3 || regime == 4) ? liquido : 0,
                AliquotaIcms = fiscal.AliquotaIcms,
                ValorIcms = (regime == 3 || regime == 4)
                    ? Math.Round(liquido * fiscal.AliquotaIcms / 100, 2)
                    : 0,
                PercentualReducaoBc = fiscal.PercentualReducaoBc,
                CodigoBeneficioFiscal = fiscal.CodigoBeneficioFiscal,

                AliquotaFcp = fiscal.AliquotaFcp,
                BaseFcp = fiscal.AliquotaFcp > 0 && (regime == 3 || regime == 4) ? liquido : 0,
                ValorFcp = fiscal.AliquotaFcp > 0 && (regime == 3 || regime == 4)
                    ? Math.Round(liquido * fiscal.AliquotaFcp / 100, 2) : 0,

                CstPis = fiscal.CstPis,
                BasePis = fiscal.AliquotaPis > 0 ? liquido : 0,
                AliquotaPis = fiscal.AliquotaPis,
                ValorPis = Math.Round(liquido * fiscal.AliquotaPis / 100, 2),

                CstCofins = fiscal.CstCofins,
                BaseCofins = fiscal.AliquotaCofins > 0 ? liquido : 0,
                AliquotaCofins = fiscal.AliquotaCofins,
                ValorCofins = Math.Round(liquido * fiscal.AliquotaCofins / 100, 2),

                ValorTotalTributos = 0 // IBPTax populado pelo VendaFiscalService no fluxo de emissão
            };
            _db.VendaItemFiscais.Add(vif);

            // Fila de conciliação de estoque (RN-22): processada manualmente/automaticamente depois
            _db.SelfCheckoutConciliacoesEstoque.Add(new SelfCheckoutConciliacaoEstoque
            {
                VendaItemId = vi.Id,
                CodigoProdutoExterno = produto.CodigoExterno,
                CodigoBarrasExterno = produto.CodigoBarras,
                Quantidade = qtd,
            });

            totalBruto += bruto;
            totalDesconto += desconto;

            resultadoItens.Add(new VendaKioskItemResultDto
            {
                VendaItemId = vi.Id,
                CodigoExterno = produto.CodigoExterno,
                Nome = produto.Nome,
                PrecoUnitario = precoUn,
                Quantidade = qtd,
                Total = liquido,
                EmPromocao = produto.EmPromocao
            });
        }

        venda.TotalBruto = totalBruto;
        venda.TotalDesconto = totalDesconto;
        venda.TotalLiquido = totalBruto - totalDesconto;
        venda.TotalItens = resultadoItens.Count;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Atualiza última atividade do terminal (sem await na transação)
        terminal.UltimaAtividade = agora;
        await _db.SaveChangesAsync(ct);

        return new IniciarVendaKioskResultDto
        {
            VendaId = venda.Id,
            TotalLiquido = venda.TotalLiquido,
            TotalItens = venda.TotalItens,
            Itens = resultadoItens
        };
    }

    public async Task RegistrarPagamentoAsync(long vendaId, RegistrarPagamentoKioskDto input, CancellationToken ct = default)
    {
        var venda = await _db.Vendas
            .Include(v => v.Pagamentos)
            .FirstOrDefaultAsync(v => v.Id == vendaId && v.Origem == VendaOrigem.SelfCheckout, ct)
            ?? throw new InvalidOperationException($"Venda kiosk {vendaId} não encontrada.");

        if (venda.Status != VendaStatus.Aberta)
            throw new InvalidOperationException("Venda já foi finalizada ou cancelada.");

        if (venda.PagamentoRecebido)
            throw new InvalidOperationException("Pagamento já foi confirmado.");

        var modalidade = input.FormaPagamento switch
        {
            FormaPagamentoKiosk.Pix => ModalidadePagamento.VendaPix,
            FormaPagamentoKiosk.Cartao => ModalidadePagamento.VendaCartao,
            _ => throw new InvalidOperationException("Forma de pagamento não suportada.")
        };

        var tipo = await _db.TiposPagamento
            .Where(t => t.Modalidade == modalidade && t.Ativo)
            .OrderBy(t => t.Ordem).ThenBy(t => t.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException(
                $"Nenhum Tipo de Pagamento ativo com modalidade {modalidade}. Cadastre em Tipos de Pagamento.");

        // Remove pagamentos anteriores caso o cliente troque a forma antes de confirmar
        if (venda.Pagamentos.Count > 0)
        {
            _db.VendaPagamentos.RemoveRange(venda.Pagamentos);
        }

        _db.VendaPagamentos.Add(new VendaPagamento
        {
            VendaId = venda.Id,
            TipoPagamentoId = tipo.Id,
            Valor = venda.TotalLiquido,
            Troco = 0
        });

        venda.TipoPagamentoId = tipo.Id;
        venda.AtualizadoEm = DataHoraHelper.Agora();

        await _db.SaveChangesAsync(ct);
    }

    public async Task<ConfirmarVendaKioskResultDto> ConfirmarPagamentoAsync(long vendaId, CancellationToken ct = default)
    {
        var venda = await _db.Vendas
            .Include(v => v.Pagamentos)
            .Include(v => v.Itens)
            .FirstOrDefaultAsync(v => v.Id == vendaId && v.Origem == VendaOrigem.SelfCheckout, ct)
            ?? throw new InvalidOperationException($"Venda kiosk {vendaId} não encontrada.");

        if (venda.Status == VendaStatus.Cancelada)
            throw new InvalidOperationException("Venda foi cancelada.");
        if (venda.Status == VendaStatus.Finalizada)
            throw new InvalidOperationException("Venda já está finalizada.");
        if (venda.Pagamentos.Count == 0)
            throw new InvalidOperationException("Pagamento ainda não foi informado pelo cliente.");

        // NÃO marcamos PagamentoRecebido/DataFinalizacao antes da NFC-e — assim,
        // se a emissão falhar, a venda continua aparecendo na lista de pendentes
        // (Status=Aberta + tem pagamento + Fiscal=null) e o atendente pode reagir.

        VendaFiscalEmissaoResult resultado;
        try
        {
            resultado = await _vendaFiscal.EmitirNfceAsync(venda.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Self-Checkout: exceção ao emitir NFC-e VendaId={VendaId}", vendaId);
            throw new InvalidOperationException(
                $"Falha ao emitir NFC-e: {ex.Message}", ex);
        }

        var vendaPersistida = await _db.Vendas
            .Include(v => v.Fiscal)
            .FirstOrDefaultAsync(v => v.Id == vendaId, ct);

        if (resultado.Autorizada)
        {
            var agora = DataHoraHelper.Agora();
            vendaPersistida!.Status = VendaStatus.Finalizada;
            vendaPersistida.PagamentoRecebido = true;
            vendaPersistida.DataPagamentoRecebido = agora;
            vendaPersistida.DataFinalizacao = agora;
            vendaPersistida.AtualizadoEm = agora;
            await _db.SaveChangesAsync(ct);
            return new ConfirmarVendaKioskResultDto
            {
                VendaId = vendaId,
                NfceAutorizada = true,
                ChaveAcesso = vendaPersistida.Fiscal?.ChaveAcesso,
                NumeroNfce = vendaPersistida.Fiscal?.Numero,
                SerieNfce = vendaPersistida.Fiscal?.Serie,
                Mensagem = resultado.MotivoStatus
            };
        }

        Log.Warning("Self-Checkout: NFC-e rejeitada VendaId={VendaId} cStat={CodStatus} xMotivo={Motivo}",
            vendaId, resultado.CodigoStatus, resultado.MotivoStatus);

        // Venda continua Aberta + sem PagamentoRecebido — volta pra lista de pendentes
        // (a query de pendentes também aceita "tem Fiscal mas não autorizado").
        return new ConfirmarVendaKioskResultDto
        {
            VendaId = vendaId,
            NfceAutorizada = false,
            ChaveAcesso = vendaPersistida?.Fiscal?.ChaveAcesso,
            NumeroNfce = vendaPersistida?.Fiscal?.Numero,
            SerieNfce = vendaPersistida?.Fiscal?.Serie,
            Mensagem = resultado.MotivoStatus ?? "Falha ao emitir NFC-e."
        };
    }

    public async Task<StatusVendaKioskDto?> ObterStatusKioskAsync(long vendaId, CancellationToken ct = default)
    {
        var v = await _db.Vendas
            .Include(x => x.Pagamentos)
            .Include(x => x.Fiscal)
            .FirstOrDefaultAsync(x => x.Id == vendaId && x.Origem == VendaOrigem.SelfCheckout, ct);

        if (v == null) return null;

        var dto = new StatusVendaKioskDto { VendaId = v.Id };

        if (v.Status == VendaStatus.Cancelada)
        {
            dto.Status = StatusVendaKiosk.Cancelada;
            dto.Mensagem = v.Observacao;
            return dto;
        }

        if (v.Status == VendaStatus.Finalizada)
        {
            if (v.Fiscal?.CodigoStatus == 100 || v.StatusFiscal == StatusFiscal.Autorizado)
            {
                dto.Status = StatusVendaKiosk.NfceAutorizada;
                dto.ChaveAcesso = v.Fiscal?.ChaveAcesso;
                dto.NumeroNfce = v.Fiscal?.Numero;
                dto.SerieNfce = v.Fiscal?.Serie;
            }
            else
            {
                dto.Status = StatusVendaKiosk.Erro;
                dto.Mensagem = v.Fiscal?.MotivoStatus;
            }
            return dto;
        }

        // Aberta
        dto.Status = v.Pagamentos.Count > 0
            ? StatusVendaKiosk.AguardandoAtendente
            : StatusVendaKiosk.AguardandoFormaPagamento;
        return dto;
    }

    public async Task<List<PagamentoPendenteDto>> ListarPagamentosPendentesAsync(long filialId, CancellationToken ct = default)
    {
        // Inclui:
        // - vendas Aberta com pagamento informado e ainda sem confirmação;
        // - vendas que tentaram emitir e a NFC-e foi rejeitada (Fiscal existe mas
        //   StatusFiscal != Autorizado) — atendente precisa reagir.
        return await _db.Vendas
            .AsNoTracking()
            .Where(v => v.FilialId == filialId
                     && v.Origem == VendaOrigem.SelfCheckout
                     && v.Status == VendaStatus.Aberta
                     && v.Pagamentos.Any()
                     && v.StatusFiscal != StatusFiscal.Autorizado)
            .OrderBy(v => v.CriadoEm)
            .Select(v => new PagamentoPendenteDto
            {
                VendaId = v.Id,
                TerminalId = v.SelfCheckoutTerminalId ?? 0,
                TerminalNumero = v.SelfCheckoutTerminal != null ? v.SelfCheckoutTerminal.Numero : 0,
                TerminalApelido = v.SelfCheckoutTerminal != null ? v.SelfCheckoutTerminal.Apelido : null,
                TotalLiquido = v.TotalLiquido,
                TotalItens = v.TotalItens,
                FormaPagamento = v.Pagamentos.Select(p => p.TipoPagamento.Nome).FirstOrDefault() ?? "—",
                CriadoEm = v.CriadoEm
            })
            .ToListAsync(ct);
    }

    public async Task CancelarAsync(long vendaId, string? motivo, CancellationToken ct = default)
    {
        var venda = await _db.Vendas
            .FirstOrDefaultAsync(v => v.Id == vendaId && v.Origem == VendaOrigem.SelfCheckout, ct)
            ?? throw new InvalidOperationException($"Venda kiosk {vendaId} não encontrada.");

        if (venda.Status == VendaStatus.Finalizada)
            throw new InvalidOperationException("Venda já finalizada não pode ser cancelada por aqui — use o módulo Fiscal.");
        if (venda.Status == VendaStatus.Cancelada) return;

        venda.Status = VendaStatus.Cancelada;
        venda.Observacao = string.IsNullOrWhiteSpace(motivo) ? "Cancelada no Self-Checkout" : motivo;
        venda.AtualizadoEm = DataHoraHelper.Agora();
        await _db.SaveChangesAsync(ct);
    }
}
