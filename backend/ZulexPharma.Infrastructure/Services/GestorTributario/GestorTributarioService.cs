using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ZulexPharma.Application.DTOs.GestorTributario;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services.GestorTributario;

/// <summary>
/// Orquestrador do Gestor Tributário:
///  • Mantém cache em memória de consultas por EAN (5 min TTL)
///  • Valida rate limit antes de cada chamada
///  • Incrementa contador de uso mensal
///  • Dispara jobs em background (Task.Run com IServiceScopeFactory) para não travar request HTTP
///  • Persiste progresso dos jobs no banco
///  • Aplica dados fiscais nos ProdutoFiscal respeitando a flag NaoAtualizarGestorTributario
/// </summary>
public class GestorTributarioService : IGestorTributarioService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private readonly IGestorTributarioProvider _provider;
    private readonly IServiceScopeFactory _scopeFactory;

    // Cache em memória (EAN → dados, com timestamp para TTL)
    private static readonly ConcurrentDictionary<string, (DateTime ts, ProdutoFiscalExternoDto dto)> _cacheEan = new();
    private static readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(5);

    private const string TELA = "Gestor Tributário";

    public GestorTributarioService(
        AppDbContext db,
        ILogAcaoService log,
        IGestorTributarioProvider provider,
        IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _log = log;
        _provider = provider;
        _scopeFactory = scopeFactory;
    }

    // ══ Status ═══════════════════════════════════════════════════════
    public async Task<GestorTributarioStatusDto> ObterStatusAsync()
    {
        var cfg = await _db.Configuracoes
            .Where(c => c.Chave.StartsWith("gestor.") )
            .ToDictionaryAsync(c => c.Chave, c => c.Valor ?? "");

        var providerCfg = cfg.GetValueOrDefault("gestor.tributario.provider", "");
        var idParceiro = cfg.GetValueOrDefault("gestor.avant.id_parceiro", "");
        var cnpj = cfg.GetValueOrDefault("gestor.avant.cnpj_cliente", "");
        var token = cfg.GetValueOrDefault("gestor.avant.token", "");
        var cMun = cfg.GetValueOrDefault("gestor.avant.cod_municipio", "");

        var configurado = !string.IsNullOrWhiteSpace(idParceiro)
                       && !string.IsNullOrWhiteSpace(cnpj)
                       && !string.IsNullOrWhiteSpace(token)
                       && !string.IsNullOrWhiteSpace(cMun);

        var agora = DateTime.UtcNow;
        var uso = await _db.GestorTributarioUsoMensais
            .FirstOrDefaultAsync(u => u.Ano == agora.Year && u.Mes == agora.Month && u.Provider == _provider.Nome);

        return new GestorTributarioStatusDto
        {
            Configurado = configurado,
            Ativo = configurado && providerCfg == _provider.Nome,
            Provider = providerCfg,
            CnpjCliente = cnpj,
            IdParceiro = int.TryParse(idParceiro, out var ip) ? ip : (int?)null,
            TokenDefinido = !string.IsNullOrWhiteSpace(token),
            Ano = agora.Year,
            Mes = agora.Month,
            RequisicoesUsadas = uso?.RequisicoesUsadas ?? 0,
            LimiteMensal = _provider.LimiteMensal,
            UltimaChamadaEm = uso?.UltimaChamadaEm
        };
    }

    // ══ Consulta por EAN ═════════════════════════════════════════════
    public async Task<ProdutoFiscalExternoDto?> ConsultarPorEanAsync(string ean)
    {
        if (string.IsNullOrWhiteSpace(ean)) return null;

        // Cache
        if (_cacheEan.TryGetValue(ean, out var hit) && DateTime.UtcNow - hit.ts < _cacheTTL)
        {
            return hit.dto;
        }

        await VerificarRateLimitOuFalharAsync();

        // Se o produto já existe no banco, pega os dados atuais pra Avant revisar.
        // Se é um produto novo (só tem EAN), usa defaults (o provider preenche).
        var produto = await _db.Produtos
            .Include(p => p.Fiscais).ThenInclude(f => f.Ncm)
            .FirstOrDefaultAsync(p => p.CodigoBarras == ean);

        ProdutoRevisaoDto dtoEnvio;
        if (produto != null)
        {
            dtoEnvio = MontarDtoRevisao(produto);
        }
        else
        {
            dtoEnvio = new ProdutoRevisaoDto
            {
                CodInterno = ean,
                Ean = ean,
                Descricao = ""   // será substituída por "PRODUTO EAN {ean}" no provider
            };
        }

        var resultado = await _provider.RevisarLoteAsync(new List<ProdutoRevisaoDto> { dtoEnvio });
        await IncrementarUsoAsync(1, tipo: "revisao");

        var dto = resultado.Itens.FirstOrDefault();
        if (dto != null && dto.Encontrado)
        {
            _cacheEan[ean] = (DateTime.UtcNow, dto);
        }
        return dto;
    }

    // ══ Revisar produto individual ═══════════════════════════════════
    public async Task<ProdutoFiscalExternoDto?> RevisarProdutoAsync(long produtoId, long? usuarioId, bool forcar = false)
    {
        var produto = await _db.Produtos
            .Include(p => p.Fiscais)
            .Include(p => p.Dados)
            .FirstOrDefaultAsync(p => p.Id == produtoId)
            ?? throw new KeyNotFoundException($"Produto {produtoId} não encontrado.");

        // Verifica flag NaoAtualizarGestorTributario (em ProdutoDados, pode variar por filial)
        var bloqueado = !forcar && produto.Dados.Any(d => d.NaoAtualizarGestorTributario);
        if (bloqueado)
            throw new InvalidOperationException("Produto marcado como 'Não atualizar Gestor Tributário'. Use a opção 'forçar' para sobrescrever.");

        if (string.IsNullOrWhiteSpace(produto.CodigoBarras))
            throw new InvalidOperationException("Produto sem código de barras — não é possível consultar o Gestor Tributário.");

        await VerificarRateLimitOuFalharAsync();

        var itens = new List<ProdutoRevisaoDto> { MontarDtoRevisao(produto) };
        var resultado = await _provider.RevisarLoteAsync(itens);
        await IncrementarUsoAsync(1, tipo: "revisao");

        var dto = resultado.Itens.FirstOrDefault();
        if (dto == null || !dto.Encontrado) return null;

        // Carrega UF por filial para mapear o icms_entrada.por_uf corretamente
        var ufPorFilial = await _db.Filiais
            .Where(fi => produto.Fiscais.Select(pf => pf.FilialId).Contains(fi.Id))
            .ToDictionaryAsync(fi => fi.Id, fi => fi.Uf);

        AplicarDadosFiscaisStaticComUf(produto, dto, _provider.Nome, ufPorFilial);
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync(TELA, "REVISÃO INDIVIDUAL", "Produto", produtoId, novo: new Dictionary<string, string?>
        {
            ["EAN"] = produto.CodigoBarras,
            ["NCM"] = dto.Ncm,
            ["CEST"] = dto.Cest,
            ["CFOP"] = dto.Cfop
        });

        return dto;
    }

    // ══ Revisar base em background ═══════════════════════════════════
    public async Task<long> IniciarRevisaoBaseAsync(RevisarBaseRequest req, long? usuarioId)
    {
        // Pré-conta quantos produtos serão afetados (para calcular requisições necessárias)
        var queryCount = MontarQuery(_db, req);
        var totalProdutos = await queryCount.CountAsync();

        if (totalProdutos == 0)
            throw new InvalidOperationException("Nenhum produto corresponde aos filtros selecionados.");

        // Rate limit defensivo: preditivo
        var requisicoesNecessarias = (int)Math.Ceiling(totalProdutos / 300.0);
        var status = await ObterStatusAsync();
        if (status.RequisicoesDisponiveis < requisicoesNecessarias)
        {
            throw new InvalidOperationException(
                $"Rate limit: esta revisão usaria {requisicoesNecessarias} requisições, mas só restam {status.RequisicoesDisponiveis} no mês.");
        }

        // Cria job persistente
        var job = new GestorTributarioJob
        {
            Tipo = TipoJobGestorTributario.RevisaoBase,
            Status = StatusJobGestorTributario.Pendente,
            Provider = _provider.Nome,
            TotalItens = totalProdutos,
            FiltroJson = JsonSerializer.Serialize(req),
            UsuarioId = usuarioId
        };
        _db.GestorTributarioJobs.Add(job);
        await _db.SaveChangesAsync();

        // Dispara em background (não trava a request HTTP)
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            try
            {
                await ExecutarRevisaoBaseAsync(scope.ServiceProvider, job.Id, req);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GestorTributario: falha fatal no job {JobId}", job.Id);
            }
        });

        return job.Id;
    }

    /// <summary>Executor do job — roda num scope próprio.</summary>
    private static async Task ExecutarRevisaoBaseAsync(IServiceProvider sp, long jobId, RevisarBaseRequest req)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var provider = sp.GetRequiredService<IGestorTributarioProvider>();

        var job = await db.GestorTributarioJobs.FindAsync(jobId);
        if (job == null) return;

        job.Status = StatusJobGestorTributario.Executando;
        job.DataInicio = DateTime.UtcNow;
        await db.SaveChangesAsync();

        try
        {
            // Carrega os produtos a revisar
            var produtos = await MontarQuery(db, req)
                .Include(p => p.Fiscais).ThenInclude(f => f.Ncm)
                .Include(p => p.Dados)
                .ToListAsync();

            // Carrega UF de todas as filiais envolvidas (para mapear ICMS entrada por UF)
            var filiaisIds = produtos.SelectMany(p => p.Fiscais).Select(f => f.FilialId).Distinct().ToList();
            var ufPorFilial = await db.Filiais
                .Where(f => filiaisIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, f => f.Uf);

            // Processa em chunks de 300 (limite da Avant)
            const int TAM = 300;
            for (int offset = 0; offset < produtos.Count; offset += TAM)
            {
                var chunk = produtos.Skip(offset).Take(TAM).ToList();

                // Se forçar=false, pula produtos com NaoAtualizarGestorTributario
                var elegiveis = req.ForcarAtualizacao
                    ? chunk
                    : chunk.Where(p => !p.Dados.Any(d => d.NaoAtualizarGestorTributario)).ToList();

                if (elegiveis.Count == 0)
                {
                    job.ItensProcessados += chunk.Count;
                    await db.SaveChangesAsync();
                    continue;
                }

                var itens = elegiveis
                    .Where(p => !string.IsNullOrWhiteSpace(p.CodigoBarras))
                    .Select(MontarDtoRevisao)
                    .ToList();

                if (itens.Count == 0)
                {
                    job.ItensProcessados += chunk.Count;
                    await db.SaveChangesAsync();
                    continue;
                }

                ResultadoRevisaoDto? resultado = null;
                try
                {
                    resultado = await provider.RevisarLoteAsync(itens);
                    await IncrementarUsoInternoAsync(db, provider.Nome, 1, "revisao");
                    job.RequisicoesUsadas++;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Job {JobId}: erro no chunk offset={Offset}", jobId, offset);
                    job.ItensComErro += itens.Count;
                    job.MensagemErro = ex.Message;
                    if (ex.Message.Contains("429") || ex.Message.Contains("Limite"))
                    {
                        // Hard stop no rate limit
                        job.Status = StatusJobGestorTributario.Erro;
                        job.DataFim = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                        return;
                    }
                }

                if (resultado != null)
                {
                    var porEan = resultado.Itens.ToDictionary(i => i.Ean ?? "", i => i, StringComparer.OrdinalIgnoreCase);
                    foreach (var produto in elegiveis)
                    {
                        if (string.IsNullOrEmpty(produto.CodigoBarras)) { job.ItensNaoEncontrados++; continue; }
                        if (porEan.TryGetValue(produto.CodigoBarras, out var dto) && dto.Encontrado)
                        {
                            AplicarDadosFiscaisStaticComUf(produto, dto, provider.Nome, ufPorFilial);
                            job.ItensAtualizados++;
                        }
                        else
                        {
                            job.ItensNaoEncontrados++;
                        }
                    }
                }

                job.ItensProcessados += chunk.Count;
                await db.SaveChangesAsync();
            }

            job.Status = StatusJobGestorTributario.Concluido;
            job.DataFim = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Job {JobId}: erro fatal", jobId);
            job.Status = StatusJobGestorTributario.Erro;
            job.MensagemErro = ex.Message;
            job.DataFim = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    private static IQueryable<Produto> MontarQuery(AppDbContext db, RevisarBaseRequest req)
    {
        var q = db.Produtos.Where(p => !p.Eliminado);
        if (req.GrupoProdutoId.HasValue) q = q.Where(p => p.GrupoProdutoId == req.GrupoProdutoId);
        if (req.FabricanteId.HasValue) q = q.Where(p => p.FabricanteId == req.FabricanteId);
        if (req.SomenteSemFiscal)
            q = q.Where(p => p.Fiscais.All(f => f.NcmId == null));
        return q;
    }

    // ══ Jobs ═════════════════════════════════════════════════════════
    public async Task<List<GestorTributarioJobDto>> ListarJobsAsync(int limite = 50)
    {
        return await _db.GestorTributarioJobs
            .Include(j => j.Usuario)
            .OrderByDescending(j => j.Id)
            .Take(limite)
            .Select(j => new GestorTributarioJobDto
            {
                Id = j.Id,
                Tipo = (int)j.Tipo,
                TipoNome = j.Tipo.ToString(),
                Status = (int)j.Status,
                StatusNome = j.Status.ToString(),
                Provider = j.Provider,
                DataInicio = j.DataInicio,
                DataFim = j.DataFim,
                TotalItens = j.TotalItens,
                ItensProcessados = j.ItensProcessados,
                ItensAtualizados = j.ItensAtualizados,
                ItensNaoEncontrados = j.ItensNaoEncontrados,
                ItensComErro = j.ItensComErro,
                RequisicoesUsadas = j.RequisicoesUsadas,
                MensagemErro = j.MensagemErro,
                UsuarioNome = j.Usuario != null ? j.Usuario.Login : null,
                CriadoEm = j.CriadoEm
            }).ToListAsync();
    }

    public async Task<GestorTributarioJobDto?> ObterJobAsync(long jobId)
    {
        return (await ListarJobsAsync(1000)).FirstOrDefault(j => j.Id == jobId);
    }

    public async Task CancelarJobAsync(long jobId)
    {
        var job = await _db.GestorTributarioJobs.FindAsync(jobId)
            ?? throw new KeyNotFoundException($"Job {jobId} não encontrado.");
        if (job.Status != StatusJobGestorTributario.Executando
         && job.Status != StatusJobGestorTributario.Pendente)
            throw new InvalidOperationException("Só é possível cancelar jobs em execução/pendentes.");
        job.Status = StatusJobGestorTributario.Cancelado;
        job.DataFim = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ══ Rate limit + uso mensal ══════════════════════════════════════
    private async Task VerificarRateLimitOuFalharAsync()
    {
        var status = await ObterStatusAsync();
        if (!status.Ativo)
            throw new InvalidOperationException("Gestor Tributário não está configurado ou ativo.");
        if (status.PercentualUsado >= 100)
            throw new InvalidOperationException("Limite mensal de requisições atingido (100%).");
    }

    private async Task IncrementarUsoAsync(int qtde, string tipo)
    {
        await IncrementarUsoInternoAsync(_db, _provider.Nome, qtde, tipo);
    }

    private static async Task IncrementarUsoInternoAsync(AppDbContext db, string provider, int qtde, string tipo)
    {
        var agora = DateTime.UtcNow;
        var uso = await db.GestorTributarioUsoMensais
            .FirstOrDefaultAsync(u => u.Ano == agora.Year && u.Mes == agora.Month && u.Provider == provider);
        if (uso == null)
        {
            uso = new GestorTributarioUsoMensal
            {
                Ano = agora.Year,
                Mes = agora.Month,
                Provider = provider
            };
            db.GestorTributarioUsoMensais.Add(uso);
        }
        uso.RequisicoesUsadas += qtde;
        if (tipo == "revisao") uso.RequisicoesRevisao += qtde;
        else if (tipo == "atualizacao") uso.RequisicoesAtualizacao += qtde;
        else if (tipo == "difal") uso.RequisicoesDifal += qtde;
        uso.UltimaChamadaEm = agora;
        await db.SaveChangesAsync();
    }

    // ══ Monta DTO de revisão a partir de um Produto ══════════════════
    // A Avant exige todos os campos fiscais. Extraímos os valores atuais
    // do primeiro ProdutoFiscal preenchido (normalmente só há um por produto).
    private static ProdutoRevisaoDto MontarDtoRevisao(Produto produto)
    {
        // Pega o fiscal mais "preenchido" (com NCM, senão o primeiro)
        var fiscal = produto.Fiscais.FirstOrDefault(f => f.NcmId != null) ?? produto.Fiscais.FirstOrDefault();
        return new ProdutoRevisaoDto
        {
            ProdutoId = produto.Id,
            CodInterno = produto.Id.ToString(),
            Ean = produto.CodigoBarras ?? "",
            Descricao = produto.Nome,
            NcmAtual = fiscal?.Ncm?.CodigoNcm,
            CestAtual = fiscal?.Cest,
            CfopAtual = fiscal?.Cfop,
            CsosnAtual = fiscal?.Csosn,
            CstAtual = fiscal?.CstIcms,
            PIcmsAtual = fiscal?.AliquotaIcms,
            PFcpAtual = fiscal?.AliquotaFcp,
            CstPisAtual = fiscal?.CstPis,
            CstCofinsAtual = fiscal?.CstCofins
        };
    }

    // ══ Aplicar dados no ProdutoFiscal ═══════════════════════════════
    private void AplicarDadosFiscais(Produto produto, ProdutoFiscalExternoDto dto)
    {
        AplicarDadosFiscaisStatic(produto, dto, _provider.Nome);
    }

    private static void AplicarDadosFiscaisStatic(Produto produto, ProdutoFiscalExternoDto dto, string providerNome)
    {
        AplicarDadosFiscaisStaticComUf(produto, dto, providerNome, null);
    }

    /// <summary>
    /// Aplica os dados fiscais em todos os ProdutoFiscal do produto. Quando ufPorFilialId é fornecido,
    /// usa a alíquota de ICMS entrada específica do UF da filial (do icms_entrada.por_uf da Avant).
    /// </summary>
    private static void AplicarDadosFiscaisStaticComUf(Produto produto, ProdutoFiscalExternoDto dto, string providerNome, Dictionary<long, string>? ufPorFilialId)
    {
        var agora = DateTime.UtcNow;
        foreach (var f in produto.Fiscais)
        {
            f.Cfop = dto.Cfop ?? f.Cfop;
            f.Cest = dto.Cest ?? f.Cest;
            f.OrigemMercadoria = dto.Origem ?? f.OrigemMercadoria;

            // ICMS saída
            f.CstIcms = dto.CstIcms ?? f.CstIcms;
            f.Csosn = dto.Csosn ?? f.Csosn;
            f.AliquotaIcms = dto.AliquotaIcms;
            f.AliquotaFcp = dto.AliquotaFcp;
            f.ModBc = dto.ModBc ?? f.ModBc;
            f.PercentualReducaoBc = dto.PercentualReducaoBc;
            f.CodigoBeneficio = dto.CodigoBeneficio ?? f.CodigoBeneficio;
            f.DispositivoLegalIcms = dto.DispositivoLegalIcms ?? f.DispositivoLegalIcms;

            // ST
            f.TemSubstituicaoTributaria = dto.TemSubstituicaoTributaria;
            f.MvaOriginal = dto.MvaOriginal;
            f.MvaAjustado4 = dto.MvaAjustado4;
            f.MvaAjustado7 = dto.MvaAjustado7;
            f.MvaAjustado12 = dto.MvaAjustado12;
            f.AliquotaIcmsSt = dto.AliquotaIcmsSt;
            f.AliquotaFcpSt = dto.AliquotaFcpSt;

            // ICMS entrada por UF — usa o UF da filial especifica se fornecido
            if (ufPorFilialId != null && ufPorFilialId.TryGetValue(f.FilialId, out var uf) && dto.IcmsEntradaPorUf != null)
            {
                if (dto.IcmsEntradaPorUf.TryGetValue(uf, out var aliq))
                    f.AliquotaIcmsInternoEntrada = aliq;
            }

            // PIS
            f.CstPis = dto.CstPis ?? f.CstPis;
            f.AliquotaPis = dto.AliquotaPis;
            f.CstPisEntrada = dto.CstPisEntrada ?? f.CstPisEntrada;
            f.NaturezaReceita = dto.NaturezaReceita ?? f.NaturezaReceita;

            // COFINS
            f.CstCofins = dto.CstCofins ?? f.CstCofins;
            f.AliquotaCofins = dto.AliquotaCofins;
            f.CstCofinsEntrada = dto.CstCofinsEntrada ?? f.CstCofinsEntrada;

            // IPI
            f.CstIpi = dto.CstIpi ?? f.CstIpi;
            f.AliquotaIpi = dto.AliquotaIpi;
            f.EnquadramentoIpi = dto.EnquadramentoIpi ?? f.EnquadramentoIpi;
            f.CstIpiEntrada = dto.CstIpiEntrada ?? f.CstIpiEntrada;
            f.AliquotaIpiEntrada = dto.AliquotaIpiEntrada;
            f.AliquotaIpiIndustria = dto.AliquotaIpiIndustria;

            // Reforma Tributária 2026+
            f.CstIs = dto.CstIs ?? f.CstIs;
            f.ClassTribIs = dto.ClassTribIs ?? f.ClassTribIs;
            f.AliquotaIs = dto.AliquotaIs;
            f.CstIbsCbs = dto.CstIbsCbs ?? f.CstIbsCbs;
            f.ClassTribIbsCbs = dto.ClassTribIbsCbs ?? f.ClassTribIbsCbs;
            f.AliquotaIbsUf = dto.AliquotaIbsUf;
            f.AliquotaIbsMun = dto.AliquotaIbsMun;
            f.AliquotaCbs = dto.AliquotaCbs;

            f.AtualizadoGestorTributarioEm = agora;
            f.AtualizadoGestorTributarioProvider = providerNome;
        }
    }
}
