using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/desconto-engine")]
public class DescontoEngineController : ControllerBase
{
    private readonly AppDbContext _db;

    public DescontoEngineController(AppDbContext db) { _db = db; }

    /// <summary>
    /// Resolve qual hierarquia usar e calcula o desconto para um produto.
    /// Prioridade: Cliente > Convênio > Colaborador > Padrão
    /// </summary>
    [HttpGet("resolver")]
    public async Task<IActionResult> ResolverDesconto(
        [FromQuery] long produtoId,
        [FromQuery] long filialId,
        [FromQuery] long? clienteId = null,
        [FromQuery] long? convenioId = null,
        [FromQuery] long? colaboradorId = null,
        [FromQuery] long? tipoPagamentoId = null)
    {
        try
        {
            // 1. Encontrar a hierarquia correta
            var hierarquia = await BuscarHierarquia(clienteId, convenioId, colaboradorId);
            if (hierarquia == null)
                return Ok(new { success = true, data = new { hierarquiaId = (long?)null, hierarquiaNome = (string?)null, descontoMinimo = 0m, descontoMaxSemSenha = 0m, descontoMaxComSenha = 0m, descontoAplicar = 0m, aplicarAutomatico = false, componente = (string?)null } });

            // 2. Buscar dados do produto
            var produto = await _db.Produtos
                .Include(p => p.Fabricante)
                .FirstOrDefaultAsync(p => p.Id == produtoId);
            if (produto == null)
                return NotFound(new { success = false, message = "Produto não encontrado." });

            var dados = await _db.ProdutosDados
                .FirstOrDefaultAsync(d => d.ProdutoId == produtoId && d.FilialId == filialId);

            // 3. Percorrer itens da hierarquia na ordem
            var itens = hierarquia.Itens.OrderBy(i => i.Ordem).ToList();
            Log.Information("DescontoEngine: Hierarquia '{Nome}' com {Total} itens para produto {ProdutoId}", hierarquia.Nome, itens.Count, produtoId);
            Log.Information("DescontoEngine: Produto GrupoPrincipalId={GP}, GrupoProdutoId={G}, SubGrupoId={SG}, FabricanteId={F}", produto.GrupoPrincipalId, produto.GrupoProdutoId, produto.SubGrupoId, produto.FabricanteId);
            if (dados != null) Log.Information("DescontoEngine: ProdutoDados DescontoMin={Min}, DescontoMaxSS={MaxSS}, DescontoMaxCS={MaxCS}, SecaoId={S}", dados.DescontoMinimo, dados.DescontoMaxSemSenha, dados.DescontoMaxComSenha, dados.SecaoId);
            else Log.Warning("DescontoEngine: ProdutoDados NÃO ENCONTRADO para produto {ProdutoId} filial {FilialId}", produtoId, filialId);

            foreach (var item in itens)
            {
                Log.Information("DescontoEngine: Avaliando componente {Componente} (ordem {Ordem})", ComponenteNome(item.Componente), item.Ordem);
                var resultado = await AvaliarComponente(item, produto, dados, hierarquia, clienteId, convenioId, tipoPagamentoId, filialId);
                if (resultado != null)
                {
                    Log.Information("DescontoEngine: MATCH! Componente {Comp} retornou Min={Min}, MaxSS={MaxSS}, MaxCS={MaxCS}", ComponenteNome(item.Componente), resultado.DescontoMinimo, resultado.DescontoMaxSemSenha, resultado.DescontoMaxComSenha);
                    var descontoAplicar = hierarquia.AplicarAutomatico
                        ? (hierarquia.DescontoAutoTipo == DescontoAutoTipo.Minimo ? resultado.DescontoMinimo : resultado.DescontoMaxSemSenha)
                        : 0m;

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            hierarquiaId = hierarquia.Id,
                            hierarquiaNome = hierarquia.Nome,
                            descontoMinimo = resultado.DescontoMinimo,
                            descontoMaxSemSenha = resultado.DescontoMaxSemSenha,
                            descontoMaxComSenha = resultado.DescontoMaxComSenha,
                            descontoAplicar,
                            aplicarAutomatico = hierarquia.AplicarAutomatico,
                            componente = ComponenteNome(item.Componente)
                        }
                    });
                }
            }

            // Nenhum componente retornou desconto
            return Ok(new { success = true, data = new { hierarquiaId = hierarquia.Id, hierarquiaNome = hierarquia.Nome, descontoMinimo = 0m, descontoMaxSemSenha = 0m, descontoMaxComSenha = 0m, descontoAplicar = 0m, aplicarAutomatico = false, componente = (string?)null } });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em DescontoEngineController.ResolverDesconto"); return StatusCode(500, new { success = false, message = "Erro ao resolver desconto." }); }
    }

    /// <summary>Retorna promoções ativas para um produto (fixas e progressivas).</summary>
    [HttpGet("promocoes")]
    public async Task<IActionResult> BuscarPromocoes(
        [FromQuery] long produtoId,
        [FromQuery] long filialId,
        [FromQuery] long? tipoPagamentoId = null)
    {
        try
        {
            var agora = DataHoraHelper.Agora();
            var diaAtual = (int)Math.Pow(2, (int)agora.DayOfWeek);

            var query = _db.PromocaoProdutos
                .Include(pp => pp.Promocao).ThenInclude(p => p.Filiais)
                .Include(pp => pp.Promocao).ThenInclude(p => p.Pagamentos)
                .Include(pp => pp.Promocao).ThenInclude(p => p.Faixas)
                .Where(pp => pp.ProdutoId == produtoId
                    && pp.Promocao.Ativo
                    && pp.Promocao.DataHoraInicio <= agora
                    && (pp.Promocao.DataHoraFim == null || pp.Promocao.DataHoraFim >= agora)
                    && (pp.Promocao.DiaSemana & diaAtual) != 0
                    && pp.Promocao.Filiais.Any(f => f.FilialId == filialId));

            if (tipoPagamentoId.HasValue)
                query = query.Where(pp => !pp.Promocao.Pagamentos.Any() || pp.Promocao.Pagamentos.Any(pg => pg.TipoPagamentoId == tipoPagamentoId));

            var promos = await query.ToListAsync();
            Log.Information("BuscarPromocoes: encontradas {Count} promoções para produtoId={ProdutoId}", promos.Count, produtoId);

            var resultado = promos.Select(pp => new
            {
                promocaoId = pp.PromocaoId,
                nome = pp.Promocao.Nome,
                tipo = (int)pp.Promocao.Tipo, // 1=Fixa, 2=Progressiva
                tipoDescricao = pp.Promocao.Tipo == Domain.Enums.TipoPromocao.Fixa ? "Fixa" : "Progressiva",
                percentualPromocao = pp.PercentualPromocao,
                valorPromocao = pp.ValorPromocao,
                qtdeLimite = pp.QtdeLimite,
                qtdeVendida = pp.QtdeVendida,
                percentualAposLimite = pp.PercentualAposLimite,
                valorAposLimite = pp.ValorAposLimite,
                qtdeMaxPorVenda = pp.Promocao.QtdeMaxPorVenda,
                permitirMudarPreco = pp.Promocao.PermitirMudarPreco,
                faixas = pp.Promocao.Faixas.OrderBy(f => f.Quantidade).Select(f => new
                {
                    quantidade = f.Quantidade,
                    percentualDesconto = f.PercentualDesconto
                }).ToList()
            }).ToList();

            return Ok(new { success = true, data = resultado });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em DescontoEngineController.BuscarPromocoes"); return StatusCode(500, new { success = false, message = "Erro ao buscar promoções." }); }
    }

    /// <summary>Identifica qual hierarquia usar: Cliente > Convênio > Colaborador > Padrão</summary>
    [HttpGet("hierarquia")]
    public async Task<IActionResult> IdentificarHierarquia(
        [FromQuery] long? clienteId = null,
        [FromQuery] long? convenioId = null,
        [FromQuery] long? colaboradorId = null)
    {
        try
        {
            var h = await BuscarHierarquia(clienteId, convenioId, colaboradorId);
            if (h == null)
                return Ok(new { success = true, data = (object?)null });
            return Ok(new { success = true, data = new { id = h.Id, nome = h.Nome, padrao = h.Padrao, aplicarAutomatico = h.AplicarAutomatico, descontoAutoTipo = h.DescontoAutoTipo, totalItens = h.Itens.Count } });
        }
        catch (Exception ex) { Log.Error(ex, "Erro em DescontoEngineController.IdentificarHierarquia"); return StatusCode(500, new { success = false, message = "Erro ao identificar hierarquia." }); }
    }

    // ── Buscar hierarquia por prioridade ────────────────────────────
    private async Task<HierarquiaDesconto?> BuscarHierarquia(long? clienteId, long? convenioId, long? colaboradorId)
    {
        HierarquiaDesconto? h = null;

        // 1. Por cliente (vinculado especificamente, ou hierarquia sem nenhum cliente = vale para todos)
        if (clienteId.HasValue)
        {
            h = await _db.Set<HierarquiaDesconto>()
                .Include(x => x.Itens).ThenInclude(i => i.Secoes)
                .Include(x => x.Clientes)
                .Where(x => x.Ativo && (x.Clientes.Any(c => c.ClienteId == clienteId) || !x.Clientes.Any()))
                .FirstOrDefaultAsync();
            if (h != null) return h;
        }

        // 2. Por convênio (vinculado especificamente, ou hierarquia sem nenhum convênio = vale para todos)
        if (convenioId.HasValue)
        {
            h = await _db.Set<HierarquiaDesconto>()
                .Include(x => x.Itens).ThenInclude(i => i.Secoes)
                .Include(x => x.Convenios)
                .Where(x => x.Ativo && (x.Convenios.Any(c => c.ConvenioId == convenioId) || !x.Convenios.Any()))
                .FirstOrDefaultAsync();
            if (h != null) return h;
        }

        // 3. Por colaborador (se vinculado especificamente, ou se hierarquia não tem nenhum colaborador = vale para todos)
        if (colaboradorId.HasValue)
        {
            h = await _db.Set<HierarquiaDesconto>()
                .Include(x => x.Itens).ThenInclude(i => i.Secoes)
                .Include(x => x.Colaboradores)
                .Where(x => x.Ativo && (x.Colaboradores.Any(c => c.ColaboradorId == colaboradorId) || !x.Colaboradores.Any()))
                .FirstOrDefaultAsync();
            if (h != null) return h;
        }

        // 4. Padrão
        h = await _db.Set<HierarquiaDesconto>()
            .Include(x => x.Itens).ThenInclude(i => i.Secoes)
            .Where(x => x.Ativo && x.Padrao)
            .FirstOrDefaultAsync();

        return h;
    }

    // ── Avaliar um componente da hierarquia ─────────────────────────
    private async Task<DescontoResultado?> AvaliarComponente(
        HierarquiaDescontoItem item, Produto produto, ProdutoDados? dados,
        HierarquiaDesconto hierarquia, long? clienteId, long? convenioId, long? tipoPagamentoId, long filialId)
    {
        switch (item.Componente)
        {
            case ComponenteDesconto.PromocaoFixa:
                return await AvaliarPromocao(produto.Id, filialId, tipoPagamentoId, hierarquia.BuscarMenorValorPromocao, Domain.Enums.TipoPromocao.Fixa);

            case ComponenteDesconto.PromocaoProgressiva:
                return await AvaliarPromocao(produto.Id, filialId, tipoPagamentoId, hierarquia.BuscarMenorValorPromocao, Domain.Enums.TipoPromocao.Progressiva);

            case ComponenteDesconto.SecaoEscolhida:
                if (dados?.SecaoId != null)
                {
                    var secaoIds = item.Secoes.Select(s => s.SecaoId).ToHashSet();
                    if (secaoIds.Contains(dados.SecaoId.Value))
                    {
                        var secao = await _db.Secoes.FindAsync(dados.SecaoId.Value);
                        if (secao != null && secao.DescontoMaximo > 0)
                            return new DescontoResultado { DescontoMinimo = secao.DescontoMinimo, DescontoMaxSemSenha = secao.DescontoMaximo, DescontoMaxComSenha = secao.DescontoMaximoComSenha };
                    }
                }
                return null;

            case ComponenteDesconto.SecaoDemais:
                if (dados?.SecaoId != null)
                {
                    var secaoEscolhida = hierarquia.Itens.FirstOrDefault(i => i.Componente == ComponenteDesconto.SecaoEscolhida);
                    var secaoIdsEscolhidas = secaoEscolhida?.Secoes.Select(s => s.SecaoId).ToHashSet() ?? new HashSet<long>();
                    if (!secaoIdsEscolhidas.Contains(dados.SecaoId.Value))
                    {
                        var secaoDemais = await _db.Secoes.FindAsync(dados.SecaoId.Value);
                        if (secaoDemais != null && secaoDemais.DescontoMaximo > 0)
                            return new DescontoResultado { DescontoMinimo = secaoDemais.DescontoMinimo, DescontoMaxSemSenha = secaoDemais.DescontoMaximo, DescontoMaxComSenha = secaoDemais.DescontoMaximoComSenha };
                    }
                }
                return null;

            case ComponenteDesconto.Cliente:
                if (clienteId.HasValue)
                {
                    // Desconto específico por produto
                    var descProd = await _db.ClienteDescontos
                        .FirstOrDefaultAsync(d => d.ClienteId == clienteId && d.ProdutoId == produto.Id);
                    if (descProd != null)
                        return new DescontoResultado { DescontoMinimo = descProd.DescontoMinimo, DescontoMaxSemSenha = descProd.DescontoMaxSemSenha, DescontoMaxComSenha = descProd.DescontoMaxComSenha };

                    // Desconto por agrupador
                    var descAgr = await BuscarDescontoClienteAgrupador(clienteId.Value, produto);
                    if (descAgr != null) return descAgr;

                    // Desconto geral do cliente
                    var cliente = await _db.Set<Cliente>().FindAsync(clienteId.Value);
                    if (cliente != null && cliente.DescontoGeral > 0)
                        return new DescontoResultado { DescontoMinimo = 0, DescontoMaxSemSenha = cliente.DescontoGeral, DescontoMaxComSenha = cliente.DescontoGeral };
                }
                return null;

            case ComponenteDesconto.Convenio:
                if (convenioId.HasValue)
                {
                    var descConv = await BuscarDescontoConvenioAgrupador(convenioId.Value, produto);
                    if (descConv != null) return descConv;
                }
                return null;

            case ComponenteDesconto.GrupoPrincipal:
                if (produto.GrupoPrincipalId.HasValue)
                {
                    var gp = await _db.GruposPrincipais.FindAsync(produto.GrupoPrincipalId.Value);
                    if (gp != null && gp.DescontoMaximo > 0)
                        return new DescontoResultado { DescontoMinimo = gp.DescontoMinimo, DescontoMaxSemSenha = gp.DescontoMaximo, DescontoMaxComSenha = gp.DescontoMaximoComSenha };
                }
                return null;

            case ComponenteDesconto.Grupo:
                if (produto.GrupoProdutoId.HasValue)
                {
                    var gr = await _db.GruposProdutos.FindAsync(produto.GrupoProdutoId.Value);
                    if (gr != null && gr.DescontoMaximo > 0)
                        return new DescontoResultado { DescontoMinimo = gr.DescontoMinimo, DescontoMaxSemSenha = gr.DescontoMaximo, DescontoMaxComSenha = gr.DescontoMaximoComSenha };
                }
                return null;

            case ComponenteDesconto.SubGrupo:
                if (produto.SubGrupoId.HasValue)
                {
                    var sg = await _db.SubGrupos.FindAsync(produto.SubGrupoId.Value);
                    if (sg != null && sg.DescontoMaximo > 0)
                        return new DescontoResultado { DescontoMinimo = sg.DescontoMinimo, DescontoMaxSemSenha = sg.DescontoMaximo, DescontoMaxComSenha = sg.DescontoMaximoComSenha };
                }
                return null;

            case ComponenteDesconto.CondPagamento:
                if (tipoPagamentoId.HasValue)
                {
                    var tp = await _db.TiposPagamento.FindAsync(tipoPagamentoId.Value);
                    if (tp != null && tp.DescontoMaxSemSenha > 0)
                        return new DescontoResultado { DescontoMinimo = tp.DescontoMinimo, DescontoMaxSemSenha = tp.DescontoMaxSemSenha, DescontoMaxComSenha = tp.DescontoMaxComSenha };
                }
                return null;

            case ComponenteDesconto.Produto:
                if (dados != null && dados.DescontoMaxSemSenha > 0)
                    return new DescontoResultado { DescontoMinimo = dados.DescontoMinimo, DescontoMaxSemSenha = dados.DescontoMaxSemSenha, DescontoMaxComSenha = dados.DescontoMaxComSenha };
                return null;

            default:
                return null;
        }
    }

    private async Task<DescontoResultado?> AvaliarPromocao(long produtoId, long filialId, long? tipoPagamentoId, bool buscarMenor, Domain.Enums.TipoPromocao? tipoFiltro = null)
    {
        var agora = DataHoraHelper.Agora();
        var diaAtual = (int)Math.Pow(2, (int)agora.DayOfWeek);
        Log.Information("AvaliarPromocao: produtoId={ProdutoId}, tipo={Tipo}, agora={Agora}", produtoId, tipoFiltro, agora);

        var query = _db.PromocaoProdutos
            .Include(pp => pp.Promocao).ThenInclude(p => p.Filiais)
            .Include(pp => pp.Promocao).ThenInclude(p => p.Pagamentos)
            .Where(pp => pp.ProdutoId == produtoId
                && pp.Promocao.Ativo
                && pp.Promocao.DataHoraInicio <= agora
                && (pp.Promocao.DataHoraFim == null || pp.Promocao.DataHoraFim >= agora)
                && (pp.Promocao.DiaSemana & diaAtual) != 0
                && pp.Promocao.Filiais.Any(f => f.FilialId == filialId));

        if (tipoFiltro.HasValue)
            query = query.Where(pp => pp.Promocao.Tipo == tipoFiltro.Value);

        if (tipoPagamentoId.HasValue)
            query = query.Where(pp => pp.Promocao.Pagamentos.Any(pg => pg.TipoPagamentoId == tipoPagamentoId));

        var promos = await query.ToListAsync();
        if (promos.Count == 0) return null;

        var promo = buscarMenor
            ? promos.OrderBy(p => p.PercentualPromocao).First()
            : promos.OrderByDescending(p => p.PercentualPromocao).First();

        return new DescontoResultado { DescontoMinimo = promo.PercentualPromocao, DescontoMaxSemSenha = promo.PercentualPromocao, DescontoMaxComSenha = promo.PercentualPromocao };
    }

    private async Task<DescontoResultado?> BuscarDescontoClienteAgrupador(long clienteId, Produto produto)
    {
        var descontos = await _db.ClienteDescontos
            .Where(d => d.ClienteId == clienteId && d.ProdutoId == null)
            .ToListAsync();

        foreach (var d in descontos)
        {
            bool match = d.TipoAgrupador switch
            {
                TipoAgrupador.GrupoPrincipal => produto.GrupoPrincipalId == d.AgrupadorId,
                TipoAgrupador.Grupo => produto.GrupoProdutoId == d.AgrupadorId,
                TipoAgrupador.SubGrupo => produto.SubGrupoId == d.AgrupadorId,
                _ => false
            };
            if (match) return new DescontoResultado { DescontoMinimo = d.DescontoMinimo, DescontoMaxSemSenha = d.DescontoMaxSemSenha, DescontoMaxComSenha = d.DescontoMaxComSenha };
        }
        return null;
    }

    private async Task<DescontoResultado?> BuscarDescontoConvenioAgrupador(long convenioId, Produto produto)
    {
        var descontos = await _db.ConvenioDescontos
            .Where(d => d.ConvenioId == convenioId)
            .ToListAsync();

        foreach (var d in descontos)
        {
            bool match = d.TipoAgrupador switch
            {
                TipoAgrupador.GrupoPrincipal => produto.GrupoPrincipalId == d.AgrupadorId,
                TipoAgrupador.Grupo => produto.GrupoProdutoId == d.AgrupadorId,
                TipoAgrupador.SubGrupo => produto.SubGrupoId == d.AgrupadorId,
                _ => false
            };
            if (match) return new DescontoResultado { DescontoMinimo = d.DescontoMinimo, DescontoMaxSemSenha = d.DescontoMaxSemSenha, DescontoMaxComSenha = d.DescontoMaxComSenha };
        }
        return null;
    }

    private static string ComponenteNome(ComponenteDesconto c) => c switch
    {
        ComponenteDesconto.PromocaoFixa => "Promoção Fixa",
        ComponenteDesconto.PromocaoProgressiva => "Promoção Progressiva",
        ComponenteDesconto.SecaoEscolhida => "Seção (escolhida)",
        ComponenteDesconto.SecaoDemais => "Seção (demais)",
        ComponenteDesconto.PBM => "PBM",
        ComponenteDesconto.Cliente => "Cliente",
        ComponenteDesconto.Convenio => "Convênio",
        ComponenteDesconto.Familia => "Família",
        ComponenteDesconto.GrupoPrincipal => "Grupo Principal",
        ComponenteDesconto.Grupo => "Grupo",
        ComponenteDesconto.SubGrupo => "SubGrupo",
        ComponenteDesconto.Fabricante => "Fabricante",
        ComponenteDesconto.CondPagamento => "Cond. Pagamento",
        ComponenteDesconto.Produto => "Produto",
        _ => ""
    };

    private class DescontoResultado
    {
        public decimal DescontoMinimo { get; set; }
        public decimal DescontoMaxSemSenha { get; set; }
        public decimal DescontoMaxComSenha { get; set; }
    }
}
