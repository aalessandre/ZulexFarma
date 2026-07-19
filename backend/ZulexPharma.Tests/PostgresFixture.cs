using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Tests;

internal static class TestBoot
{
    // Mesmo switch do Program.cs:9 — sem ele o harness DIVERGE do runtime e mente
    // (DateTime Unspecified estoura em timestamptz). Precisa rodar antes de qualquer uso do Npgsql.
    [ModuleInitializer]
    internal static void Init() => AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
}

/// <summary>
/// Fixture de POSTGRES REAL (EF InMemory e' PROIBIDO neste subsistema — nao reproduz sequences,
/// locks, constraints, isolamento nem ordem de commit; ver plano-correcao-replicacao fase 0).
/// Cria um banco descartavel 'zulexpharma_test' (drop + create + migrations) uma vez por run.
/// Conexao admin: env ERPPHARMA_TEST_PG ou, na ausencia, a connection string do
/// appsettings.Development.json do projeto API (gitignored, maquina de dev).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string NomeBancoTeste = "zulexpharma_test";
    public string ConnTeste { get; private set; } = "";

    public async Task InitializeAsync()
    {
        var admin = new NpgsqlConnectionStringBuilder(ResolverConnBase()) { Database = "postgres" };
        await using (var conn = new NpgsqlConnection(admin.ConnectionString))
        {
            await conn.OpenAsync();
            await Exec(conn, $"DROP DATABASE IF EXISTS {NomeBancoTeste} WITH (FORCE)");
            await Exec(conn, $"CREATE DATABASE {NomeBancoTeste}");
        }

        var teste = new NpgsqlConnectionStringBuilder(ResolverConnBase()) { Database = NomeBancoTeste };
        ConnTeste = teste.ConnectionString;

        await using var db = CriarContexto();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask; // banco fica pra inspecao post-mortem; o proximo run dropa

    /// <summary>
    /// AppDbContext apontando pro banco de teste. Config minima em memoria (No:Modo/No:Codigo) —
    /// mesmo shape que o runtime le. AplicandoSync=true = caminho do applicator (sem outbox).
    /// </summary>
    public AppDbContext CriarContexto(string modo = "Edge", int noCodigo = 1, bool aplicandoSync = false)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["No:Modo"] = modo,
            ["No:Codigo"] = noCodigo.ToString(),
        }).Build();
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnTeste).Options;
        return new AppDbContext(opts, null, config) { AplicandoSync = aplicandoSync };
    }

    public async Task<NpgsqlConnection> AbrirConexaoAsync()
    {
        var conn = new NpgsqlConnection(ConnTeste);
        await conn.OpenAsync();
        return conn;
    }

    public static async Task Exec(NpgsqlConnection conn, string sql, NpgsqlTransaction? tx = null)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        await cmd.ExecuteNonQueryAsync();
    }

    private static string ResolverConnBase()
    {
        var env = Environment.GetEnvironmentVariable("ERPPHARMA_TEST_PG");
        if (!string.IsNullOrWhiteSpace(env)) return env;

        // Fallback: appsettings.Development.json do API (gitignored) — sobe a arvore ate' achar backend/
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidato = Path.Combine(dir.FullName, "ZulexPharma.API", "appsettings.Development.json");
            if (File.Exists(candidato))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(candidato));
                var cs = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
                if (!string.IsNullOrWhiteSpace(cs)) return cs;
            }
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Sem conexao de teste: defina a env var ERPPHARMA_TEST_PG (connection string de um Postgres " +
            "com permissao de CREATE DATABASE) ou garanta o appsettings.Development.json do API com " +
            "ConnectionStrings:DefaultConnection.");
    }
}

[CollectionDefinition("pg")]
public class PgCollection : ICollectionFixture<PostgresFixture> { }
