using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Feriados;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class FeriadoService : IFeriadoService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private readonly IHttpClientFactory _httpFactory;
    private const string TELA = "Feriados";
    private const string ENTIDADE = "Feriado";
    private const string BrasilApiBaseUrl = "https://brasilapi.com.br";

    public FeriadoService(AppDbContext db, ILogAcaoService log, IHttpClientFactory httpFactory)
    {
        _db = db; _log = log; _httpFactory = httpFactory;
    }

    public async Task<List<FeriadoDto>> ListarAsync(int? ano = null, long? filialId = null)
    {
        var q = _db.Feriados.Include(f => f.Filial).AsQueryable();
        if (ano.HasValue)
        {
            var ini = new DateOnly(ano.Value, 1, 1);
            var fim = new DateOnly(ano.Value, 12, 31);
            q = q.Where(f => f.Data >= ini && f.Data <= fim);
        }
        if (filialId.HasValue)
            q = q.Where(f => f.FilialId == null || f.FilialId == filialId.Value);

        return await q.OrderBy(f => f.Data).ThenBy(f => f.Nome)
            .Select(f => new FeriadoDto
            {
                Id = f.Id, Data = f.Data, Nome = f.Nome,
                Ambito = f.Ambito, Uf = f.Uf,
                FilialId = f.FilialId,
                FilialNome = f.Filial != null ? f.Filial.NomeFilial : null,
                Origem = f.Origem, Ativo = f.Ativo
            })
            .ToListAsync();
    }

    public async Task<FeriadoDto> CriarAsync(FeriadoFormDto dto)
    {
        Validar(dto);
        var feriado = new Feriado
        {
            Data = dto.Data,
            Nome = dto.Nome.Trim().ToUpper(),
            Ambito = dto.Ambito,
            Uf = dto.Ambito == AmbitoFeriado.Estadual ? dto.Uf?.Trim().ToUpper() : null,
            FilialId = dto.Ambito == AmbitoFeriado.Municipal ? dto.FilialId : null,
            Origem = OrigemFeriado.Manual,
            Ativo = dto.Ativo
        };

        if (await ExisteDuplicado(feriado))
            throw new ArgumentException("Já existe um feriado cadastrado nessa data com o mesmo âmbito.");

        _db.Feriados.Add(feriado);
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, feriado.Id);
        return await MapAsync(feriado);
    }

    public async Task AtualizarAsync(long id, FeriadoFormDto dto)
    {
        Validar(dto);
        var f = await _db.Feriados.FindAsync(id)
            ?? throw new KeyNotFoundException("Feriado não encontrado.");
        f.Data = dto.Data;
        f.Nome = dto.Nome.Trim().ToUpper();
        f.Ambito = dto.Ambito;
        f.Uf = dto.Ambito == AmbitoFeriado.Estadual ? dto.Uf?.Trim().ToUpper() : null;
        f.FilialId = dto.Ambito == AmbitoFeriado.Municipal ? dto.FilialId : null;
        f.Ativo = dto.Ativo;

        if (await ExisteDuplicado(f, ignoreId: id))
            throw new ArgumentException("Já existe um feriado cadastrado nessa data com o mesmo âmbito.");

        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id);
    }

    public async Task ExcluirAsync(long id)
    {
        var f = await _db.Feriados.FindAsync(id)
            ?? throw new KeyNotFoundException("Feriado não encontrado.");
        _db.Feriados.Remove(f);
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id);
    }

    public async Task<FeriadoImportResultDto> ImportarNacionaisAsync(int ano)
    {
        var url = $"{BrasilApiBaseUrl}/api/feriados/v1/{ano}";
        using var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add("User-Agent", "ZulexPharma-ERP/1.0");

        List<BrasilApiFeriado>? lista;
        try
        {
            var json = await client.GetStringAsync(url);
            lista = JsonSerializer.Deserialize<List<BrasilApiFeriado>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao buscar feriados na BrasilAPI | ano: {Ano}", ano);
            throw new InvalidOperationException("Erro ao consultar BrasilAPI. Tente novamente.");
        }

        if (lista == null) throw new InvalidOperationException("BrasilAPI retornou vazio.");

        var result = new FeriadoImportResultDto();
        foreach (var item in lista)
        {
            if (!DateOnly.TryParse(item.Date, out var data)) continue;
            var existe = await _db.Feriados.AnyAsync(f =>
                f.Data == data && f.Ambito == AmbitoFeriado.Nacional);
            if (existe) { result.JaExistentes++; continue; }

            _db.Feriados.Add(new Feriado
            {
                Data = data,
                Nome = (item.Name ?? "").Trim().ToUpper(),
                Ambito = AmbitoFeriado.Nacional,
                Origem = OrigemFeriado.Importado,
                Ativo = true
            });
            result.Importados++;
            result.Nomes.Add(item.Name ?? "");
        }

        if (result.Importados > 0) await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "IMPORTAÇÃO", ENTIDADE, 0,
            novo: new Dictionary<string, string?> { ["ano"] = ano.ToString(), ["importados"] = result.Importados.ToString() });
        return result;
    }

    public async Task<bool> IsFeriadoAsync(DateOnly data, long filialId)
    {
        var filial = await _db.Filiais.AsNoTracking().FirstOrDefaultAsync(f => f.Id == filialId);
        var uf = filial?.Uf;

        return await _db.Feriados.AnyAsync(f => f.Ativo && f.Data == data && (
            f.Ambito == AmbitoFeriado.Nacional ||
            (f.Ambito == AmbitoFeriado.Estadual && f.Uf == uf) ||
            (f.Ambito == AmbitoFeriado.Municipal && f.FilialId == filialId)
        ));
    }

    // ── Helpers ────────────────────────────────────────────

    private async Task<bool> ExisteDuplicado(Feriado f, long? ignoreId = null)
    {
        return await _db.Feriados.AnyAsync(x =>
            (ignoreId == null || x.Id != ignoreId) &&
            x.Data == f.Data && x.Ambito == f.Ambito &&
            x.Uf == f.Uf && x.FilialId == f.FilialId);
    }

    private static void Validar(FeriadoFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new ArgumentException("Nome do feriado é obrigatório.");
        if (dto.Ambito == AmbitoFeriado.Estadual && string.IsNullOrWhiteSpace(dto.Uf))
            throw new ArgumentException("UF é obrigatória para feriado estadual.");
        if (dto.Ambito == AmbitoFeriado.Municipal && !dto.FilialId.HasValue)
            throw new ArgumentException("Filial é obrigatória para feriado municipal.");
    }

    private async Task<FeriadoDto> MapAsync(Feriado f)
    {
        var filialNome = f.FilialId.HasValue
            ? await _db.Filiais.Where(x => x.Id == f.FilialId).Select(x => x.NomeFilial).FirstOrDefaultAsync()
            : null;
        return new FeriadoDto
        {
            Id = f.Id, Data = f.Data, Nome = f.Nome,
            Ambito = f.Ambito, Uf = f.Uf,
            FilialId = f.FilialId, FilialNome = filialNome,
            Origem = f.Origem, Ativo = f.Ativo
        };
    }

    private class BrasilApiFeriado
    {
        public string? Date { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
    }
}
