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
    private static readonly Dictionary<string, Type> _tabelasSyncaveis = new()
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
        ["GruposPrincipais"] = typeof(GrupoPrincipal),
        ["GruposProdutos"] = typeof(GrupoProduto),
        ["SubGrupos"] = typeof(SubGrupo),
        ["Secoes"] = typeof(Secao),
    };

    public SyncService(AppDbContext db) => _db = db;

    public static IReadOnlyList<string> TabelasSyncaveis => _tabelasSyncaveis.Keys.ToList();

    /// <summary>
    /// Gets records from a table that have changed since a given version.
    /// Used by: remote filial pulling changes from central server.
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
                    // New record - insert
                    _db.Add(entidade);
                    aplicados++;
                }
                else
                {
                    // Existing record - last-write-wins
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
