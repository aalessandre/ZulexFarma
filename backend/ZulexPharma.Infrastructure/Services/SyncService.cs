using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class SyncService
{
    private readonly AppDbContext _db;

    // Map of sync-able table names to their DbSet accessors
    // ORDEM IMPORTA: tabelas pai antes das filhas (respeitar FKs)
    // 1. Tabelas sem FK externa
    // 2. Tabelas que outras referenciam
    // 3. Tabelas dependentes
    private static readonly Dictionary<string, Type> _tabelasSyncaveis = new()
    {
        // Nível 1: sem dependências
        ["Filiais"] = typeof(Filial),
        ["Fabricantes"] = typeof(Fabricante),
        ["Substancias"] = typeof(Substancia),
        ["GruposPrincipais"] = typeof(GrupoPrincipal),
        ["GruposProdutos"] = typeof(GrupoProduto),
        ["SubGrupos"] = typeof(SubGrupo),
        ["Secoes"] = typeof(Secao),
        ["UsuariosGrupos"] = typeof(GrupoUsuario),
        // Nível 2: dependem de Filiais e/ou UsuariosGrupos
        ["UsuariosGruposPermissao"] = typeof(GrupoPermissao),
        ["Pessoas"] = typeof(Pessoa),
        // Nível 3: dependem de Pessoas
        ["PessoasContato"] = typeof(PessoaContato),
        ["PessoasEndereco"] = typeof(PessoaEndereco),
        ["Colaboradores"] = typeof(Colaborador),
        ["Fornecedores"] = typeof(Fornecedor),
        // Nível 4: dependem de Colaboradores + Filiais + UsuariosGrupos
        ["Usuarios"] = typeof(Usuario),
        // Nível 5: dependem de Usuarios + Filiais + UsuariosGrupos
        ["UsuarioFilialGrupos"] = typeof(UsuarioFilialGrupo),
    };

    public SyncService(AppDbContext db) => _db = db;

    public static IReadOnlyList<string> TabelasSyncaveis => _tabelasSyncaveis.Keys.ToList();

    /// <summary>
    /// Gets records from a table that have changed since a given version.
    /// Used by: central server sending changes to a filial (PULL).
    /// Excludes records that originated from the requesting filial (to avoid echo).
    /// </summary>
    public async Task<SyncPacote> ObterAlteracoes(string tabela, long versaoDesde, long? filialId = null, int limite = 500)
    {
        if (!_tabelasSyncaveis.ContainsKey(tabela))
            throw new ArgumentException($"Tabela '{tabela}' não é sincronizável.");

        var tipo = _tabelasSyncaveis[tabela];
        var dbSet = GetDbSetAsQueryable(tipo);

        IQueryable<BaseEntity> query = dbSet.Where(e => e.VersaoSync > versaoDesde);

        if (filialId.HasValue && filialId > 0)
            query = query.Where(e => e.FilialOrigemId == null || e.FilialOrigemId != filialId);

        var registros = await query
            .OrderBy(e => e.VersaoSync)
            .Take(limite)
            .ToListAsync();

        var maxVersao = registros.Count > 0 ? registros.Max(e => e.VersaoSync) : versaoDesde;

        return new SyncPacote
        {
            Tabela = tabela,
            VersaoDesde = versaoDesde,
            VersaoAte = maxVersao,
            TotalRegistros = registros.Count,
            Registros = registros.Select(r => JsonSerializer.Serialize(r, r.GetType(), new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            })).ToList()
        };
    }

    /// <summary>
    /// Gets LOCAL records from a table that originated from a specific filial.
    /// Used by: local background service to PUSH only this filial's changes to central.
    /// </summary>
    public async Task<SyncPacote> ObterAlteracoesLocais(string tabela, long versaoDesde, long filialOrigemId, int limite = 500)
    {
        if (!_tabelasSyncaveis.ContainsKey(tabela))
            throw new ArgumentException($"Tabela '{tabela}' não é sincronizável.");

        var tipo = _tabelasSyncaveis[tabela];
        var dbSet = GetDbSetAsQueryable(tipo);

        // Only get records that originated from THIS filial
        var registros = await dbSet
            .Where(e => e.VersaoSync > versaoDesde && e.FilialOrigemId == filialOrigemId)
            .OrderBy(e => e.VersaoSync)
            .Take(limite)
            .ToListAsync();

        var maxVersao = registros.Count > 0 ? registros.Max(e => e.VersaoSync) : versaoDesde;

        return new SyncPacote
        {
            Tabela = tabela,
            VersaoDesde = versaoDesde,
            VersaoAte = maxVersao,
            TotalRegistros = registros.Count,
            Registros = registros.Select(r => JsonSerializer.Serialize(r, r.GetType(), new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            })).ToList()
        };
    }

    /// <summary>
    /// Receives records from a remote filial and applies them to the local database.
    /// Uses last-write-wins conflict resolution based on AtualizadoEm.
    /// </summary>
    public async Task<SyncResultado> AplicarAlteracoes(string tabela, List<string> registrosJson)
    {
        if (!_tabelasSyncaveis.ContainsKey(tabela))
            throw new ArgumentException($"Tabela '{tabela}' não é sincronizável.");

        var tipo = _tabelasSyncaveis[tabela];
        var aplicados = 0;
        var conflitos = 0;
        var erros = 0;

        foreach (var json in registrosJson)
        {
            try
            {
                var entidade = (BaseEntity?)JsonSerializer.Deserialize(json, tipo, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (entidade == null) { erros++; continue; }

                var existente = await _db.FindAsync(tipo, entidade.Id) as BaseEntity;

                if (existente == null)
                {
                    // Check unique fields before inserting
                    if (await TemDuplicataUnica(tabela, tipo, entidade))
                    {
                        conflitos++;
                        Log.Warning("Sync: registro duplicado por campo unico na tabela {Tabela}, Id={Id}", tabela, entidade.Id);
                        continue;
                    }
                    // New record - insert
                    _db.Add(entidade);
                    aplicados++;
                }
                else if (existente.FilialOrigemId != entidade.FilialOrigemId
                         && existente.FilialOrigemId != null && entidade.FilialOrigemId != null)
                {
                    // Same Id but DIFFERENT filial — these are different records!
                    // Insert as new record with auto-generated Id
                    entidade.Id = 0; // Reset Id so EF generates a new one
                    if (await TemDuplicataUnica(tabela, tipo, entidade))
                    {
                        conflitos++;
                        continue;
                    }
                    _db.Add(entidade);
                    aplicados++;
                    Log.Information("Sync: registro Id conflitante em {Tabela}, inserido com novo Id (FilialOrigem local={Local} remoto={Remoto})",
                        tabela, existente.FilialOrigemId, entidade.FilialOrigemId);
                }
                else
                {
                    // Same record, same filial - last-write-wins
                    if (entidade.AtualizadoEm >= existente.AtualizadoEm || existente.AtualizadoEm == null)
                    {
                        _db.Entry(existente).CurrentValues.SetValues(entidade);
                        aplicados++;
                    }
                    else
                    {
                        conflitos++; // Local is newer, skip
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Erro ao aplicar registro sync na tabela {Tabela}", tabela);
                erros++;
            }
        }

        await _db.SaveChangesAsync();

        return new SyncResultado
        {
            Tabela = tabela,
            Aplicados = aplicados,
            Conflitos = conflitos,
            Erros = erros
        };
    }

    /// <summary>
    /// Gets the sync status for all tables for a given filial.
    /// </summary>
    public async Task<List<SyncStatus>> ObterStatus(long filialId)
    {
        var controles = await _db.SyncControles
            .Where(s => s.FilialId == filialId)
            .ToListAsync();

        var result = new List<SyncStatus>();
        foreach (var tabela in _tabelasSyncaveis.Keys)
        {
            var controle = controles.FirstOrDefault(c => c.Tabela == tabela);

            // Count pending changes
            var tipo = _tabelasSyncaveis[tabela];
            var dbSet = GetDbSetAsQueryable(tipo);
            var ultimaVersao = controle?.UltimaVersaoEnviada ?? 0;
            var pendentes = await dbSet.CountAsync(e => e.VersaoSync > ultimaVersao &&
                (e.FilialOrigemId == filialId || e.FilialOrigemId == null));

            result.Add(new SyncStatus
            {
                Tabela = tabela,
                UltimaVersaoEnviada = controle?.UltimaVersaoEnviada ?? 0,
                UltimaVersaoRecebida = controle?.UltimaVersaoRecebida ?? 0,
                UltimoSync = controle?.UltimoSync,
                Status = controle?.Status ?? "NUNCA",
                PendentesEnvio = pendentes
            });
        }

        return result;
    }

    /// <summary>
    /// Updates the sync control record after a successful sync.
    /// </summary>
    public async Task AtualizarControle(long filialId, string tabela, long? versaoEnviada = null, long? versaoRecebida = null, string status = "OK", string? erro = null)
    {
        var controle = await _db.SyncControles
            .FirstOrDefaultAsync(s => s.FilialId == filialId && s.Tabela == tabela);

        if (controle == null)
        {
            controle = new SyncControle
            {
                FilialId = filialId,
                Tabela = tabela
            };
            _db.SyncControles.Add(controle);
        }

        if (versaoEnviada.HasValue) controle.UltimaVersaoEnviada = versaoEnviada.Value;
        if (versaoRecebida.HasValue) controle.UltimaVersaoRecebida = versaoRecebida.Value;
        controle.UltimoSync = DateTime.UtcNow;
        controle.Status = status;
        controle.MensagemErro = erro;

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Resets the PostgreSQL auto-increment sequence for a table to MAX(Id)+1.
    /// Must be called after inserting records with explicit IDs (from sync).
    /// </summary>
    public async Task ResetarSequence(string tabela)
    {
        try
        {
            // PostgreSQL sequence name convention: "TableName_Id_seq"
            var sql = $"SELECT setval(pg_get_serial_sequence('\"{tabela}\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"{tabela}\"), 0) + 1, false)";
            await _db.Database.ExecuteSqlRawAsync(sql);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Erro ao resetar sequence da tabela {Tabela}", tabela);
        }
    }

    /// <summary>
    /// Verifica se um registro remoto tem duplicata por campo unico (CPF, CNPJ, Login, etc).
    /// Le os campos marcados como unicos no Dicionario de Dados.
    /// </summary>
    private async Task<bool> TemDuplicataUnica(string tabela, Type tipo, BaseEntity entidade)
    {
        try
        {
            // Get unique fields from DD
            var camposUnicos = await _db.DicionarioRevisoes
                .Where(r => r.Tabela == tabela && r.Unico == true)
                .Select(r => r.Coluna)
                .ToListAsync();

            if (camposUnicos.Count == 0) return false;

            foreach (var campo in camposUnicos)
            {
                if (campo == "Id") continue; // PK already handled

                var prop = tipo.GetProperty(campo);
                if (prop == null) continue;

                var valor = prop.GetValue(entidade);
                if (valor == null || (valor is string s && string.IsNullOrWhiteSpace(s))) continue;

                // Efficient database-level duplicate check using raw SQL
                var valorStr = valor.ToString()!.Replace("'", "''");
                var sql = $"SELECT EXISTS(SELECT 1 FROM \"{tabela}\" WHERE \"Id\" != {entidade.Id} AND LOWER(CAST(\"{campo}\" AS TEXT)) = LOWER('{valorStr}'))";
                var conn = _db.Database.GetDbConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                if (result is bool b && b) return true;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Erro ao verificar duplicatas unicas em {Tabela}", tabela);
        }

        return false;
    }

    private IQueryable<BaseEntity> GetDbSetAsQueryable(Type tipo)
    {
        // Use reflection to get the DbSet for the given type
        var method = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!.MakeGenericMethod(tipo);
        var dbSet = method.Invoke(_db, null)!;
        return (IQueryable<BaseEntity>)dbSet;
    }
}

// DTOs for sync operations
public class SyncPacote
{
    public string Tabela { get; set; } = string.Empty;
    public long VersaoDesde { get; set; }
    public long VersaoAte { get; set; }
    public int TotalRegistros { get; set; }
    public bool TemMaisDados { get; set; }
    public List<string> Registros { get; set; } = new();
}

public class SyncResultado
{
    public string Tabela { get; set; } = string.Empty;
    public int Aplicados { get; set; }
    public int Conflitos { get; set; }
    public int Erros { get; set; }
}

public class SyncStatus
{
    public string Tabela { get; set; } = string.Empty;
    public long UltimaVersaoEnviada { get; set; }
    public long UltimaVersaoRecebida { get; set; }
    public DateTime? UltimoSync { get; set; }
    public string Status { get; set; } = string.Empty;
    public int PendentesEnvio { get; set; }
}
