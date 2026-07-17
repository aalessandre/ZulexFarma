using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.Fase3;

/// <summary>
/// FASE 3 — CONTRATO DE COLECAO no OUTBOX e no APPLICATOR:
/// - editar o pai SEM Include -> a chave da colecao e' OMITIDA no JSON -> o destino PRESERVA os filhos
///   (o motivo da reversao da v1: FinalizarAsync sem Itens.Descontos apagaria desconto de toda venda);
/// - editar COM Include -> chave presente = autoritativa -> destino reconcilia (delete-missing);
/// - editar SO' o filho -> o pai ganha touch e a op "U" sai com a colecao presente.
/// </summary>
[Collection("pg")]
public class ContratoColecaoTests
{
    private readonly PostgresFixture _pg;
    public ContratoColecaoTests(PostgresFixture pg) => _pg = pg;

    private async Task<(long pessoaId, long clienteId)> CriarClienteComFilhosAsync()
    {
        await using var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1);
        var pessoa = new Pessoa { Tipo = "F", Nome = $"CONTRATO {Guid.NewGuid():N}", CpfCnpj = Guid.NewGuid().ToString("N")[..11] };
        db.Pessoas.Add(pessoa);
        await db.SaveChangesAsync();
        var cliente = new Cliente { PessoaId = pessoa.Id };
        cliente.Autorizacoes.Add(new ClienteAutorizacao { Nome = "MARIA" });
        cliente.Autorizacoes.Add(new ClienteAutorizacao { Nome = "JOSE" });
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        return (pessoa.Id, cliente.Id);
    }

    private async Task<string> UltimoJsonDaOpAsync(long clienteId, string operacao)
    {
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var op = await db.SyncFila.Where(f => f.Tabela == "Clientes" && f.RegistroId == clienteId && f.Operacao == operacao)
            .OrderByDescending(f => f.Id).FirstAsync();
        return op.DadosJson!;
    }

    private static bool TemChave(string json, string chave)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty(chave, out var v) && v.ValueKind == JsonValueKind.Array;
    }

    [Fact]
    public async Task Outbox_InsertIncluiColecao_EdicaoSemIncludeOmite_ComIncludeInclui()
    {
        var (_, clienteId) = await CriarClienteComFilhosAsync();

        // INSERT: grafo recem-criado em memoria = verdade -> colecao PRESENTE
        Assert.True(TemChave(await UltimoJsonDaOpAsync(clienteId, "I"), "autorizacoes"),
            "no INSERT do agregado a colecao em memoria E' o estado — precisa viajar");

        // Edicao SEM Include: a colecao default vazia NAO pode virar [] autoritativo
        await using (var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1))
        {
            var cli = await db.Clientes.FirstAsync(c => c.Id == clienteId);
            cli.LimiteCredito = 99;
            await db.SaveChangesAsync();
        }
        Assert.False(TemChave(await UltimoJsonDaOpAsync(clienteId, "U"), "autorizacoes"),
            "PERIGO DA V1: edicao sem Include serializou a colecao vazia como [] — o destino apagaria " +
            "filhos LEGITIMOS. Colecao nao carregada = chave OMITIDA (preservar).");

        // Edicao COM Include: colecao carregada = autoritativa -> presente
        await using (var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1))
        {
            var cli = await db.Clientes.Include(c => c.Autorizacoes).FirstAsync(c => c.Id == clienteId);
            cli.Bloqueado = true;
            await db.SaveChangesAsync();
        }
        Assert.True(TemChave(await UltimoJsonDaOpAsync(clienteId, "U"), "autorizacoes"));
    }

    [Fact]
    public async Task EdicaoSoDeFilho_GeraOpDoPai_ComColecaoPresente()
    {
        var (_, clienteId) = await CriarClienteComFilhosAsync();
        long filaAntes;
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
            filaAntes = await db.SyncFila.Where(f => f.Tabela == "Clientes" && f.RegistroId == clienteId).CountAsync();

        // Padrao dos services: RemoveRange + re-add, SEM tocar o cabecalho
        await using (var db2 = _pg.CriarContexto(modo: "Edge", noCodigo: 1))
        {
            var cli = await db2.Clientes.Include(c => c.Autorizacoes).FirstAsync(c => c.Id == clienteId);
            db2.Set<ClienteAutorizacao>().RemoveRange(cli.Autorizacoes);
            cli.Autorizacoes.Clear();
            cli.Autorizacoes.Add(new ClienteAutorizacao { Nome = "ANA" });
            await db2.SaveChangesAsync();
        }

        await using var db3 = _pg.CriarContexto(aplicandoSync: true);
        var opsDepois = await db3.SyncFila.Where(f => f.Tabela == "Clientes" && f.RegistroId == clienteId).CountAsync();
        Assert.True(opsDepois > filaAntes,
            "EDICAO SO'-DE-FILHO NAO REPLICOU: o pai ficou Unchanged e nenhuma op saiu — o touch do " +
            "pai (fase 3) precisa promover o agregado pra Modified.");

        var json = await UltimoJsonDaOpAsync(clienteId, "U");
        Assert.True(TemChave(json, "autorizacoes"), "a op do touch precisa levar a colecao (carregada) como autoritativa");
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("autorizacoes").GetArrayLength());
    }
}
