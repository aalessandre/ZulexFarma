using Npgsql;

namespace ZulexPharma.Tests.BugsAtivos;

/// <summary>
/// BUG ATIVO (plano §2, cura fase 2): o PULL serve "Id > ultimoId" (SyncController.Receber), mas o
/// Id da SyncFila e' alocado no INSERT e so' fica VISIVEL no COMMIT. Uma transacao que commita tarde
/// tem Id MENOR que o cursor ja' avancado -> a op NUNCA e' entregue. Perda permanente e silenciosa.
/// Este teste reproduz a mecanica exata do cursor contra o Postgres real.
/// VERMELHO ate' a fase 2 (publicador + SeqEntrega).
/// </summary>
[Collection("pg")]
public class CursorGapTests
{
    private readonly PostgresFixture _pg;
    public CursorGapTests(PostgresFixture pg) => _pg = pg;

    private const string InsertSql = """
        INSERT INTO "SyncFila" ("Tabela","Operacao","RegistroId","NoOrigemId","DadosJson","CriadoEm","Enviado")
        VALUES ('GapTeste','I', {0}, 1, '{{}}', now(), false) RETURNING "Id"
        """;

    [Fact]
    public async Task Gap_CursorId_PerdeCommitTardio()
    {
        await using (var limpa = await _pg.AbrirConexaoAsync())
            await PostgresFixture.Exec(limpa, "DELETE FROM \"SyncFila\" WHERE \"Tabela\" = 'GapTeste'");

        // Tx A: aloca o Id MENOR e fica aberta (ex.: /enviar com lote grande ainda commitando)
        await using var connA = await _pg.AbrirConexaoAsync();
        var txA = await connA.BeginTransactionAsync();
        var idA = await InserirAsync(connA, txA, registroId: 9001);

        // Tx B: aloca o Id MAIOR e commita PRIMEIRO
        await using var connB = await _pg.AbrirConexaoAsync();
        var txB = await connB.BeginTransactionAsync();
        var idB = await InserirAsync(connB, txB, registroId: 9002);
        await txB.CommitAsync();
        Assert.True(idA < idB, "pre-condicao: A alocou Id menor que B");

        // PULL 1 (mecanica do /receber): Id > cursor, ordena por Id, cursor = maior Id servido
        long cursor = 0;
        var entregues = new HashSet<long>(await LerIdsAsync(cursor));
        cursor = entregues.Count > 0 ? entregues.Max() : cursor;

        // A tx A commita DEPOIS do pull — a op dela existe, e' valida, precisa ser entregue
        await txA.CommitAsync();

        // PULL 2 em diante: o cursor ja' passou do Id de A
        foreach (var id in await LerIdsAsync(cursor)) entregues.Add(id);

        Assert.True(entregues.Contains(idA) && entregues.Contains(idB),
            $"PERDA SILENCIOSA: a op {idA} (commit tardio) nunca foi entregue — o cursor 'Id > ultimoId' " +
            $"avancou para {cursor} enquanto ela ainda estava invisivel. Entregues: [{string.Join(",", entregues)}]. " +
            "Cura (fase 2): publicador numera SeqEntrega so' em linha COMMITADA; cursor passa a ser SeqEntrega.");
    }

    private async Task<long> InserirAsync(NpgsqlConnection conn, NpgsqlTransaction tx, long registroId)
    {
        await using var cmd = new NpgsqlCommand(string.Format(InsertSql, registroId), conn, tx);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<List<long>> LerIdsAsync(long cursor)
    {
        var ids = new List<long>();
        await using var conn = await _pg.AbrirConexaoAsync();
        await using var cmd = new NpgsqlCommand(
            $"SELECT \"Id\" FROM \"SyncFila\" WHERE \"Tabela\" = 'GapTeste' AND \"Id\" > {cursor} ORDER BY \"Id\"", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) ids.Add(reader.GetInt64(0));
        return ids;
    }
}
