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
    private readonly IWebHostEnvironment _env;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public DicionarioDadosController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet("estrutura")]
    public async Task<IActionResult> ObterEstrutura()
    {
        try
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            var resultado = new List<TabelaInfo>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT t.table_name, c.column_name, c.data_type,
                           c.character_maximum_length, c.is_nullable, c.column_default, c.ordinal_position
                    FROM information_schema.tables t
                    JOIN information_schema.columns c ON c.table_name = t.table_name AND c.table_schema = t.table_schema
                    WHERE t.table_schema = 'public' AND t.table_type = 'BASE TABLE'
                      AND t.table_name NOT IN ('__EFMigrationsHistory')
                    ORDER BY t.table_name, c.ordinal_position";

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
                        Obrigatorio = reader.GetString(4) == "NO",
                        ValorPadrao = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Ordem = reader.GetInt32(6)
                    });
                }
            }

            // Unique/PK constraints
            var unicos = new HashSet<string>();
            using (var cmd2 = conn.CreateCommand())
            {
                cmd2.CommandText = @"
                    SELECT tc.table_name, kcu.column_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage kcu
                        ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema
                    WHERE tc.constraint_type IN ('UNIQUE', 'PRIMARY KEY') AND tc.table_schema = 'public'";

                using var reader2 = await cmd2.ExecuteReaderAsync();
                while (await reader2.ReadAsync())
                    unicos.Add($"{reader2.GetString(0)}.{reader2.GetString(1)}");
            }

            foreach (var t in resultado)
                foreach (var c in t.Colunas)
                    c.Unico = unicos.Contains($"{t.Nome}.{c.Nome}");

            // Load table definitions
            var defTabelas = await _db.DicionarioTabelas.ToListAsync();
            foreach (var t in resultado)
            {
                var def = defTabelas.FirstOrDefault(d => d.Tabela == t.Nome);
                if (def != null)
                {
                    t.Escopo = def.Escopo;
                    t.Replica = def.Replica;
                    t.InstrucaoIA = def.InstrucaoIA;
                }
            }

            // Load field reviews
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
                        c.InstrucaoIA = rev.InstrucaoIA;
                    }
                }

            return Ok(new { success = true, data = resultado });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao obter estrutura do banco");
            return StatusCode(500, new { success = false, message = $"Erro: {ex.Message}" });
        }
    }

    [HttpPost("revisar-campo")]
    public async Task<IActionResult> RevisarCampo([FromBody] RevisaoCampoDto dto)
    {
        try
        {
            var existente = await _db.DicionarioRevisoes
                .FirstOrDefaultAsync(r => r.Tabela == dto.Tabela && r.Coluna == dto.Coluna);

            if (existente == null)
            {
                existente = new Domain.Entities.DicionarioRevisao { Tabela = dto.Tabela, Coluna = dto.Coluna };
                _db.DicionarioRevisoes.Add(existente);
            }

            existente.Revisado = dto.Revisado;
            existente.Observacao = dto.Observacao;
            existente.Unico = dto.Unico;
            existente.Obrigatorio = dto.Obrigatorio;
            existente.InstrucaoIA = dto.InstrucaoIA;
            existente.RevisadoEm = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await AutoExportarJson();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao salvar revisão de campo");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("revisar-tabela")]
    public async Task<IActionResult> RevisarTabela([FromBody] RevisaoTabelaDto dto)
    {
        try
        {
            var existente = await _db.DicionarioTabelas
                .FirstOrDefaultAsync(d => d.Tabela == dto.Tabela);

            if (existente == null)
            {
                existente = new Domain.Entities.DicionarioTabela { Tabela = dto.Tabela };
                _db.DicionarioTabelas.Add(existente);
            }

            existente.Escopo = dto.Escopo;
            existente.Replica = dto.Replica;
            existente.InstrucaoIA = dto.InstrucaoIA;
            existente.AtualizadoEm = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await AutoExportarJson();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao salvar definição de tabela");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Retorna instruções IA do dicionário para montar o system prompt da Cassi.
    /// </summary>
    [HttpGet("instrucoes-ia")]
    public async Task<IActionResult> ObterInstrucoesIA()
    {
        try
        {
            var tabelas = await _db.DicionarioTabelas
                .Where(t => t.InstrucaoIA != null && t.InstrucaoIA != "")
                .ToListAsync();

            var campos = await _db.DicionarioRevisoes
                .Where(r => r.InstrucaoIA != null && r.InstrucaoIA != "")
                .ToListAsync();

            var instrucoes = new List<string>();

            foreach (var t in tabelas)
                instrucoes.Add($"Tabela {t.Tabela} ({t.Escopo}): {t.InstrucaoIA}");

            foreach (var c in campos)
                instrucoes.Add($"Campo {c.Tabela}.{c.Coluna}: {c.InstrucaoIA}");

            return Ok(new { success = true, data = instrucoes });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao obter instruções IA");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
    /// <summary>
    /// Exporta o DD para JSON e retorna o conteúdo.
    /// Também salva automaticamente em ContextDocuments/dicionario-dados.json.
    /// </summary>
    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar()
    {
        try
        {
            var dados = await MontarExportacao();
            await SalvarJsonNoDisco(dados);
            return Ok(new { success = true, data = dados });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao exportar DD");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Importa o DD de um JSON, substituindo todas as definições.
    /// </summary>
    [HttpPost("importar")]
    public async Task<IActionResult> Importar([FromBody] DicionarioExportacao dados)
    {
        try
        {
            // Limpar existentes
            _db.DicionarioTabelas.RemoveRange(await _db.DicionarioTabelas.ToListAsync());
            _db.DicionarioRevisoes.RemoveRange(await _db.DicionarioRevisoes.ToListAsync());
            await _db.SaveChangesAsync();

            // Inserir importados
            foreach (var t in dados.Tabelas)
            {
                _db.DicionarioTabelas.Add(new Domain.Entities.DicionarioTabela
                {
                    Tabela = t.Tabela,
                    Escopo = t.Escopo,
                    Replica = t.Replica,
                    InstrucaoIA = t.InstrucaoIA,
                    AtualizadoEm = DateTime.UtcNow
                });
            }

            foreach (var c in dados.Campos)
            {
                _db.DicionarioRevisoes.Add(new Domain.Entities.DicionarioRevisao
                {
                    Tabela = c.Tabela,
                    Coluna = c.Coluna,
                    Revisado = c.Revisado,
                    Unico = c.Unico,
                    Obrigatorio = c.Obrigatorio,
                    Observacao = c.Observacao,
                    InstrucaoIA = c.InstrucaoIA,
                    RevisadoEm = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true, message = $"Importado: {dados.Tabelas.Count} tabelas, {dados.Campos.Count} campos" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao importar DD");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    private async Task<DicionarioExportacao> MontarExportacao()
    {
        var tabelas = await _db.DicionarioTabelas.OrderBy(t => t.Tabela).ToListAsync();
        var campos = await _db.DicionarioRevisoes.OrderBy(r => r.Tabela).ThenBy(r => r.Coluna).ToListAsync();

        return new DicionarioExportacao
        {
            ExportadoEm = DateTime.UtcNow,
            Tabelas = tabelas.Select(t => new DdTabela
            {
                Tabela = t.Tabela, Escopo = t.Escopo, Replica = t.Replica, InstrucaoIA = t.InstrucaoIA
            }).ToList(),
            Campos = campos.Select(c => new DdCampo
            {
                Tabela = c.Tabela, Coluna = c.Coluna, Revisado = c.Revisado,
                Unico = c.Unico, Obrigatorio = c.Obrigatorio,
                Observacao = c.Observacao, InstrucaoIA = c.InstrucaoIA
            }).ToList()
        };
    }

    private async Task SalvarJsonNoDisco(DicionarioExportacao dados)
    {
        try
        {
            // Salvar na pasta ContextDocuments (relativo à raiz do projeto)
            var rootPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", ".."));
            var filePath = Path.Combine(rootPath, "ContextDocuments", "dicionario-dados.json");
            var json = JsonSerializer.Serialize(dados, _jsonOpts);
            await System.IO.File.WriteAllTextAsync(filePath, json);
            Log.Debug("DD exportado para {Path}", filePath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Não foi possível salvar DD no disco");
        }
    }

    private async Task AutoExportarJson()
    {
        try
        {
            var dados = await MontarExportacao();
            await SalvarJsonNoDisco(dados);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Auto-export DD falhou");
        }
    }
}

public class DicionarioExportacao
{
    public DateTime ExportadoEm { get; set; }
    public List<DdTabela> Tabelas { get; set; } = new();
    public List<DdCampo> Campos { get; set; } = new();
}

public class DdTabela
{
    public string Tabela { get; set; } = "";
    public string Escopo { get; set; } = "global";
    public bool Replica { get; set; }
    public string? InstrucaoIA { get; set; }
}

public class DdCampo
{
    public string Tabela { get; set; } = "";
    public string Coluna { get; set; } = "";
    public bool Revisado { get; set; }
    public bool? Unico { get; set; }
    public bool? Obrigatorio { get; set; }
    public string? Observacao { get; set; }
    public string? InstrucaoIA { get; set; }
}

public class TabelaInfo
{
    public string Nome { get; set; } = "";
    public string Escopo { get; set; } = "global";
    public bool Replica { get; set; } = true;
    public string? InstrucaoIA { get; set; }
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
    public bool Revisado { get; set; }
    public string? Observacao { get; set; }
    public bool? UnicoCustom { get; set; }
    public bool? ObrigatorioCustom { get; set; }
    public string? InstrucaoIA { get; set; }
}

public record RevisaoCampoDto(string Tabela, string Coluna, bool Revisado, string? Observacao, bool? Unico, bool? Obrigatorio, string? InstrucaoIA);
public record RevisaoTabelaDto(string Tabela, string Escopo, bool Replica, string? InstrucaoIA);
