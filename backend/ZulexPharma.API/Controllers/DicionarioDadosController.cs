using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

[Authorize]
[ApiController]
[Route("api/dicionario-dados")]
public class DicionarioDadosController : ControllerBase
{
    private readonly AppDbContext _db;

    public DicionarioDadosController(AppDbContext db) => _db = db;

    /// <summary>
    /// Retorna todas as tabelas e colunas do banco com metadados do PostgreSQL.
    /// </summary>
    [HttpGet("estrutura")]
    public async Task<IActionResult> ObterEstrutura()
    {
        try
        {
            var sql = @"
                SELECT
                    t.table_name AS tabela,
                    c.column_name AS coluna,
                    c.data_type AS tipo,
                    c.character_maximum_length AS tamanho,
                    CASE WHEN c.is_nullable = 'NO' THEN true ELSE false END AS obrigatorio,
                    c.column_default AS valor_padrao,
                    c.ordinal_position AS ordem
                FROM information_schema.tables t
                JOIN information_schema.columns c ON c.table_name = t.table_name AND c.table_schema = t.table_schema
                WHERE t.table_schema = 'public'
                  AND t.table_type = 'BASE TABLE'
                  AND t.table_name NOT IN ('__EFMigrationsHistory')
                ORDER BY t.table_name, c.ordinal_position";

            var resultado = new List<TabelaInfo>();
            var conn = _db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            using var reader = await cmd.ExecuteReaderAsync();

            var tabelaAtual = "";
            TabelaInfo? tabela = null;

            while (await reader.ReadAsync())
            {
                var nomeTabela = reader.GetString(0);
                if (nomeTabela != tabelaAtual)
                {
                    tabela = new TabelaInfo { Nome = nomeTabela };
                    resultado.Add(tabela);
                    tabelaAtual = nomeTabela;
                }

                tabela!.Colunas.Add(new ColunaInfo
                {
                    Nome = reader.GetString(1),
                    Tipo = reader.GetString(2),
                    Tamanho = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Obrigatorio = reader.GetBoolean(4),
                    ValorPadrao = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Ordem = reader.GetInt32(6)
                });
            }

            // Get unique constraints
            var sqlUnique = @"
                SELECT
                    tc.table_name AS tabela,
                    kcu.column_name AS coluna
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                    ON tc.constraint_name = kcu.constraint_name
                    AND tc.table_schema = kcu.table_schema
                WHERE tc.constraint_type IN ('UNIQUE', 'PRIMARY KEY')
                  AND tc.table_schema = 'public'";

            using var cmd2 = conn.CreateCommand();
            cmd2.CommandText = sqlUnique;
            using var reader2 = await cmd2.ExecuteReaderAsync();

            var unicos = new HashSet<string>();
            while (await reader2.ReadAsync())
            {
                unicos.Add($"{reader2.GetString(0)}.{reader2.GetString(1)}");
            }

            foreach (var t in resultado)
                foreach (var c in t.Colunas)
                    c.Unico = unicos.Contains($"{t.Nome}.{c.Nome}");

            // Load saved reviews
            var revisoes = await _db.DicionarioRevisoes.ToListAsync();
            foreach (var t in resultado)
                foreach (var c in t.Colunas)
                {
                    var rev = revisoes.FirstOrDefault(r => r.Tabela == t.Nome && r.Coluna == c.Nome);
                    if (rev != null)
                    {
                        c.Revisado = rev.Revisado;
                        c.Observacao = rev.Observacao;
                        c.UnicoCustom = rev.Unico;
                        c.ObrigatorioCustom = rev.Obrigatorio;
                        c.Replica = rev.Replica;
                    }
                }

            return Ok(new { success = true, data = resultado });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao obter estrutura do banco");
            return StatusCode(500, new { success = false, message = "Erro ao obter estrutura." });
        }
    }

    /// <summary>
    /// Salva a revisão de um campo (marcação de revisado, observação, etc).
    /// </summary>
    [HttpPost("revisar")]
    public async Task<IActionResult> Revisar([FromBody] RevisaoCampoDto dto)
    {
        try
        {
            var existente = await _db.DicionarioRevisoes
                .FirstOrDefaultAsync(r => r.Tabela == dto.Tabela && r.Coluna == dto.Coluna);

            if (existente == null)
            {
                existente = new Domain.Entities.DicionarioRevisao
                {
                    Tabela = dto.Tabela,
                    Coluna = dto.Coluna
                };
                _db.DicionarioRevisoes.Add(existente);
            }

            existente.Revisado = dto.Revisado;
            existente.Observacao = dto.Observacao;
            existente.Unico = dto.Unico;
            existente.Obrigatorio = dto.Obrigatorio;
            existente.Replica = dto.Replica;
            existente.RevisadoEm = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao salvar revisão");
            return StatusCode(500, new { success = false, message = "Erro ao salvar." });
        }
    }
}

// DTOs
public class TabelaInfo
{
    public string Nome { get; set; } = "";
    public List<ColunaInfo> Colunas { get; set; } = new();
}

public class ColunaInfo
{
    public string Nome { get; set; } = "";
    public string Tipo { get; set; } = "";
    public int? Tamanho { get; set; }
    public bool Obrigatorio { get; set; }
    public bool Unico { get; set; }
    public string? ValorPadrao { get; set; }
    public int Ordem { get; set; }
    // Custom review fields
    public bool Revisado { get; set; }
    public string? Observacao { get; set; }
    public bool? UnicoCustom { get; set; }
    public bool? ObrigatorioCustom { get; set; }
    public bool? Replica { get; set; }
}

public record RevisaoCampoDto(
    string Tabela,
    string Coluna,
    bool Revisado,
    string? Observacao,
    bool? Unico,
    bool? Obrigatorio,
    bool? Replica
);
