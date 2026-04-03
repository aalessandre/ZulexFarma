using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

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
    public static async Task<bool> AplicarOperacaoAsync(
        AppDbContext db, string tabela, string operacao, long registroId,
        string? dadosJson, CancellationToken ct = default)
    {
        var tipo = ResolverTipo(tabela);
        if (tipo == null) return false;

        if (operacao == "D")
        {
            var existente = await BuscarPorId(db, tipo, registroId);
            if (existente == null) return false;
            db.Remove(existente);
            await db.SaveChangesAsync(ct);
            return true;
        }

        if (operacao == "I" && dadosJson != null)
        {
            var existente = await BuscarPorId(db, tipo, registroId);
            if (existente != null) return false; // Já existe por Id, skip (idempotência)

            // Usar raw SQL com ON CONFLICT DO NOTHING para evitar erros de unique constraint.
            // Seeds idênticos (Configuracoes, Usuarios, Filiais) criam registros com mesma Chave/Login/CNPJ
            // mas IDs diferentes em cada filial — ON CONFLICT pula silenciosamente.
            var entidade = (BaseEntity?)JsonSerializer.Deserialize(dadosJson, tipo, _jsonOpts);
            if (entidade == null) return false;

            var inserted = await InsertOnConflictDoNothing(db, tipo, entidade, ct);
            if (!inserted)
            {
                Log.Debug("Sync INSERT ignorado: {Tabela} Id={Id} — registro com mesma constraint já existe localmente.", tabela, registroId);
            }
            return true; // Sempre avançar ponteiro
        }

        if (operacao == "U" && dadosJson != null)
        {
            var existente = await BuscarPorId(db, tipo, registroId);
            if (existente == null) return false; // Não existe, skip
            var entidade = (BaseEntity?)JsonSerializer.Deserialize(dadosJson, tipo, _jsonOpts);
            if (entidade == null) return false;
            LimparNavigations(db, entidade);
            db.Entry(existente).CurrentValues.SetValues(entidade);
            await db.SaveChangesAsync(ct);
            return true;
        }

        return false;
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
    /// Busca entidade por SyncGuid (útil para resolver conflitos de unique constraint).
    /// </summary>
    public static async Task<BaseEntity?> BuscarPorSyncGuid(AppDbContext db, Type tipo, Guid syncGuid)
    {
        if (syncGuid == Guid.Empty) return null;
        var method = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!.MakeGenericMethod(tipo);
        var dbSet = (IQueryable<BaseEntity>)method.Invoke(db, null)!;
        return await dbSet.FirstOrDefaultAsync(e => e.SyncGuid == syncGuid);
    }

    /// <summary>
    /// Insere via raw SQL com ON CONFLICT DO NOTHING (PostgreSQL).
    /// Retorna true se inseriu, false se conflitou e foi ignorado.
    /// </summary>
    private static async Task<bool> InsertOnConflictDoNothing(AppDbContext db, Type tipo, BaseEntity entidade, CancellationToken ct)
    {
        var entityType = db.Model.FindEntityType(tipo);
        if (entityType == null) return false;

        var tableName = entityType.GetTableName()!;
        var properties = entityType.GetProperties()
            .Where(p => !p.IsShadowProperty() && p.PropertyInfo != null)
            .ToList();

        var columns = new List<string>();
        var paramPlaceholders = new List<string>();
        var paramValues = new List<object?>();
        var idx = 0;

        foreach (var prop in properties)
        {
            var columnName = prop.GetColumnName();
            var value = prop.PropertyInfo!.GetValue(entidade);

            columns.Add($"\"{columnName}\"");
            paramPlaceholders.Add($"@p{idx}");
            paramValues.Add(value ?? DBNull.Value);
            idx++;
        }

        var sql = $"INSERT INTO \"{tableName}\" ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramPlaceholders)}) ON CONFLICT DO NOTHING";

        var parameters = paramValues.Select((v, i) =>
        {
            var param = new Npgsql.NpgsqlParameter($"@p{i}", v ?? DBNull.Value);
            return (object)param;
        }).ToArray();

        var rows = await db.Database.ExecuteSqlRawAsync(sql, parameters, ct);
        return rows > 0;
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

        // Nível 4 — depende de nível 3
        "UsuarioFilialGrupos"      => 4,

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
        ["LogsAcao"] = typeof(LogAcao),
        ["LogsErro"] = typeof(LogErro),
    };
}
