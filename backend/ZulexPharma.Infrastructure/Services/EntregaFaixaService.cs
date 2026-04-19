using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.Entregas;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class EntregaFaixaService : IEntregaFaixaService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Faixas de Entrega";
    private const string ENTIDADE = "EntregaFaixa";

    public EntregaFaixaService(AppDbContext db, ILogAcaoService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<List<EntregaFaixaDto>> ListarAsync(long filialId)
    {
        return await _db.EntregaFaixas
            .Where(f => f.FilialId == filialId)
            .OrderBy(f => f.Ordem).ThenBy(f => f.RaioMaxKm)
            .Select(f => new EntregaFaixaDto
            {
                Id = f.Id,
                FilialId = f.FilialId,
                RaioMaxKm = f.RaioMaxKm,
                Valor = f.Valor,
                Ordem = f.Ordem
            })
            .ToListAsync();
    }

    public async Task<EntregaFaixaDto> CriarAsync(EntregaFaixaFormDto dto)
    {
        if (dto.RaioMaxKm <= 0) throw new ArgumentException("Raio deve ser maior que zero.");
        if (dto.Valor < 0) throw new ArgumentException("Valor não pode ser negativo.");

        var faixa = new EntregaFaixa
        {
            FilialId = dto.FilialId,
            RaioMaxKm = dto.RaioMaxKm,
            Valor = dto.Valor,
            Ordem = dto.Ordem
        };
        _db.EntregaFaixas.Add(faixa);
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, faixa.Id, novo: new()
        {
            ["RaioMaxKm"] = faixa.RaioMaxKm.ToString("0.###"),
            ["Valor"] = faixa.Valor.ToString("0.00")
        });

        return new EntregaFaixaDto
        {
            Id = faixa.Id,
            FilialId = faixa.FilialId,
            RaioMaxKm = faixa.RaioMaxKm,
            Valor = faixa.Valor,
            Ordem = faixa.Ordem
        };
    }

    public async Task AtualizarAsync(long id, EntregaFaixaFormDto dto)
    {
        if (dto.RaioMaxKm <= 0) throw new ArgumentException("Raio deve ser maior que zero.");
        if (dto.Valor < 0) throw new ArgumentException("Valor não pode ser negativo.");

        var faixa = await _db.EntregaFaixas.FindAsync(id)
            ?? throw new KeyNotFoundException($"Faixa {id} não encontrada.");

        faixa.RaioMaxKm = dto.RaioMaxKm;
        faixa.Valor = dto.Valor;
        faixa.Ordem = dto.Ordem;
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync(TELA, "EDIÇÃO", ENTIDADE, id);
    }

    public async Task ExcluirAsync(long id)
    {
        var faixa = await _db.EntregaFaixas.FindAsync(id)
            ?? throw new KeyNotFoundException($"Faixa {id} não encontrada.");
        _db.EntregaFaixas.Remove(faixa);
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id);
    }
}
