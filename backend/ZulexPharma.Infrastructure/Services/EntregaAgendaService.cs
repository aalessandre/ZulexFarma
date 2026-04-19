using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Entregas;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class EntregaAgendaService : IEntregaAgendaService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Entregas - Faixas e Regras";
    private const string ENTIDADE = "EntregaAgenda";

    public EntregaAgendaService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<EntregaAgendaSlotDto>> ListarAsync(long filialId)
    {
        return await _db.EntregaAgendas
            .Where(a => a.FilialId == filialId)
            .Include(a => a.Perfil)
            .OrderBy(a => a.EhFeriado).ThenBy(a => a.DiaSemana).ThenBy(a => a.Turno)
            .Select(a => new EntregaAgendaSlotDto
            {
                Id = a.Id,
                DiaSemana = a.DiaSemana,
                Turno = a.Turno,
                EhFeriado = a.EhFeriado,
                PerfilId = a.PerfilId,
                PerfilNome = a.Perfil!.Nome
            })
            .ToListAsync();
    }

    public async Task SalvarAsync(EntregaAgendaSaveDto dto)
    {
        ValidarCobertura(dto);

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var antigos = await _db.EntregaAgendas.Where(a => a.FilialId == dto.FilialId).ToListAsync();
            _db.EntregaAgendas.RemoveRange(antigos);
            await _db.SaveChangesAsync();

            foreach (var s in dto.Slots)
            {
                _db.EntregaAgendas.Add(new EntregaAgenda
                {
                    FilialId = dto.FilialId,
                    DiaSemana = s.EhFeriado ? null : s.DiaSemana,
                    Turno = s.Turno,
                    EhFeriado = s.EhFeriado,
                    PerfilId = s.PerfilId
                });
            }
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, dto.FilialId);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<EntregaPerfil?> ResolverPerfilAsync(long filialId, DateTime dataHora, bool ehFeriado)
    {
        var turno = TurnoHelper.Resolver(dataHora);

        EntregaAgenda? slot;
        if (ehFeriado)
        {
            slot = await _db.EntregaAgendas
                .Include(a => a.Perfil).ThenInclude(p => p!.Faixas)
                .FirstOrDefaultAsync(a => a.FilialId == filialId && a.EhFeriado && a.Turno == turno);
        }
        else
        {
            var diaSemana = TurnoHelper.DiaSemana(dataHora);
            slot = await _db.EntregaAgendas
                .Include(a => a.Perfil).ThenInclude(p => p!.Faixas)
                .FirstOrDefaultAsync(a => a.FilialId == filialId && !a.EhFeriado
                                       && a.DiaSemana == diaSemana && a.Turno == turno);
        }

        return slot?.Perfil;
    }

    private static void ValidarCobertura(EntregaAgendaSaveDto dto)
    {
        if (dto.Slots == null || dto.Slots.Count != 16)
            throw new ArgumentException("Agenda incompleta — são necessários 16 slots (7 dias × 2 turnos + 1 feriado × 2 turnos).");

        var turnos = new[] { TurnoEntrega.Diurno, TurnoEntrega.Noturno };

        // 14 slots normais (7 dias × 2 turnos)
        for (int d = 1; d <= 7; d++)
        {
            foreach (var t in turnos)
            {
                if (!dto.Slots.Any(s => !s.EhFeriado && s.DiaSemana == d && s.Turno == t && s.PerfilId > 0))
                    throw new ArgumentException($"Slot faltando: dia {d}, turno {t}.");
            }
        }

        // 2 slots de feriado (um por turno)
        foreach (var t in turnos)
        {
            if (!dto.Slots.Any(s => s.EhFeriado && s.Turno == t && s.PerfilId > 0))
                throw new ArgumentException($"Slot faltando: feriado, turno {t}.");
        }
    }
}
