using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using System.Text.Json;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>Resultado da aplicacao de uma operacao de sync (Fase 1 — corretude de conflito).</summary>
public enum ResultadoSync
{
    Aplicado,          // aplicado com sucesso
    Idempotente,       // ja' estava no estado alvo (reenvio/mesma versao) — nada a fazer
    Stale,             // LWW: a versao que chegou e' MAIS VELHA que a local — descartada de proposito
    PrecisaRetry,      // U cujo I ainda nao chegou (ou dependencia faltando) — reprocessar depois
    Conflito,          // chave unica de OUTRA origem (23505) sem merge automatico — quarentena
    TipoDesconhecido   // tabela fora do dicionario — quarentena/log (nunca silencioso)
}

/// <summary>
/// Lógica compartilhada para aplicar operações de sync no banco de dados.
/// Usada pelo SyncBackgroundService (filiais) e SyncController (Railway).
/// </summary>
public static class SyncApplicator
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Aplica uma operação (INSERT/UPDATE/DELETE) no banco de dados.
    /// Retorna true se aplicada com sucesso, false se ignorada (ex: já existe / não existe).
    /// Lança exceção em caso de erro.
    /// </summary>
    public static async Task<ResultadoSync> AplicarOperacaoAsync(
        AppDbContext db, string tabela, string operacao, long registroId,
        string? dadosJson, DateTime opCriadoEm, CancellationToken ct = default)
    {
        var tipo = ResolverTipo(tabela);
        if (tipo == null) return ResultadoSync.TipoDesconhecido;

        if (operacao == "D")
        {
            var existente = await BuscarPorId(db, tipo, registroId);
            if (existente == null) return ResultadoSync.Idempotente; // ja' nao existe
            // LWW: um UPDATE local MAIS NOVO que o DELETE vence (nao apaga).
            var localTs = existente.AtualizadoEm ?? existente.CriadoEm;
            if (localTs > opCriadoEm) return ResultadoSync.Stale;
            db.Remove(existente);
            await db.SaveChangesAsync(ct);
            return ResultadoSync.Aplicado;
        }

        if ((operacao == "I" || operacao == "U") && dadosJson != null)
        {
            var entidade = (BaseEntity?)JsonSerializer.Deserialize(dadosJson, tipo, _jsonOpts);
            if (entidade == null) return ResultadoSync.Conflito;

            var existentePorId = await BuscarPorId(db, tipo, registroId);
            if (existentePorId != null)
                return await AplicarUpdateComLww(db, existentePorId, entidade, ct); // reenvio/idempotencia via LWW

            if (operacao == "U") return ResultadoSync.PrecisaRetry; // UPDATE sem o INSERT ter chegado

            // INSERT novo
            LimparNavigations(db, entidade);
            db.Add(entidade);
            db.Entry(entidade).Property("Id").IsTemporary = false;
            try
            {
                await db.SaveChangesAsync(ct);
                return ResultadoSync.Aplicado;
            }
            catch (DbUpdateException ex) when (EhUniqueViolation(ex))
            {
                // 23505: chave (CPF/CNPJ/login) ja' existe. Tenta merge por SyncGuid (identidade estavel);
                // se for de OUTRA origem (SyncGuid diferente) -> quarentena (merge e' decisao de negocio).
                Desanexar(db);
                var porGuid = await BuscarPorSyncGuid(db, tipo, entidade.SyncGuid);
                if (porGuid != null) return await AplicarUpdateComLww(db, porGuid, entidade, ct);
                return ResultadoSync.Conflito;
            }
        }

        return ResultadoSync.Conflito;
    }

    /// <summary>UPDATE com Last-Writer-Wins por AtualizadoEm (desempate por NoOrigemId maior).</summary>
    private static async Task<ResultadoSync> AplicarUpdateComLww(AppDbContext db, BaseEntity existente, BaseEntity entidade, CancellationToken ct)
    {
        var incomingTs = entidade.AtualizadoEm ?? entidade.CriadoEm;
        var currentTs = existente.AtualizadoEm ?? existente.CriadoEm;
        if (incomingTs < currentTs) return ResultadoSync.Stale;
        if (incomingTs == currentTs && (entidade.NoOrigemId ?? 0) <= (existente.NoOrigemId ?? 0))
            return ResultadoSync.Idempotente; // empate: no maior vence; igual = ja' aplicado
        LimparNavigations(db, entidade);
        entidade.Id = existente.Id; // preserva a PK local (evita erro de mudar chave; Ids batem no caso normal)
        db.Entry(existente).CurrentValues.SetValues(entidade);
        await db.SaveChangesAsync(ct);
        return ResultadoSync.Aplicado;
    }

    private static bool EhUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;

    /// <summary>
    /// Solta do ChangeTracker o que sujou num apply que falhou, pra nao poluir o proximo — MAS
    /// preserva os registros de PAINEL (SyncFila do "recebido") e a propria SyncQuarentena, senao
    /// um erro numa op posterior do lote sumia com a evidencia das anteriores (nada pode ser silencioso).
    /// </summary>
    public static void Desanexar(AppDbContext db)
    {
        foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged).ToList())
        {
            if (entry.Entity is SyncFila or SyncQuarentena) continue;
            entry.State = EntityState.Detached;
        }
    }

    public const int MaxTentativasQuarentena = 5;
    // PrecisaRetry (U chegou antes do I) e' TRANSITORIO — resolve quando o I chega. Cap ALTO pra
    // sobreviver a um no de origem atrasado (nao o teto baixo de conflito genuino), senao o U ficaria
    // preso pra sempre e divergiria em silencio.
    public const int MaxTentativasReordenacao = 240;

    /// <summary>
    /// Coloca (upsert por Tabela+RegistroId+Operacao) uma op que NAO pode ser aplicada na quarentena,
    /// pra retry — em vez de perde-la avancando o ponteiro. Sobe a contagem de tentativas.
    /// </summary>
    public static async Task QuarentenarAsync(AppDbContext db, string tabela, string operacao, long registroId,
        string? dadosJson, DateTime opCriadoEm, long noOrigemId, string motivo, string? erro, CancellationToken ct = default)
    {
        var q = await db.SyncQuarentena.FirstOrDefaultAsync(
            x => x.Tabela == tabela && x.RegistroId == registroId && x.Operacao == operacao, ct);
        if (q == null)
        {
            db.SyncQuarentena.Add(new SyncQuarentena
            {
                Tabela = tabela, Operacao = operacao, RegistroId = registroId, DadosJson = dadosJson,
                OpCriadoEm = opCriadoEm, NoOrigemId = noOrigemId, Motivo = motivo, Tentativas = 1,
                UltimoErro = Truncar(erro, 1000), Resolvido = false
            });
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (EhUniqueViolation(ex))
            {
                // corrida: outro push inseriu a mesma (Tabela,RegistroId,Operacao) — recarrega e atualiza
                Desanexar(db);
                q = await db.SyncQuarentena.FirstOrDefaultAsync(
                    x => x.Tabela == tabela && x.RegistroId == registroId && x.Operacao == operacao, ct);
                if (q != null)
                {
                    AtualizarQuarentena(q, dadosJson, opCriadoEm, motivo, erro);
                    await db.SaveChangesAsync(ct);
                }
            }
        }
        else
        {
            AtualizarQuarentena(q, dadosJson, opCriadoEm, motivo, erro);
            await db.SaveChangesAsync(ct);
        }
        Log.Warning("Sync quarentena [{Motivo}] {Tabela}/{Op} Id={Id}: {Erro}", motivo, tabela, operacao, registroId, erro ?? motivo);
    }

    private static void AtualizarQuarentena(SyncQuarentena q, string? dadosJson, DateTime opCriadoEm, string motivo, string? erro)
    {
        q.DadosJson = dadosJson;
        q.OpCriadoEm = opCriadoEm;
        q.Motivo = motivo;
        q.Tentativas += 1;
        q.UltimoErro = Truncar(erro, 1000);
        q.Resolvido = false;
        q.AtualizadoEm = Domain.Helpers.DataHoraHelper.Agora();
    }

    /// <summary>
    /// Reprocessa itens da quarentena (retry) ate' sucesso (Resolvido) ou o teto de tentativas.
    /// Chamada a cada ciclo — resolve o caso "U chegou antes do I" quando o I finalmente chega.
    /// Retorna quantos foram resolvidos.
    /// </summary>
    public static async Task<int> DrenarQuarentenaAsync(AppDbContext db, CancellationToken ct = default)
    {
        var pendentes = await db.SyncQuarentena.AsNoTracking()
            .Where(q => !q.Resolvido && (
                (q.Motivo == "PrecisaRetry" && q.Tentativas < MaxTentativasReordenacao) ||
                (q.Motivo != "PrecisaRetry" && q.Tentativas < MaxTentativasQuarentena)))
            .OrderBy(q => q.Id).Take(200).ToListAsync(ct);
        if (pendentes.Count == 0) return 0;

        var resolvidos = 0;
        foreach (var q in pendentes)
        {
            ResultadoSync res;
            string? erro = null;
            try
            {
                res = await AplicarOperacaoAsync(db, q.Tabela, q.Operacao, q.RegistroId, q.DadosJson, q.OpCriadoEm, ct);
            }
            catch (Exception ex)
            {
                Desanexar(db);
                res = ResultadoSync.Conflito;
                erro = ex.Message;
            }

            var ok = res is ResultadoSync.Aplicado or ResultadoSync.Idempotente or ResultadoSync.Stale;
            if (ok) resolvidos++;
            // ExecuteUpdate (sem tracking) pra nao misturar com as entidades aplicadas no tracker.
            await db.SyncQuarentena.Where(x => x.Id == q.Id).ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Resolvido, ok)
                .SetProperty(x => x.Tentativas, x => x.Tentativas + (ok ? 0 : 1))
                .SetProperty(x => x.Motivo, res.ToString())
                .SetProperty(x => x.UltimoErro, Truncar(erro, 1000))
                .SetProperty(x => x.AtualizadoEm, Domain.Helpers.DataHoraHelper.Agora()), ct);
        }
        return resolvidos;
    }

    private static string? Truncar(string? s, int max) => s == null || s.Length <= max ? s : s.Substring(0, max);

    /// <summary>Busca por SyncGuid (identidade estavel cross-no) via reflexao no DbSet.</summary>
    public static async Task<BaseEntity?> BuscarPorSyncGuid(AppDbContext db, Type tipo, Guid syncGuid)
    {
        if (syncGuid == Guid.Empty) return null;
        var method = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!.MakeGenericMethod(tipo);
        var dbSet = (IQueryable<BaseEntity>)method.Invoke(db, null)!;
        return await dbSet.FirstOrDefaultAsync(e => e.SyncGuid == syncGuid);
    }

    /// <summary>
    /// Resolve tipo CLR a partir do nome da tabela.
    /// </summary>
    public static Type? ResolverTipo(string tabela) => _tiposPorTabela.GetValueOrDefault(tabela);

    /// <summary>
    /// Busca entidade por Id usando reflexão no DbSet correto.
    /// </summary>
    public static async Task<BaseEntity?> BuscarPorId(AppDbContext db, Type tipo, long id)
    {
        if (id <= 0) return null;
        var method = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!.MakeGenericMethod(tipo);
        var dbSet = (IQueryable<BaseEntity>)method.Invoke(db, null)!;
        return await dbSet.FirstOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>
    /// Anula todas as navigation properties para evitar tracking de grafo.
    /// </summary>
    public static void LimparNavigations(AppDbContext db, object entidade)
    {
        var entityType = db.Model.FindEntityType(entidade.GetType());
        if (entityType == null) return;

        foreach (var nav in entityType.GetNavigations())
        {
            var prop = nav.PropertyInfo;
            if (prop != null && prop.CanWrite)
                prop.SetValue(entidade, null);
        }

        foreach (var nav in entityType.GetSkipNavigations())
        {
            var prop = nav.PropertyInfo;
            if (prop != null && prop.CanWrite)
                prop.SetValue(entidade, null);
        }
    }

    /// <summary>
    /// Prioridades de dependência para ordenação no sync.
    /// INSERT/UPDATE: menor número primeiro (pais antes de filhos).
    /// DELETE: maior número primeiro (filhos antes de pais).
    /// </summary>
    public static int GetOrdemTabela(string tabela) => tabela switch
    {
        // Nível 0 — sem dependência
        "Filiais"                  => 0,
        "UsuariosGrupos"           => 0,
        "Ncms"                     => 0,

        // Nível 1 — depende apenas de nível 0
        "Pessoas"                  => 1,
        "UsuariosGruposPermissao"  => 1,
        "NcmFederais"              => 1,
        "NcmIcmsUfs"               => 1,
        "NcmStUfs"                 => 1,
        "Fabricantes"              => 1,
        "Prescritores"             => 1,
        "PlanosContas"             => 1,
        "TiposPagamento"           => 1,
        "Convenios"                => 2,
        "Clientes"                 => 2,
        "HierarquiaDescontos"      => 3,
        "Vendas"                   => 4,
        "VendaFiscais"             => 5,
        "VendaItemFiscais"         => 6,
        "VendaReceitas"            => 5,
        "VendaReceitaItens"        => 6,
        "EntregaPerfis"            => 2,
        "EntregaFaixas"            => 3,
        "EntregaAgendas"           => 3,
        "Feriados"                 => 2,
        "Entregas"                 => 5,
        "EntregaEventos"           => 6,
        "CampanhasFidelidade"       => 3,
        "CampanhasFidelidadeItens"  => 4,
        "PremiosFidelidade"         => 1,
        "Promocoes"                => 2,
        "ContasBancarias"          => 2,
        "ContasPagar"              => 3,
        "Substancias"              => 1,
        "GruposPrincipais"         => 1,
        "GruposProdutos"           => 1,
        "SubGrupos"                => 1,
        "Secoes"                   => 1,
        "ProdutoFamilias"          => 1,
        "ProdutosLocais"           => 1,

        // Nível 2 — depende de nível 1 (Pessoa → Colaborador/Fornecedor, classificações → Produto)
        "Colaboradores"            => 2,
        "Fornecedores"             => 2,
        "PessoasContato"           => 2,
        "PessoasEndereco"          => 2,
        "Produtos"                 => 2,

        // Nível 3 — depende de nível 2 (Produto → sub-tabelas, Colaborador → Usuário)
        "Usuarios"                 => 3,
        "ProdutosBarras"           => 3,
        "ProdutosMs"               => 3,
        "ProdutosSubstancias"      => 3,
        "ProdutosFornecedores"     => 3,
        "ProdutosFiscal"           => 3,
        "ProdutosDados"            => 3,

        // Nível 3 — Compras (depende de Fornecedores nível 2)
        "Compras"                  => 3,

        // Nível 4 — depende de nível 3
        "ComprasProdutos"          => 4,
        "UsuarioFilialGrupos"      => 4,

        // Nível 5 — depende de nível 4 (CompraFiscal depende de ComprasProdutos)
        "ComprasFiscal"            => 5,

        // Nível 0 — referência
        "IcmsUfs"                  => 0,
        "AtualizacoesPreco"        => 3,
        "AtualizacoesPrecoItens"   => 4,

        // Nível 0 — configurações (sem dependência)
        "Configuracoes"            => 0,

        // Tabelas locais (não replicam, mas listadas por completude)
        "LogsAcao"                 => 5,
        "LogsErro"                 => 5,

        // Fallback — tabelas desconhecidas por último
        _ => 10
    };

    private static readonly Dictionary<string, Type> _tiposPorTabela = new()
    {
        ["Filiais"] = typeof(Filial),
        ["Pessoas"] = typeof(Pessoa),
        ["PessoasContato"] = typeof(PessoaContato),
        ["PessoasEndereco"] = typeof(PessoaEndereco),
        ["Colaboradores"] = typeof(Colaborador),
        ["Fornecedores"] = typeof(Fornecedor),
        ["Usuarios"] = typeof(Usuario),
        ["UsuariosGrupos"] = typeof(GrupoUsuario),
        ["UsuariosGruposPermissao"] = typeof(GrupoPermissao),
        ["UsuarioFilialGrupos"] = typeof(UsuarioFilialGrupo),
        ["Fabricantes"] = typeof(Fabricante),
        ["Prescritores"] = typeof(Prescritor),
        ["PlanosContas"] = typeof(PlanoConta),
        ["TiposPagamento"] = typeof(TipoPagamento),
        ["Convenios"] = typeof(Convenio),
        ["Clientes"] = typeof(Cliente),
        ["HierarquiaDescontos"] = typeof(HierarquiaDesconto),
        ["Vendas"] = typeof(Venda),
        ["VendaFiscais"] = typeof(VendaFiscal),
        ["VendaItemFiscais"] = typeof(VendaItemFiscal),
        ["VendaReceitas"] = typeof(VendaReceita),
        ["VendaReceitaItens"] = typeof(VendaReceitaItem),
        ["EntregaPerfis"] = typeof(EntregaPerfil),
        ["EntregaFaixas"] = typeof(EntregaFaixa),
        ["EntregaAgendas"] = typeof(EntregaAgenda),
        ["Feriados"] = typeof(Feriado),
        ["Entregas"] = typeof(Entrega),
        ["EntregaEventos"] = typeof(EntregaEvento),
        ["CampanhasFidelidade"] = typeof(CampanhaFidelidade),
        ["CampanhasFidelidadeItens"] = typeof(CampanhaFidelidadeItem),
        ["PremiosFidelidade"] = typeof(PremioFidelidade),
        ["Promocoes"] = typeof(Promocao),
        ["ContasBancarias"] = typeof(ContaBancaria),
        ["ContasPagar"] = typeof(ContaPagar),
        ["Substancias"] = typeof(Substancia),
        ["GruposPrincipais"] = typeof(GrupoPrincipal),
        ["GruposProdutos"] = typeof(GrupoProduto),
        ["SubGrupos"] = typeof(SubGrupo),
        ["Secoes"] = typeof(Secao),
        ["ProdutoFamilias"] = typeof(ProdutoFamilia),
        ["ProdutosLocais"] = typeof(ProdutoLocal),
        ["Produtos"] = typeof(Produto),
        ["ProdutosBarras"] = typeof(ProdutoBarras),
        ["ProdutosMs"] = typeof(ProdutoMs),
        ["ProdutosSubstancias"] = typeof(ProdutoSubstancia),
        ["ProdutosFornecedores"] = typeof(ProdutoFornecedor),
        ["ProdutosFiscal"] = typeof(ProdutoFiscal),
        ["ProdutosDados"] = typeof(ProdutoDados),
        ["Ncms"] = typeof(Ncm),
        ["NcmFederais"] = typeof(NcmFederal),
        ["NcmIcmsUfs"] = typeof(NcmIcmsUf),
        ["NcmStUfs"] = typeof(NcmStUf),
        ["Configuracoes"] = typeof(Configuracao),
        ["IcmsUfs"] = typeof(IcmsUf),
        ["AtualizacoesPreco"] = typeof(AtualizacaoPreco),
        ["AtualizacoesPrecoItens"] = typeof(AtualizacaoPrecoItem),
        ["Compras"] = typeof(Compra),
        ["ComprasProdutos"] = typeof(CompraProduto),
        ["ComprasFiscal"] = typeof(CompraFiscal),
        ["LogsAcao"] = typeof(LogAcao),
        ["LogsErro"] = typeof(LogErro),
    };
}
