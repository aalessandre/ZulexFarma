using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Globalization;
using System.Text.Json;
using ZulexPharma.Application.DTOs.Fiscal;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class AtualizacaoPrecoService : IAtualizacaoPrecoService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;

    public AtualizacaoPrecoService(AppDbContext db, ILogAcaoService log)
    {
        _db = db;
        _log = log;
    }

    // ── Info da base ─────────────────────────────────────────────────
    public async Task<AbcFarmaBaseInfo> ObterInfoBaseAsync()
    {
        var total = await _db.AbcFarmaBase.CountAsync();
        var ultima = total > 0 ? await _db.AbcFarmaBase.MaxAsync(x => x.AtualizadoEm) : (DateTime?)null;
        return new AbcFarmaBaseInfo { TotalRegistros = total, UltimaAtualizacao = ultima };
    }

    // ── Upload da base ABCFarma ──────────────────────────────────────
    public async Task<UploadAbcFarmaResult> UploadBaseAsync(string conteudoJson)
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var registros = JsonSerializer.Deserialize<List<AbcFarmaJson>>(conteudoJson, opts)
            ?? throw new ArgumentException("JSON inválido.");

        var agora = DateTime.UtcNow;
        var inseridos = 0;
        var atualizados = 0;

        // Limpar base existente e reinserir (mais seguro que upsert com EANs duplicados)
        var existentes = await _db.AbcFarmaBase.CountAsync();
        if (existentes > 0)
        {
            await _db.Database.ExecuteSqlRawAsync(@"TRUNCATE TABLE ""AbcFarmaBase""");
            atualizados = existentes;
        }

        // Inserir em lotes para performance
        var lote = new List<AbcFarmaBase>(500);
        foreach (var r in registros)
        {
            var ean = r.EAN?.Trim() ?? "";
            if (string.IsNullOrEmpty(ean)) continue;

            var novo = new AbcFarmaBase { Ean = ean };
            MapAbcFarma(novo, r, agora);
            lote.Add(novo);
            inseridos++;

            if (lote.Count >= 500)
            {
                _db.AbcFarmaBase.AddRange(lote);
                await _db.SaveChangesAsync();
                _db.ChangeTracker.Clear();
                lote.Clear();
            }
        }

        if (lote.Count > 0)
        {
            _db.AbcFarmaBase.AddRange(lote);
            await _db.SaveChangesAsync();
        }

        return new UploadAbcFarmaResult
        {
            TotalRegistros = registros.Count,
            Inseridos = inseridos,
            Atualizados = atualizados
        };
    }

    // ── Processar atualização ────────────────────────────────────────
    public async Task<ProcessarAtualizacaoResult> ProcessarAsync(ProcessarAtualizacaoRequest request)
    {
        // Buscar alíquota da filial
        var filial = await _db.Filiais.FindAsync(request.FilialId)
            ?? throw new KeyNotFoundException("Filial não encontrada.");

        var aliquota = filial.AliquotaIcms;
        if (aliquota <= 0)
            throw new ArgumentException("A filial não tem alíquota de ICMS configurada. Configure em Cadastro > Filiais.");

        // Buscar produtos da filial com seus dados
        var query = _db.ProdutosDados
            .Include(d => d.Produto).ThenInclude(p => p.Barras)
            .Include(d => d.Produto).ThenInclude(p => p.GrupoPrincipal)
            .Where(d => d.FilialId == request.FilialId && d.Produto.Ativo && !d.Produto.Eliminado);

        // Filtrar por grupos principais
        if (request.GruposPrincipaisIds.Count > 0)
            query = query.Where(d => d.Produto.GrupoPrincipalId.HasValue
                && request.GruposPrincipaisIds.Contains(d.Produto.GrupoPrincipalId.Value));

        var dadosProdutos = await query.ToListAsync();

        // Carregar base ABCFarma
        var baseAbc = await _db.AbcFarmaBase.ToDictionaryAsync(x => x.Ean, x => x);

        var itensPreview = new List<AtualizacaoPrecoPreviewItem>();

        foreach (var dados in dadosProdutos)
        {
            // Buscar na ABCFarma pelo código de barras
            AbcFarmaBase? abc = null;
            foreach (var barras in dados.Produto.Barras)
            {
                if (baseAbc.TryGetValue(barras.Barras, out abc)) break;
            }
            // Fallback pelo barras principal
            if (abc == null && !string.IsNullOrEmpty(dados.Produto.CodigoBarras))
                baseAbc.TryGetValue(dados.Produto.CodigoBarras, out abc);

            if (abc == null) continue;

            // Pegar PMC pela alíquota da filial
            var pmcNovo = ObterPmcPorAliquota(abc, aliquota);
            if (pmcNovo <= 0) continue;

            var pmcAtual = dados.Pmc;
            var vendaAtual = dados.ValorVenda;

            // Calcular novo valor de venda mantendo a mesma margem
            var vendaNovo = pmcNovo; // Por padrão usa PMC como referência
            if (vendaAtual > 0 && pmcAtual > 0)
            {
                // Manter a proporção: vendaNovo = pmcNovo * (vendaAtual / pmcAtual)
                vendaNovo = Math.Round(pmcNovo * (vendaAtual / pmcAtual), 2);
            }

            // Aplicar filtro de modo
            if (request.Modo == "AUMENTAR" && vendaNovo <= vendaAtual) continue;
            if (request.Modo == "REDUZIR" && vendaNovo >= vendaAtual) continue;

            // Sem alteração
            if (vendaNovo == vendaAtual && pmcNovo == pmcAtual) continue;

            var variacao = vendaAtual > 0 ? Math.Round(((vendaNovo - vendaAtual) / vendaAtual) * 100, 2) : 0;

            itensPreview.Add(new AtualizacaoPrecoPreviewItem
            {
                ProdutoId = dados.ProdutoId,
                ProdutoDadosId = dados.Id,
                ProdutoNome = dados.Produto.Nome,
                Ean = dados.Produto.CodigoBarras,
                GrupoPrincipalNome = dados.Produto.GrupoPrincipal?.Nome,
                ValorVendaAtual = vendaAtual,
                ValorVendaNovo = vendaNovo,
                PmcAtual = pmcAtual,
                PmcNovo = pmcNovo,
                VariacaoPercent = variacao
            });
        }

        // Se modo AUTOMATICO, aplicar
        long? atualizacaoId = null;
        if (request.Acao == "AUTOMATICO" && itensPreview.Count > 0)
        {
            atualizacaoId = await AplicarAtualizacao(request, itensPreview, dadosProdutos);
        }

        return new ProcessarAtualizacaoResult
        {
            AtualizacaoPrecoId = atualizacaoId,
            TotalProdutos = dadosProdutos.Count,
            TotalAlterados = itensPreview.Count,
            Itens = itensPreview
        };
    }

    // ── Listar histórico ─────────────────────────────────────────────
    public async Task<List<AtualizacaoPrecoListDto>> ListarHistoricoAsync(long filialId)
    {
        return await _db.AtualizacoesPreco
            .Where(a => a.FilialId == filialId)
            .OrderByDescending(a => a.DataExecucao)
            .Take(50)
            .Select(a => new AtualizacaoPrecoListDto
            {
                Id = a.Id,
                Tipo = a.Tipo,
                DataExecucao = a.DataExecucao,
                NomeUsuario = a.NomeUsuario,
                TotalProdutos = a.TotalProdutos,
                TotalAlterados = a.TotalAlterados,
                Status = a.Status
            })
            .ToListAsync();
    }

    // ── Reverter ─────────────────────────────────────────────────────
    public async Task ReverterAsync(long atualizacaoPrecoId)
    {
        var atualizacao = await _db.AtualizacoesPreco
            .Include(a => a.Itens)
            .FirstOrDefaultAsync(a => a.Id == atualizacaoPrecoId)
            ?? throw new KeyNotFoundException("Atualização não encontrada.");

        if (atualizacao.Status == "REVERTIDA")
            throw new ArgumentException("Esta atualização já foi revertida.");

        foreach (var item in atualizacao.Itens)
        {
            var dados = await _db.ProdutosDados.FindAsync(item.ProdutoDadosId);
            if (dados == null) continue;

            dados.ValorVenda = item.ValorVendaAnterior;
            dados.Pmc = item.PmcAnterior;
        }

        atualizacao.Status = "REVERTIDA";
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync("Atualização Preços", "REVERSÃO", "AtualizacaoPreco", atualizacaoPrecoId,
            novo: new Dictionary<string, string?> {
                ["Tipo"] = atualizacao.Tipo,
                ["Produtos revertidos"] = atualizacao.TotalAlterados.ToString()
            });
    }

    // ═════════════════════════════════════════════════════════════════
    // HELPERS
    // ═════════════════════════════════════════════════════════════════

    private async Task<long> AplicarAtualizacao(
        ProcessarAtualizacaoRequest request,
        List<AtualizacaoPrecoPreviewItem> itens,
        List<ProdutoDados> dadosProdutos)
    {
        var atualizacao = new AtualizacaoPreco
        {
            FilialId = request.FilialId,
            Tipo = "ABCFARMA",
            DataExecucao = DateTime.UtcNow,
            FiltroJson = JsonSerializer.Serialize(new { request.Modo, request.GruposPrincipaisIds, request.ReajustarPromocoes, request.ReajustarOfertas }),
            TotalProdutos = dadosProdutos.Count,
            TotalAlterados = itens.Count,
            Status = "APLICADA"
        };
        _db.AtualizacoesPreco.Add(atualizacao);
        await _db.SaveChangesAsync();

        foreach (var item in itens)
        {
            var dados = dadosProdutos.FirstOrDefault(d => d.Id == item.ProdutoDadosId);
            if (dados == null) continue;

            // Salvar snapshot antes
            _db.AtualizacoesPrecoItens.Add(new AtualizacaoPrecoItem
            {
                AtualizacaoPrecoId = atualizacao.Id,
                ProdutoId = item.ProdutoId,
                ProdutoDadosId = item.ProdutoDadosId,
                ProdutoNome = item.ProdutoNome,
                ValorVendaAnterior = dados.ValorVenda,
                ValorVendaNovo = item.ValorVendaNovo,
                PmcAnterior = dados.Pmc,
                PmcNovo = item.PmcNovo,
                CustoMedioAnterior = dados.CustoMedio,
                MarkupAnterior = dados.Markup,
                ProjecaoLucroAnterior = dados.ProjecaoLucro
            });

            // Aplicar novos valores
            dados.ValorVenda = item.ValorVendaNovo;
            dados.Pmc = item.PmcNovo;
        }

        await _db.SaveChangesAsync();

        await _log.RegistrarAsync("Atualização Preços", "APLICAÇÃO", "AtualizacaoPreco", atualizacao.Id,
            novo: new Dictionary<string, string?> {
                ["Tipo"] = "ABCFARMA",
                ["Modo"] = request.Modo,
                ["Produtos alterados"] = itens.Count.ToString(),
                ["Total analisados"] = dadosProdutos.Count.ToString()
            });

        return atualizacao.Id;
    }

    private static decimal ObterPmcPorAliquota(AbcFarmaBase abc, decimal aliquota)
    {
        return aliquota switch
        {
            0 => abc.Pmc0,
            12 => abc.Pmc12,
            17 => abc.Pmc17,
            17.5m => abc.Pmc17, // Fallback 17
            18 => abc.Pmc18,
            19 => abc.Pmc19,
            19.5m => abc.Pmc195,
            20 => abc.Pmc20,
            20.5m => abc.Pmc205,
            21 => abc.Pmc21,
            22 => abc.Pmc22,
            22.5m => abc.Pmc225,
            23 => abc.Pmc23,
            _ => abc.Pmc18 // Fallback padrão
        };
    }

    private static void MapAbcFarma(AbcFarmaBase e, AbcFarmaJson r, DateTime agora)
    {
        e.RegistroAnvisa = r.REGISTRO_ANVISA?.Trim();
        e.Nome = (r.NOME ?? "").Trim().ToUpper();
        e.Descricao = r.DESCRICAO?.Trim();
        e.Composicao = r.COMPOSICAO?.Trim();
        e.NomeFabricante = r.NOME_FABRICANTE?.Trim();
        e.ClasseTerapeutica = r.CLASSE_TERAPEUTICA?.Trim();
        e.Ncm = r.NCM?.Trim();
        e.Pf0 = Dec(r.PF_0); e.Pmc0 = Dec(r.PMC_0);
        e.Pf12 = Dec(r.PF_12); e.Pmc12 = Dec(r.PMC_12);
        e.Pf17 = Dec(r.PF_17); e.Pmc17 = Dec(r.PMC_17);
        e.Pf18 = Dec(r.PF_18); e.Pmc18 = Dec(r.PMC_18);
        e.Pf19 = Dec(r.PF_19); e.Pmc19 = Dec(r.PMC_19);
        e.Pf195 = Dec(r.PF_19_5); e.Pmc195 = Dec(r.PMC_19_5);
        e.Pf20 = Dec(r.PF_20); e.Pmc20 = Dec(r.PMC_20);
        e.Pf205 = Dec(r.PF_20_5); e.Pmc205 = Dec(r.PMC_20_5);
        e.Pf21 = Dec(r.PF_21); e.Pmc21 = Dec(r.PMC_21);
        e.Pf22 = Dec(r.PF_22); e.Pmc22 = Dec(r.PMC_22);
        e.Pf225 = Dec(r.PF_22_5); e.Pmc225 = Dec(r.PMC_22_5);
        e.Pf23 = Dec(r.PF_23); e.Pmc23 = Dec(r.PMC_23);
        e.PercentualIpi = Dec(r.PERCENTUAL_IPI);
        if (DateTime.TryParse(r.DATA_VIGENCIA, out var dv))
            e.DataVigencia = DateTime.SpecifyKind(dv, DateTimeKind.Utc);
        e.AtualizadoEm = agora;
    }

    private static decimal Dec(string? v)
        => decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;

    // ── JSON DTO para deserialização do arquivo ABCFarma ─────────────
    private class AbcFarmaJson
    {
        public string? EAN { get; set; }
        public string? REGISTRO_ANVISA { get; set; }
        public string? NOME { get; set; }
        public string? DESCRICAO { get; set; }
        public string? COMPOSICAO { get; set; }
        public string? NOME_FABRICANTE { get; set; }
        public string? CLASSE_TERAPEUTICA { get; set; }
        public string? NCM { get; set; }
        public string? PF_0 { get; set; }
        public string? PMC_0 { get; set; }
        public string? PF_12 { get; set; }
        public string? PMC_12 { get; set; }
        public string? PF_17 { get; set; }
        public string? PMC_17 { get; set; }
        public string? PF_18 { get; set; }
        public string? PMC_18 { get; set; }
        public string? PF_19 { get; set; }
        public string? PMC_19 { get; set; }
        public string? PF_19_5 { get; set; }
        public string? PMC_19_5 { get; set; }
        public string? PF_20 { get; set; }
        public string? PMC_20 { get; set; }
        public string? PF_20_5 { get; set; }
        public string? PMC_20_5 { get; set; }
        public string? PF_21 { get; set; }
        public string? PMC_21 { get; set; }
        public string? PF_22 { get; set; }
        public string? PMC_22 { get; set; }
        public string? PF_22_5 { get; set; }
        public string? PMC_22_5 { get; set; }
        public string? PF_23 { get; set; }
        public string? PMC_23 { get; set; }
        public string? PERCENTUAL_IPI { get; set; }
        public string? DATA_VIGENCIA { get; set; }
    }
}
