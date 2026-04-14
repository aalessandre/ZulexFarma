using System.Text;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Caixa;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class CaixaMovimentoService : ICaixaMovimentoService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Caixa";
    private const string ENTIDADE = "CaixaMovimento";

    public CaixaMovimentoService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    // ═══ Listagens ═══════════════════════════════════════════════════

    public async Task<List<CaixaMovimentoListDto>> ListarPorCaixaAsync(long caixaId)
    {
        try
        {
            var list = await _db.CaixaMovimentos
                .Include(m => m.TipoPagamento)
                .Include(m => m.VendaPagamento)
                .Include(m => m.Usuario).ThenInclude(u => u!.Colaborador).ThenInclude(c => c!.Pessoa)
                .Where(m => m.CaixaId == caixaId)
                .OrderBy(m => m.DataMovimento)
                .ToListAsync();
            return list.Select(Mapear).ToList();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em CaixaMovimentoService.ListarPorCaixaAsync"); throw; }
    }

    public async Task<List<CaixaMovimentoListDto>> ListarPorVendaAsync(long vendaId)
    {
        try
        {
            var list = await _db.CaixaMovimentos
                .Include(m => m.TipoPagamento)
                .Include(m => m.VendaPagamento)
                .Where(m => m.VendaPagamento != null && m.VendaPagamento.VendaId == vendaId)
                .OrderBy(m => m.DataMovimento)
                .ToListAsync();
            return list.Select(Mapear).ToList();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em CaixaMovimentoService.ListarPorVendaAsync"); throw; }
    }

    public async Task<List<CaixaMovimentoListDto>> ListarSangriasPendentesAsync(long filialId)
    {
        try
        {
            return await _db.CaixaMovimentos
                .Include(m => m.Caixa).ThenInclude(c => c.Colaborador).ThenInclude(co => co.Pessoa)
                .Include(m => m.Usuario).ThenInclude(u => u!.Colaborador).ThenInclude(c => c!.Pessoa)
                .Where(m => m.Tipo == TipoMovimentoCaixa.Sangria
                         && m.StatusConferencia == StatusConferenciaMovimento.PendenteConferente
                         && m.Caixa.FilialId == filialId)
                .OrderBy(m => m.DataMovimento)
                .Select(m => Mapear(m))
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em CaixaMovimentoService.ListarSangriasPendentesAsync"); throw; }
    }

    // ═══ Operações do caixa ══════════════════════════════════════════

    public async Task<long> CriarSangriaAsync(SangriaFormDto dto, long usuarioId)
    {
        try
        {
            var caixa = await _db.Caixas.FirstOrDefaultAsync(c => c.Id == dto.CaixaId)
                ?? throw new ArgumentException("Caixa não encontrado.");
            if (caixa.Status != CaixaStatus.Aberto) throw new ArgumentException("Caixa não está aberto.");
            if (dto.Valor <= 0) throw new ArgumentException("Valor da sangria deve ser maior que zero.");

            var tipoDinheiro = await ObterTipoPagamentoDinheiroAsync();
            var mov = new CaixaMovimento
            {
                CaixaId = dto.CaixaId,
                Tipo = TipoMovimentoCaixa.Sangria,
                DataMovimento = DataHoraHelper.Agora(),
                Valor = dto.Valor,
                TipoPagamentoId = tipoDinheiro?.Id,
                Descricao = $"Sangria de R$ {dto.Valor:N2}",
                Observacao = dto.Observacao,
                UsuarioId = usuarioId,
                StatusConferencia = StatusConferenciaMovimento.PendenteConferente
            };
            _db.CaixaMovimentos.Add(mov);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "SANGRIA", ENTIDADE, mov.Id, novo: new() { ["Valor"] = dto.Valor.ToString("N2"), ["CaixaId"] = dto.CaixaId.ToString() });
            return mov.Id;
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em CriarSangriaAsync"); throw; }
    }

    public async Task<long> CriarSuprimentoAsync(SuprimentoFormDto dto, long usuarioId)
    {
        try
        {
            var caixa = await _db.Caixas.Include(c => c.Filial).FirstOrDefaultAsync(c => c.Id == dto.CaixaId)
                ?? throw new ArgumentException("Caixa não encontrado.");
            if (caixa.Status != CaixaStatus.Aberto) throw new ArgumentException("Caixa não está aberto.");
            if (dto.Valor <= 0) throw new ArgumentException("Valor do suprimento deve ser maior que zero.");
            if (caixa.Filial?.ContaCofreId == null) throw new ArgumentException("Conta Cofre não configurada para esta filial.");

            var agora = DataHoraHelper.Agora();
            var tipoDinheiro = await ObterTipoPagamentoDinheiroAsync();

            var mov = new CaixaMovimento
            {
                CaixaId = dto.CaixaId,
                Tipo = TipoMovimentoCaixa.Suprimento,
                DataMovimento = agora,
                Valor = dto.Valor,
                TipoPagamentoId = tipoDinheiro?.Id,
                Descricao = $"Suprimento de R$ {dto.Valor:N2}",
                Observacao = dto.Observacao,
                UsuarioId = usuarioId,
                StatusConferencia = StatusConferenciaMovimento.Conferido,
                DataConferencia = agora,
                ConferidoPorUsuarioId = usuarioId
            };
            _db.CaixaMovimentos.Add(mov);
            await _db.SaveChangesAsync();

            // Saída imediata no cofre
            _db.MovimentosContaBancaria.Add(new MovimentoContaBancaria
            {
                ContaBancariaId = caixa.Filial.ContaCofreId.Value,
                DataMovimento = agora,
                Tipo = TipoMovimentoBancario.Saida,
                Valor = dto.Valor,
                Descricao = $"Suprimento caixa #{caixa.Codigo ?? caixa.Id.ToString()}",
                CaixaMovimentoId = mov.Id,
                CaixaId = dto.CaixaId,
                UsuarioId = usuarioId
            });
            await _db.SaveChangesAsync();

            await _log.RegistrarAsync(TELA, "SUPRIMENTO", ENTIDADE, mov.Id, novo: new() { ["Valor"] = dto.Valor.ToString("N2"), ["CaixaId"] = dto.CaixaId.ToString() });
            return mov.Id;
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em CriarSuprimentoAsync"); throw; }
    }

    public async Task<long> CriarRecebimentoAsync(RecebimentoFormDto dto, long usuarioId)
    {
        try
        {
            var caixa = await _db.Caixas.FirstOrDefaultAsync(c => c.Id == dto.CaixaId)
                ?? throw new ArgumentException("Caixa não encontrado.");
            if (caixa.Status != CaixaStatus.Aberto) throw new ArgumentException("Caixa não está aberto.");
            if (dto.Valor <= 0) throw new ArgumentException("Valor do recebimento deve ser maior que zero.");

            var cr = await _db.ContasReceber.FirstOrDefaultAsync(x => x.Id == dto.ContaReceberId)
                ?? throw new ArgumentException("Conta a receber não encontrada.");
            if (cr.Status != StatusContaReceber.Aberta) throw new ArgumentException("Conta a receber não está aberta.");

            var valorRestante = cr.ValorLiquido - cr.ValorRecebido;
            if (dto.Valor > valorRestante + 0.01m)
                throw new ArgumentException($"Valor excede o valor restante da conta (R$ {valorRestante:N2}).");

            var tipo = await _db.Set<TipoPagamento>().FirstOrDefaultAsync(t => t.Id == dto.TipoPagamentoId)
                ?? throw new ArgumentException("Tipo de pagamento inválido.");

            var agora = DataHoraHelper.Agora();
            cr.ValorRecebido += dto.Valor;
            if (cr.ValorRecebido >= cr.ValorLiquido - 0.01m)
            {
                cr.Status = StatusContaReceber.Recebida;
                cr.DataRecebimento = agora;
            }

            var mov = new CaixaMovimento
            {
                CaixaId = dto.CaixaId,
                Tipo = TipoMovimentoCaixa.Recebimento,
                DataMovimento = agora,
                Valor = dto.Valor,
                TipoPagamentoId = dto.TipoPagamentoId,
                Descricao = $"Recebimento CR#{cr.Codigo ?? cr.Id.ToString()} ({tipo.Nome})",
                Observacao = dto.Observacao,
                UsuarioId = usuarioId,
                ContaReceberId = dto.ContaReceberId,
                StatusConferencia = StatusConferenciaMovimento.Conferido,
                DataConferencia = agora,
                ConferidoPorUsuarioId = usuarioId
            };
            _db.CaixaMovimentos.Add(mov);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "RECEBIMENTO", ENTIDADE, mov.Id, novo: new() { ["Valor"] = dto.Valor.ToString("N2"), ["ContaReceberId"] = dto.ContaReceberId.ToString() });
            return mov.Id;
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em CriarRecebimentoAsync"); throw; }
    }

    public async Task<long> CriarPagamentoAsync(PagamentoFormDto dto, long usuarioId)
    {
        try
        {
            var caixa = await _db.Caixas.FirstOrDefaultAsync(c => c.Id == dto.CaixaId)
                ?? throw new ArgumentException("Caixa não encontrado.");
            if (caixa.Status != CaixaStatus.Aberto) throw new ArgumentException("Caixa não está aberto.");
            if (dto.Valor <= 0) throw new ArgumentException("Valor do pagamento deve ser maior que zero.");

            var pessoa = await _db.Set<Pessoa>().FirstOrDefaultAsync(p => p.Id == dto.PessoaId)
                ?? throw new ArgumentException("Fornecedor/pessoa não encontrado.");
            var plano = await _db.Set<PlanoConta>().FirstOrDefaultAsync(p => p.Id == dto.PlanoContaId)
                ?? throw new ArgumentException("Plano de contas inválido.");
            var tipo = await _db.Set<TipoPagamento>().FirstOrDefaultAsync(t => t.Id == dto.TipoPagamentoId)
                ?? throw new ArgumentException("Tipo de pagamento inválido.");

            var agora = DataHoraHelper.Agora();

            // Cria ContaPagar já quitada (para aparecer no DRE)
            var cp = new ContaPagar
            {
                FilialId = caixa.FilialId,
                PessoaId = dto.PessoaId,
                PlanoContaId = dto.PlanoContaId,
                Descricao = string.IsNullOrWhiteSpace(dto.Descricao) ? $"Despesa no caixa — {plano.Descricao}" : dto.Descricao,
                Valor = dto.Valor,
                ValorFinal = dto.Valor,
                DataEmissao = agora,
                DataVencimento = agora.Date,
                DataPagamento = agora,
                Observacao = dto.Observacao,
                Status = StatusConta.Pago
            };
            _db.Set<ContaPagar>().Add(cp);
            await _db.SaveChangesAsync();

            var mov = new CaixaMovimento
            {
                CaixaId = dto.CaixaId,
                Tipo = TipoMovimentoCaixa.Pagamento,
                DataMovimento = agora,
                Valor = dto.Valor,
                TipoPagamentoId = dto.TipoPagamentoId,
                Descricao = $"Pagamento CP#{cp.Codigo ?? cp.Id.ToString()} ({tipo.Nome})",
                Observacao = dto.Observacao,
                UsuarioId = usuarioId,
                ContaPagarId = cp.Id,
                StatusConferencia = StatusConferenciaMovimento.Conferido,
                DataConferencia = agora,
                ConferidoPorUsuarioId = usuarioId
            };
            _db.CaixaMovimentos.Add(mov);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "PAGAMENTO", ENTIDADE, mov.Id, novo: new() { ["Valor"] = dto.Valor.ToString("N2"), ["ContaPagarId"] = cp.Id.ToString() });
            return mov.Id;
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em CriarPagamentoAsync"); throw; }
    }

    // ═══ Confirmação de posse (bip) ══════════════════════════════════

    public async Task<CaixaMovimentoListDto> BiparCanhotoAsync(string codigo, long usuarioId)
    {
        try
        {
            var mov = await _db.CaixaMovimentos
                .Include(m => m.TipoPagamento)
                .Include(m => m.Usuario).ThenInclude(u => u!.Colaborador).ThenInclude(c => c!.Pessoa)
                .FirstOrDefaultAsync(m => m.Codigo == codigo)
                ?? throw new ArgumentException("Canhoto não encontrado.");

            if (mov.StatusConferencia == StatusConferenciaMovimento.Conferido)
                throw new ArgumentException("Este canhoto já foi conferido.");

            if (mov.Tipo == TipoMovimentoCaixa.Sangria && mov.StatusConferencia == StatusConferenciaMovimento.PendenteConferente)
                throw new ArgumentException("Sangria requer confirmação do conferente em Financeiro > Sangrias Pendentes.");

            var agora = DataHoraHelper.Agora();
            mov.StatusConferencia = StatusConferenciaMovimento.Conferido;
            mov.DataConferencia = agora;
            mov.ConferidoPorUsuarioId = usuarioId;
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "BIPAGEM", ENTIDADE, mov.Id);
            return Mapear(mov);
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em BiparCanhotoAsync"); throw; }
    }

    public async Task ConfirmarSangriaConferenteAsync(long movimentoId, long usuarioId)
    {
        try
        {
            var mov = await _db.CaixaMovimentos
                .Include(m => m.Caixa).ThenInclude(c => c.Filial)
                .FirstOrDefaultAsync(m => m.Id == movimentoId)
                ?? throw new ArgumentException("Movimento não encontrado.");

            if (mov.Tipo != TipoMovimentoCaixa.Sangria)
                throw new ArgumentException("Movimento não é uma sangria.");
            if (mov.StatusConferencia == StatusConferenciaMovimento.Conferido)
                throw new ArgumentException("Sangria já foi confirmada.");
            if (mov.UsuarioId == usuarioId)
                throw new ArgumentException("O conferente não pode ser o mesmo operador da sangria.");
            if (mov.Caixa.Filial?.ContaCofreId == null)
                throw new ArgumentException("Conta Cofre não configurada para esta filial.");

            var agora = DataHoraHelper.Agora();
            mov.ConferenteUsuarioId = usuarioId;
            mov.DataConferenteSangria = agora;
            mov.StatusConferencia = StatusConferenciaMovimento.Conferido;
            mov.DataConferencia = agora;
            mov.ConferidoPorUsuarioId = usuarioId;

            _db.MovimentosContaBancaria.Add(new MovimentoContaBancaria
            {
                ContaBancariaId = mov.Caixa.Filial.ContaCofreId.Value,
                DataMovimento = agora,
                Tipo = TipoMovimentoBancario.Entrada,
                Valor = mov.Valor,
                Descricao = $"Sangria caixa #{mov.Caixa.Codigo ?? mov.Caixa.Id.ToString()}",
                CaixaMovimentoId = mov.Id,
                CaixaId = mov.CaixaId,
                UsuarioId = usuarioId
            });
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CONFIRMAÇÃO SANGRIA", ENTIDADE, mov.Id);
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em ConfirmarSangriaConferenteAsync"); throw; }
    }

    // ═══ Canhoto HTML (thermal 80mm) ═════════════════════════════════

    public async Task<string> GerarCanhotoHtmlAsync(long movimentoId)
    {
        var mov = await _db.CaixaMovimentos
            .Include(m => m.Caixa).ThenInclude(c => c.Filial)
            .Include(m => m.Caixa).ThenInclude(c => c.Colaborador).ThenInclude(co => co.Pessoa)
            .Include(m => m.TipoPagamento)
            .Include(m => m.Usuario).ThenInclude(u => u!.Colaborador).ThenInclude(c => c!.Pessoa)
            .FirstOrDefaultAsync(m => m.Id == movimentoId)
            ?? throw new KeyNotFoundException($"Movimento {movimentoId} não encontrado.");

        var filial = mov.Caixa.Filial;
        var tipoTxt = mov.Tipo switch
        {
            TipoMovimentoCaixa.Abertura => "ABERTURA DE CAIXA",
            TipoMovimentoCaixa.Fechamento => "FECHAMENTO DE CAIXA",
            TipoMovimentoCaixa.VendaPagamento => "VENDA — " + (mov.TipoPagamento?.Nome ?? "PAGAMENTO"),
            TipoMovimentoCaixa.Sangria => "SANGRIA",
            TipoMovimentoCaixa.Suprimento => "SUPRIMENTO",
            TipoMovimentoCaixa.Recebimento => "RECEBIMENTO",
            TipoMovimentoCaixa.Pagamento => "PAGAMENTO",
            _ => "MOVIMENTO"
        };

        var barcode = GerarCode128Html(mov.Codigo ?? "", 260, 50);
        var operadorNome = mov.Usuario?.Colaborador?.Pessoa?.Nome ?? "—";
        var caixaCodigo = mov.Caixa.Codigo ?? mov.Caixa.Id.ToString();

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Canhoto " + (mov.Codigo ?? "") + "</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("@media print { @page { size: 80mm auto; margin: 0; } body { margin: 0; } }");
        sb.AppendLine("body { font-family: 'Courier New', monospace; width: 74mm; margin: 0 auto; padding: 4mm; font-size: 10pt; color: #000; }");
        sb.AppendLine(".c{text-align:center} .r{text-align:right} .b{font-weight:bold} .l{border-top:1px dashed #000;margin:4px 0} .big{font-size:16pt}");
        sb.AppendLine(".tinfo{width:100%;border-collapse:collapse} .tinfo td{padding:1px 0;vertical-align:top}");
        sb.AppendLine(".bcode{border-collapse:collapse;margin:0 auto;-webkit-print-color-adjust:exact;print-color-adjust:exact}");
        sb.AppendLine(".bcode td{padding:0;margin:0;font-size:0;line-height:0;border:none}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<div class='c b'>{filial?.NomeFantasia ?? "ZulexPharma"}</div>");
        sb.AppendLine($"<div class='c' style='font-size:8pt'>{filial?.Cnpj ?? ""}</div>");
        sb.AppendLine($"<div class='c' style='font-size:8pt'>{filial?.Rua}, {filial?.Numero} — {filial?.Cidade}/{filial?.Uf}</div>");
        sb.AppendLine("<div class='l'></div>");
        sb.AppendLine($"<div class='c b big'>{tipoTxt}</div>");
        sb.AppendLine("<div class='l'></div>");
        sb.AppendLine("<table class='tinfo'>");
        sb.AppendLine($"<tr><td>Caixa:</td><td class='r b'>#{caixaCodigo}</td></tr>");
        sb.AppendLine($"<tr><td>Operador:</td><td class='r'>{operadorNome}</td></tr>");
        sb.AppendLine($"<tr><td>Data/Hora:</td><td class='r'>{mov.DataMovimento:dd/MM/yyyy HH:mm:ss}</td></tr>");
        if (mov.TipoPagamento != null)
            sb.AppendLine($"<tr><td>Forma:</td><td class='r'>{mov.TipoPagamento.Nome}</td></tr>");
        sb.AppendLine($"<tr><td class='b big'>VALOR:</td><td class='r b big'>R$ {mov.Valor:N2}</td></tr>");
        sb.AppendLine("</table>");
        if (!string.IsNullOrWhiteSpace(mov.Observacao))
        {
            sb.AppendLine("<div class='l'></div>");
            sb.AppendLine($"<div style='font-size:8pt'><b>Obs:</b> {System.Net.WebUtility.HtmlEncode(mov.Observacao)}</div>");
        }
        sb.AppendLine("<div class='l'></div>");
        sb.AppendLine("<div class='c'>");
        sb.AppendLine(barcode);
        sb.AppendLine($"<div class='b' style='font-size:11pt;margin-top:2px'>{mov.Codigo}</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class='l'></div>");
        if (mov.Tipo == TipoMovimentoCaixa.Sangria)
        {
            sb.AppendLine("<div style='font-size:8pt'>_______________________________</div>");
            sb.AppendLine("<div class='c' style='font-size:8pt'>Assinatura Operador</div>");
            sb.AppendLine("<div style='font-size:8pt;margin-top:8px'>_______________________________</div>");
            sb.AppendLine("<div class='c' style='font-size:8pt'>Assinatura Conferente</div>");
        }
        sb.AppendLine("<script>window.onload=function(){window.print();};</script>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    // ═══ Conferência ═════════════════════════════════════════════════

    public async Task<ConferenciaCaixaDto> ObterConferenciaAsync(long caixaId)
    {
        try
        {
            var caixa = await _db.Caixas
                .Include(c => c.Colaborador).ThenInclude(co => co.Pessoa)
                .Include(c => c.Declarados).ThenInclude(d => d.TipoPagamento)
                .Include(c => c.Movimentos).ThenInclude(m => m.TipoPagamento)
                .Include(c => c.Movimentos).ThenInclude(m => m.Usuario).ThenInclude(u => u!.Colaborador).ThenInclude(co => co!.Pessoa)
                .FirstOrDefaultAsync(c => c.Id == caixaId)
                ?? throw new KeyNotFoundException($"Caixa {caixaId} não encontrado.");

            var dto = new ConferenciaCaixaDto
            {
                CaixaId = caixa.Id,
                Codigo = caixa.Codigo,
                ModeloFechamento = caixa.ModeloFechamento,
                DataAbertura = caixa.DataAbertura,
                DataFechamento = caixa.DataFechamento,
                DataConferencia = caixa.DataConferencia,
                ColaboradorNome = caixa.Colaborador?.Pessoa?.Nome ?? "",
                ValorAbertura = caixa.ValorAbertura,
                Status = (int)caixa.Status,
                StatusDescricao = caixa.Status.ToString()
            };

            // Agrupa movimentos por forma de pagamento (abertura entra no grupo dinheiro; fechamento fica de fora — valor zero)
            var movsPorTipo = caixa.Movimentos
                .Where(m => m.Tipo != TipoMovimentoCaixa.Fechamento)
                .GroupBy(m => new { m.TipoPagamentoId, TipoPagamentoNome = m.TipoPagamento?.Nome ?? "Sem tipo", Modalidade = (int?)m.TipoPagamento?.Modalidade });

            foreach (var grupo in movsPorTipo)
            {
                var movs = grupo.Select(Mapear).ToList();
                var isDinheiro = grupo.Key.Modalidade == (int)ModalidadePagamento.VendaVista;

                decimal valorSistema;
                decimal declarado;

                if (isDinheiro)
                {
                    // DINHEIRO: sistema = entradas (abertura + venda + recebimento + suprimento) - saídas (pagamento)
                    // Sangrias NÃO entram no sistema — elas são o "declarado" que deve bater com o sistema.
                    valorSistema = grupo
                        .Where(m => m.Tipo != TipoMovimentoCaixa.Sangria
                                 && m.Tipo != TipoMovimentoCaixa.Fechamento)
                        .Sum(m => m.Tipo == TipoMovimentoCaixa.Pagamento ? -m.Valor : m.Valor);

                    // Declarado = soma das sangrias CONFERIDAS (como valor positivo)
                    declarado = grupo
                        .Where(m => m.Tipo == TipoMovimentoCaixa.Sangria
                                 && m.StatusConferencia == StatusConferenciaMovimento.Conferido)
                        .Sum(m => m.Valor);
                }
                else
                {
                    // Outras modalidades (cartão, pix, prazo): auto-conferidas, sistema = declarado = soma dos movimentos
                    valorSistema = grupo.Sum(m => m.Valor);
                    // Se houver declaração explícita (modo simples), usa ela; senão, usa o sistema
                    var declaradoExplicito = caixa.Declarados.FirstOrDefault(d => d.TipoPagamentoId == grupo.Key.TipoPagamentoId)?.ValorDeclarado;
                    declarado = declaradoExplicito ?? valorSistema;
                }

                dto.FormasPagamento.Add(new ConferenciaFormaPagamentoDto
                {
                    TipoPagamentoId = grupo.Key.TipoPagamentoId,
                    TipoPagamentoNome = grupo.Key.TipoPagamentoNome,
                    Modalidade = grupo.Key.Modalidade,
                    ValorDeclarado = declarado,
                    ValorSistema = valorSistema,
                    QtdeMovimentos = movs.Count,
                    QtdeConferidos = movs.Count(m => m.StatusConferencia == StatusConferenciaMovimento.Conferido),
                    Movimentos = movs
                });
            }

            return dto;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException) { Log.Error(ex, "Erro em ObterConferenciaAsync"); throw; }
    }

    public async Task ConferirCaixaAsync(long caixaId, ConferirCaixaFormDto dto, long usuarioId)
    {
        try
        {
            var caixa = await _db.Caixas
                .Include(c => c.Filial)
                .Include(c => c.Movimentos).ThenInclude(m => m.TipoPagamento)
                .FirstOrDefaultAsync(c => c.Id == caixaId)
                ?? throw new KeyNotFoundException("Caixa não encontrado.");

            if (caixa.Status == CaixaStatus.Conferido) throw new ArgumentException("Caixa já foi conferido.");
            if (caixa.Status != CaixaStatus.Fechado) throw new ArgumentException("Só é possível conferir caixas fechados.");
            if (caixa.Filial?.ContaCofreId == null) throw new ArgumentException("Conta Cofre não configurada para esta filial.");

            var agora = DataHoraHelper.Agora();
            var ids = new HashSet<long>(dto.MovimentoIdsConferidos);

            foreach (var mov in caixa.Movimentos)
            {
                if (ids.Contains(mov.Id) && mov.StatusConferencia != StatusConferenciaMovimento.Conferido)
                {
                    mov.StatusConferencia = StatusConferenciaMovimento.Conferido;
                    mov.DataConferencia = agora;
                    mov.ConferidoPorUsuarioId = usuarioId;
                }
            }

            // Para os movimentos em DINHEIRO conferidos que ainda não geraram MovimentoContaBancaria, cria entrada no cofre
            var dinheiroConferidos = caixa.Movimentos
                .Where(m => m.TipoPagamento?.Modalidade == ModalidadePagamento.VendaVista
                         && m.StatusConferencia == StatusConferenciaMovimento.Conferido
                         && m.Tipo != TipoMovimentoCaixa.Sangria
                         && m.Tipo != TipoMovimentoCaixa.Suprimento)
                .ToList();

            // Descontar sangrias/pagamentos já realizados (se ainda não foram)
            var saldoDinheiro = dinheiroConferidos.Sum(m =>
                (m.Tipo == TipoMovimentoCaixa.Pagamento) ? -m.Valor : m.Valor);

            if (saldoDinheiro > 0)
            {
                _db.MovimentosContaBancaria.Add(new MovimentoContaBancaria
                {
                    ContaBancariaId = caixa.Filial.ContaCofreId.Value,
                    DataMovimento = agora,
                    Tipo = TipoMovimentoBancario.Entrada,
                    Valor = saldoDinheiro,
                    Descricao = $"Conferência caixa #{caixa.Codigo ?? caixa.Id.ToString()} (saldo dinheiro)",
                    CaixaId = caixa.Id,
                    UsuarioId = usuarioId
                });
            }

            caixa.Status = CaixaStatus.Conferido;
            caixa.DataConferencia = agora;
            if (!string.IsNullOrWhiteSpace(dto.Observacao))
                caixa.Observacao = (caixa.Observacao + "\n" + dto.Observacao).Trim();

            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CONFERÊNCIA", "Caixa", caixa.Id);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em ConferirCaixaAsync"); throw; }
    }

    // ═══ Helpers ═════════════════════════════════════════════════════

    private async Task<TipoPagamento?> ObterTipoPagamentoDinheiroAsync()
    {
        return await _db.Set<TipoPagamento>()
            .Where(t => t.Modalidade == ModalidadePagamento.VendaVista && t.Ativo)
            .OrderBy(t => t.Ordem)
            .FirstOrDefaultAsync();
    }

    private static CaixaMovimentoListDto Mapear(CaixaMovimento m) => new()
    {
        Id = m.Id,
        Codigo = m.Codigo,
        CaixaId = m.CaixaId,
        VendaId = m.VendaPagamento?.VendaId,
        Tipo = m.Tipo,
        TipoDescricao = m.Tipo.ToString(),
        DataMovimento = m.DataMovimento,
        Valor = m.Valor,
        TipoPagamentoId = m.TipoPagamentoId,
        TipoPagamentoNome = m.TipoPagamento?.Nome,
        ModalidadePagamento = m.TipoPagamento != null ? (int)m.TipoPagamento.Modalidade : (int?)null,
        Descricao = m.Descricao,
        Observacao = m.Observacao,
        StatusConferencia = m.StatusConferencia,
        StatusConferenciaDescricao = m.StatusConferencia.ToString(),
        DataConferencia = m.DataConferencia,
        ConferidoPorUsuarioId = m.ConferidoPorUsuarioId,
        ConferenteUsuarioId = m.ConferenteUsuarioId,
        DataConferenteSangria = m.DataConferenteSangria,
        UsuarioNome = m.Usuario?.Colaborador?.Pessoa?.Nome
    };

    // ═══ Code128 SVG generation (inline, sem dependência) ═══════════
    // Implementação simplificada: gera retângulos SVG representando o código.
    // Para produção, considere usar Zen.Barcode ou similar.
    private static string GerarCode128Html(string texto, int totalWidth, int height)
    {
        if (string.IsNullOrEmpty(texto)) return "";

        var patterns = Code128Patterns;
        var bars = new StringBuilder();
        var checksum = 104; // Start B = 104
        int startIdx = 104;
        int position = 1;

        bars.Append(patterns[startIdx]);
        foreach (var ch in texto)
        {
            int code = ch - 32;
            if (code < 0 || code >= 95) code = 0;
            bars.Append(patterns[code]);
            checksum += code * position;
            position++;
        }
        checksum %= 103;
        bars.Append(patterns[checksum]);
        bars.Append(patterns[106]); // Stop pattern

        var bitString = bars.ToString();
        // Agrupa bits consecutivos do mesmo tipo para reduzir número de células
        // e garantir que impressoras térmicas rendam corretamente.
        var grupos = new List<(char Bit, int Count)>();
        char atual = bitString[0];
        int cont = 1;
        for (int i = 1; i < bitString.Length; i++)
        {
            if (bitString[i] == atual) cont++;
            else { grupos.Add((atual, cont)); atual = bitString[i]; cont = 1; }
        }
        grupos.Add((atual, cont));

        // Cada "unit" ocupa ~2px. Larguras múltiplas de 2px.
        const int unitPx = 2;
        var larguraTotal = bitString.Length * unitPx;
        var sb = new StringBuilder();
        sb.Append($"<table class='bcode' cellspacing='0' cellpadding='0' border='0' style='width:{larguraTotal}px;height:{height}px'><tr>");
        foreach (var g in grupos)
        {
            var color = g.Bit == '1' ? "#000" : "#fff";
            var w = g.Count * unitPx;
            sb.Append($"<td style='background-color:{color};width:{w}px;height:{height}px'>&nbsp;</td>");
        }
        sb.Append("</tr></table>");
        return sb.ToString();
    }

    // Tabela de patterns Code128 (107 entradas: 0-102 data, 103-105 Start, 106 Stop)
    private static readonly string[] Code128Patterns = new[]
    {
        "11011001100","11001101100","11001100110","10010011000","10010001100","10001001100","10011001000","10011000100",
        "10001100100","11001001000","11001000100","11000100100","10110011100","10011011100","10011001110","10111001100",
        "10011101100","10011100110","11001110010","11001011100","11001001110","11011100100","11001110100","11101101110",
        "11101001100","11100101100","11100100110","11101100100","11100110100","11100110010","11011011000","11011000110",
        "11000110110","10100011000","10001011000","10001000110","10110001000","10001101000","10001100010","11010001000",
        "11000101000","11000100010","10110111000","10110001110","10001101110","10111011000","10111000110","10001110110",
        "11101110110","11010001110","11000101110","11011101000","11011100010","11011101110","11101011000","11101000110",
        "11100010110","11101101000","11101100010","11100011010","11101111010","11001000010","11110001010","10100110000",
        "10100001100","10010110000","10010000110","10000101100","10000100110","10110010000","10110000100","10011010000",
        "10011000010","10000110100","10000110010","11000010010","11001010000","11110111010","11000010100","10001111010",
        "10100111100","10010111100","10010011110","10111100100","10011110100","10011110010","11110100100","11110010100",
        "11110010010","11011011110","11011110110","11110110110","10101111000","10100011110","10001011110","10111101000",
        "10111100010","11110101000","11110100010","10111011110","10111101110","11101011110","11110101110","11010000100",
        "11010010000","11010011100","1100011101011"
    };
}
