using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.BugsAtivos;

/// <summary>
/// BUG ATIVO (plano §2 / synAteAqui §6.2 / Codex P0.7, cura fase 3): os services editam agregados
/// com RemoveRange + re-add (Ids NOVOS da faixa do no) e o applicator (UpsertFilhosPocoAsync) e'
/// append/update-only — nunca deleta filho ausente do JSON. Resultado: cada edicao DUPLICA o
/// conjunto de filhos no no destino (os Ids velhos ficam + os novos entram).
/// VERMELHO ate' a fase 3 (contrato de colecao: chave presente no JSON = autoritativa -> delete-missing).
/// </summary>
[Collection("pg")]
public class FilhosPocoTests
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly PostgresFixture _pg;
    public FilhosPocoTests(PostgresFixture pg) => _pg = pg;

    [Fact]
    public async Task EditarCliente_NaoDeveDuplicarFilhosNoDestino()
    {
        var t0 = new DateTime(2026, 7, 1, 10, 0, 0);
        var t1 = new DateTime(2026, 7, 1, 12, 0, 0);
        const long pessoaId = 3_000_000_110;
        const long clienteId = 3_000_000_111;

        // Pessoa (FK do Cliente) chega primeiro, como no sync real
        var pessoa = new Pessoa
        {
            Id = pessoaId, Tipo = "F", Nome = "CLIENTE TESTE FASE0", CpfCnpj = "99988877766",
            CriadoEm = t0, NoOrigemId = 3, Ativo = true
        };
        var resPessoa = await Aplicar("Pessoas", "I", pessoaId, JsonSerializer.Serialize(pessoa, _json), t0);
        Assert.Equal(ResultadoSync.Aplicado, resPessoa);

        // Cliente V1 com 2 autorizacoes (Ids da faixa do no 3)
        var guid = Guid.NewGuid();
        var v1 = NovoCliente(clienteId, pessoaId, guid, t0, atualizadoEm: null);
        v1.Autorizacoes.Add(new ClienteAutorizacao { Id = 3_000_000_112, ClienteId = clienteId, Nome = "MARIA" });
        v1.Autorizacoes.Add(new ClienteAutorizacao { Id = 3_000_000_113, ClienteId = clienteId, Nome = "JOSE" });
        var resV1 = await Aplicar("Clientes", "I", clienteId, JsonSerializer.Serialize(v1, _json), t0);
        Assert.Equal(ResultadoSync.Aplicado, resV1);

        // Edicao na origem: o service faz RemoveRange + re-add -> as MESMAS 2 pessoas autorizadas
        // voltam com Ids NOVOS. O JSON da edicao carrega SO' os 2 filhos novos.
        var v2 = NovoCliente(clienteId, pessoaId, guid, t0, atualizadoEm: t1);
        v2.Autorizacoes.Add(new ClienteAutorizacao { Id = 3_000_000_114, ClienteId = clienteId, Nome = "MARIA" });
        v2.Autorizacoes.Add(new ClienteAutorizacao { Id = 3_000_000_115, ClienteId = clienteId, Nome = "JOSE" });
        var resV2 = await Aplicar("Clientes", "U", clienteId, JsonSerializer.Serialize(v2, _json), t1);

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var filhos = await db.Set<ClienteAutorizacao>().Where(a => a.ClienteId == clienteId).ToListAsync();

        Assert.True(filhos.Count == 2,
            $"DUPLICACAO: o cliente foi editado UMA vez na origem (2 autorizacoes) e o destino terminou " +
            $"com {filhos.Count} filhos [{string.Join(", ", filhos.Select(f => $"{f.Id}:{f.Nome}"))}] (U deu {resV2}). " +
            "O applicator so' insere/atualiza por Id — os filhos velhos (112/113) nunca sao removidos. " +
            "Cada edicao rotineira duplica o conjunto; a proxima edicao no par leva os orfaos de volta. " +
            "Cura (fase 3): colecao PRESENTE no JSON = autoritativa -> diff por Id com delete-missing.");
    }

    private static Cliente NovoCliente(long id, long pessoaId, Guid guid, DateTime criadoEm, DateTime? atualizadoEm) => new()
    {
        Id = id, PessoaId = pessoaId, CriadoEm = criadoEm, AtualizadoEm = atualizadoEm,
        NoOrigemId = 3, SyncGuid = guid, Ativo = true
    };

    private async Task<ResultadoSync> Aplicar(string tabela, string op, long id, string json, DateTime opCriadoEm)
    {
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        return await SyncApplicator.AplicarOperacaoAsync(db, tabela, op, id, json, opCriadoEm, noOrigemId: 3);
    }
}
