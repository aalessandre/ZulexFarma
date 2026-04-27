using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services.SelfCheckout;

public class SequenciaCentralService : ISequenciaCentralService
{
    private readonly AppDbContext _db;

    public SequenciaCentralService(AppDbContext db) { _db = db; }

    public async Task<long> ProximoNumeroAsync(long filialId, ModeloDocumento modelo, int serie, long? numeroPartida = null, CancellationToken ct = default)
    {
        // Tenta dentro de uma transação serializável + lock pessimista de linha.
        await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        // SELECT ... FOR UPDATE para travar a linha durante o incremento.
        // EF Core não expõe FOR UPDATE direto, então usamos SQL bruto.
        var existente = await _db.SequenciasCentrais
            .FromSqlInterpolated($@"
                SELECT * FROM ""SequenciasCentrais""
                WHERE ""FilialId"" = {filialId}
                  AND ""ModeloDocumento"" = {(int)modelo}
                  AND ""Serie"" = {serie}
                FOR UPDATE")
            .FirstOrDefaultAsync(ct);

        long numeroReservado;
        if (existente == null)
        {
            // Sem registro: cria iniciando em numeroPartida (ou 1 se null).
            var partida = numeroPartida is > 0 ? numeroPartida.Value : 1L;
            var novo = new SequenciaCentral
            {
                FilialId = filialId,
                ModeloDocumento = modelo,
                Serie = serie,
                ProximoNumero = partida + 1 // a partida foi reservada para o caller
            };
            _db.SequenciasCentrais.Add(novo);
            numeroReservado = partida;
        }
        else
        {
            numeroReservado = existente.ProximoNumero;
            existente.ProximoNumero = numeroReservado + 1;
            existente.AtualizadoEm = DataHoraHelper.Agora();
            _db.SequenciasCentrais.Update(existente);
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return numeroReservado;
    }
}
