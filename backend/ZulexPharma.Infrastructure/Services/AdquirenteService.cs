using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Adquirentes;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class AdquirenteService : IAdquirenteService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Adquirentes";
    private const string ENTIDADE = "Adquirente";

    public AdquirenteService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<AdquirenteListDto>> ListarAsync()
    {
        try
        {
            return await _db.Adquirentes
                .Include(a => a.Bandeiras)
                .OrderBy(a => a.Nome)
                .Select(a => new AdquirenteListDto
                {
                    Id = a.Id, Nome = a.Nome,
                    TotalBandeiras = a.Bandeiras.Count,
                    Ativo = a.Ativo, CriadoEm = a.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em AdquirenteService.ListarAsync"); throw; }
    }

    public async Task<AdquirenteDetalheDto?> ObterAsync(long id)
    {
        try
        {
            var a = await _db.Adquirentes
                .Include(x => x.Bandeiras).ThenInclude(b => b.Tarifas).ThenInclude(t => t.ContaBancaria)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return null;
            return MapDetalhe(a);
        }
        catch (Exception ex) { Log.Error(ex, "Erro em AdquirenteService.ObterAsync | Id: {Id}", id); throw; }
    }

    public async Task<AdquirenteDetalheDto> CriarAsync(AdquirenteFormDto dto)
    {
        try
        {
            Validar(dto);
            var adq = new Adquirente { Nome = dto.Nome.Trim().ToUpper(), Ativo = dto.Ativo };
            MapearBandeiras(adq, dto.Bandeiras);
            _db.Adquirentes.Add(adq);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, adq.Id, novo: new() { ["Nome"] = adq.Nome });
            return (await ObterAsync(adq.Id))!;
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em AdquirenteService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, AdquirenteFormDto dto)
    {
        try
        {
            Validar(dto);
            var adq = await _db.Adquirentes
                .Include(x => x.Bandeiras).ThenInclude(b => b.Tarifas)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException($"Adquirente {id} não encontrada.");

            adq.Nome = dto.Nome.Trim().ToUpper();
            adq.Ativo = dto.Ativo;

            // Limpar e recriar bandeiras/tarifas
            foreach (var b in adq.Bandeiras) _db.AdquirenteTarifas.RemoveRange(b.Tarifas);
            _db.AdquirenteBandeiras.RemoveRange(adq.Bandeiras);
            MapearBandeiras(adq, dto.Bandeiras);

            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em AdquirenteService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var adq = await _db.Adquirentes.FindAsync(id)
                ?? throw new KeyNotFoundException($"Adquirente {id} não encontrada.");

            // Verificar uso
            var emUso = await _db.ContasReceber.AnyAsync(c => c.AdquirenteBandeiraId != null &&
                _db.AdquirenteBandeiras.Any(b => b.AdquirenteId == id && b.Id == c.AdquirenteBandeiraId));
            if (emUso)
            {
                adq.Ativo = false;
                await _db.SaveChangesAsync();
                return "inativado";
            }

            _db.Adquirentes.Remove(adq);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id);
            return "excluido";
        }
        catch (Exception ex) when (ex is not KeyNotFoundException) { Log.Error(ex, "Erro em AdquirenteService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    private static void Validar(AdquirenteFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
    }

    private static void MapearBandeiras(Adquirente adq, List<BandeiraFormDto> bandeiras)
    {
        foreach (var bDto in bandeiras)
        {
            var bandeira = new AdquirenteBandeira { Bandeira = bDto.Bandeira.Trim().ToUpper() };
            foreach (var tDto in bDto.Tarifas)
            {
                bandeira.Tarifas.Add(new AdquirenteTarifa
                {
                    Modalidade = tDto.Modalidade,
                    Tarifa = tDto.Tarifa,
                    PrazoRecebimento = tDto.PrazoRecebimento,
                    ContaBancariaId = tDto.ContaBancariaId
                });
            }
            adq.Bandeiras.Add(bandeira);
        }
    }

    private static string ModalidadeTexto(ModalidadeCartao m) => m switch
    {
        ModalidadeCartao.Debito => "Débito",
        ModalidadeCartao.Credito => "Crédito",
        _ => $"Parcelado {(int)m - 1}x"
    };

    private static AdquirenteDetalheDto MapDetalhe(Adquirente a) => new()
    {
        Id = a.Id, Nome = a.Nome, Ativo = a.Ativo, CriadoEm = a.CriadoEm,
        Bandeiras = a.Bandeiras.Select(b => new BandeiraDto
        {
            Id = b.Id, Bandeira = b.Bandeira,
            Tarifas = b.Tarifas.Select(t => new TarifaDto
            {
                Id = t.Id, Modalidade = t.Modalidade,
                ModalidadeDescricao = ModalidadeTexto(t.Modalidade),
                Tarifa = t.Tarifa, PrazoRecebimento = t.PrazoRecebimento,
                ContaBancariaId = t.ContaBancariaId,
                ContaBancariaNome = t.ContaBancaria != null ? $"{t.ContaBancaria.Descricao}" : null
            }).OrderBy(t => t.Modalidade).ToList()
        }).OrderBy(b => b.Bandeira).ToList()
    };
}
