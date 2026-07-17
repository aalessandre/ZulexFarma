using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.Fase3;

/// <summary>
/// FASE 3b — testes dos achados da revisao adversarial (2 criticos + 3 altos + medios).
/// </summary>
[Collection("pg")]
public class Fase3bTests
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly PostgresFixture _pg;
    public Fase3bTests(PostgresFixture pg) => _pg = pg;

    private static Guid GuidDe(long id) => new($"00000000-0000-0000-0000-{id:D12}");

    /// <summary>
    /// C1 (CRITICO): reconciliacao de filhos com FK faltando NAO pode commitar os DELETEs sem os
    /// INSERTs — a venda ficaria sem NENHUM item no destino ate' o retry (ou pra sempre).
    /// </summary>
    [Fact]
    public async Task ReconciliacaoComFkFaltando_NaoApagaOsFilhosAntigos()
    {
        var t0 = new DateTime(2026, 7, 4, 10, 0, 0);
        var t1 = new DateTime(2026, 7, 4, 12, 0, 0);
        const long pessoaId = 3_000_000_601;
        const long clienteId = 3_000_000_602;
        const long convenioBomId = 3_000_000_603;
        const long convenioInexistenteId = 3_000_000_699; // NUNCA criado -> 23503 no insert do filho

        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            var pessoa = new Pessoa { Id = pessoaId, Tipo = "F", Nome = "C1 PESSOA", CpfCnpj = "11122233344", CriadoEm = t0, NoOrigemId = 3, SyncGuid = GuidDe(pessoaId), Ativo = true };
            await SyncApplicator.AplicarOperacaoAsync(db, "Pessoas", "I", pessoaId, JsonSerializer.Serialize(pessoa, _json), t0, 3);
            var convenio = new Convenio { Id = convenioBomId, PessoaId = pessoaId, CriadoEm = t0, NoOrigemId = 3, SyncGuid = GuidDe(convenioBomId), Ativo = true };
            Assert.Equal(ResultadoSync.Aplicado, await SyncApplicator.AplicarOperacaoAsync(
                db, "Convenios", "I", convenioBomId, JsonSerializer.Serialize(convenio, _json), t0, 3));
        }

        // V1: cliente com 1 convenio VALIDO
        var v1 = new Cliente { Id = clienteId, PessoaId = pessoaId, CriadoEm = t0, NoOrigemId = 3, SyncGuid = GuidDe(clienteId), Ativo = true };
        v1.Convenios.Add(new ClienteConvenio { Id = 3_000_000_604, ClienteId = clienteId, ConvenioId = convenioBomId, Matricula = "M1" });
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
            Assert.Equal(ResultadoSync.Aplicado, await SyncApplicator.AplicarOperacaoAsync(
                db, "Clientes", "I", clienteId, JsonSerializer.Serialize(v1, _json), t0, 3));

        // V2 (edicao com RemoveRange+re-add): filho novo aponta pra convenio que AINDA nao replicou
        var v2 = new Cliente { Id = clienteId, PessoaId = pessoaId, CriadoEm = t0, AtualizadoEm = t1, NoOrigemId = 3, SyncGuid = GuidDe(clienteId), Ativo = true };
        v2.Convenios.Add(new ClienteConvenio { Id = 3_000_000_605, ClienteId = clienteId, ConvenioId = convenioInexistenteId, Matricula = "M2" });
        ResultadoSync resV2;
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
            resV2 = await SyncApplicator.AplicarOperacaoAsync(
                db, "Clientes", "U", clienteId, JsonSerializer.Serialize(v2, _json), t1, 3);

        Assert.Equal(ResultadoSync.PrecisaRetry, resV2);
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            var filhos = await db.Set<ClienteConvenio>().Where(c => c.ClienteId == clienteId).ToListAsync();
            Assert.True(filhos.Count == 1 && filhos[0].Id == 3_000_000_604,
                $"C1: o DELETE-MISSING commitou sem os INSERTs — cliente ficou com {filhos.Count} convenios " +
                "(o antigo foi apagado e o novo falhou na FK). O savepoint tem que reverter o bloco INTEIRO.");
        }
    }

    /// <summary>
    /// C2 (CRITICO): contexto COMPARTILHADO do lote — a op seguinte nao pode comparar o LWW contra
    /// a instancia STALE do tracker (escrita concorrente de outro request ficaria invisivel).
    /// </summary>
    [Fact]
    public async Task LoteComContextoCompartilhado_NaoUsaVersaoStaleDoTracker()
    {
        var t0 = new DateTime(2026, 7, 4, 10, 0, 0);
        var t2 = new DateTime(2026, 7, 4, 12, 0, 0);
        var t5 = new DateTime(2026, 7, 4, 15, 0, 0);
        const long id = 3_000_000_610;

        Fabricante Payload(string nome, DateTime? ts) => new()
        { Id = id, Nome = nome, CriadoEm = t0, AtualizadoEm = ts, NoOrigemId = 3, SyncGuid = GuidDe(id), Ativo = true };

        // "Lote" da loja 1 no MESMO contexto (como o /enviar real)
        await using var dbLote = _pg.CriarContexto(aplicandoSync: true);
        await SyncApplicator.AplicarOperacaoAsync(dbLote, "Fabricantes", "I", id, JsonSerializer.Serialize(Payload("V-10h", null), _json), t0, 1);

        // Request CONCORRENTE de outra loja aplica versao MAIS NOVA (contexto proprio)
        await using (var dbOutro = _pg.CriarContexto(aplicandoSync: true))
            await SyncApplicator.AplicarOperacaoAsync(dbOutro, "Fabricantes", "U", id, JsonSerializer.Serialize(Payload("V-15h", t5), _json), t5, 2);

        // O lote da loja 1 continua: op de 12h. Contra o BANCO (15h) e' Stale; contra o TRACKER
        // stale (10h) "venceria" e sobrescreveria a versao de 15h — divergencia permanente.
        var res = await SyncApplicator.AplicarOperacaoAsync(dbLote, "Fabricantes", "U", id, JsonSerializer.Serialize(Payload("V-12h", t2), _json), t2, 1);

        Assert.Equal(ResultadoSync.Stale, res);
        await using var dbCheck = _pg.CriarContexto(aplicandoSync: true);
        Assert.Equal("V-15h", (await dbCheck.Fabricantes.FindAsync(id))!.Nome);
    }

    /// <summary>A1 (ALTO): U com colecoes OMITIDAS nao pode RECRIAR agregado sobre lapide (nasceria sem filhos).</summary>
    [Fact]
    public async Task RecriacaoSobreLapide_ComColecaoOmitida_VaiPraQuarentena()
    {
        var t0 = new DateTime(2026, 7, 4, 10, 0, 0);
        var tD = new DateTime(2026, 7, 4, 11, 0, 0);
        var tU = new DateTime(2026, 7, 4, 12, 0, 0);
        const long pessoaId = 3_000_000_620;
        const long clienteId = 3_000_000_621;

        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            var pessoa = new Pessoa { Id = pessoaId, Tipo = "F", Nome = "A1 PESSOA", CpfCnpj = "22233344455", CriadoEm = t0, NoOrigemId = 3, SyncGuid = GuidDe(pessoaId), Ativo = true };
            await SyncApplicator.AplicarOperacaoAsync(db, "Pessoas", "I", pessoaId, JsonSerializer.Serialize(pessoa, _json), t0, 3);
            var v1 = new Cliente { Id = clienteId, PessoaId = pessoaId, CriadoEm = t0, NoOrigemId = 3, SyncGuid = GuidDe(clienteId), Ativo = true };
            v1.Autorizacoes.Add(new ClienteAutorizacao { Id = 3_000_000_622, ClienteId = clienteId, Nome = "MARIA" });
            await SyncApplicator.AplicarOperacaoAsync(db, "Clientes", "I", clienteId, JsonSerializer.Serialize(v1, _json), t0, 3);
            await SyncApplicator.AplicarOperacaoAsync(db, "Clientes", "D", clienteId, null, tD, 3); // morte (cascade leva filhos)
        }

        // U MAIS NOVO que a lapide, mas com TODAS as colecoes omitidas (origem editou sem Include)
        var v2 = new Cliente { Id = clienteId, PessoaId = pessoaId, CriadoEm = t0, AtualizadoEm = tU, NoOrigemId = 3, SyncGuid = GuidDe(clienteId), Ativo = true };
        var node = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(v2, _json))!.AsObject();
        foreach (var chave in new[] { "convenios", "autorizacoes", "descontos", "usosContinuos", "bloqueios" })
            node.Remove(chave);

        ResultadoSync res;
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
            res = await SyncApplicator.AplicarOperacaoAsync(db, "Clientes", "U", clienteId, node.ToJsonString(_json), tU, 3);

        Assert.Equal(ResultadoSync.RecriacaoSemGrafo, res);
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
            Assert.Null(await db.Clientes.FindAsync(clienteId)); // nao recriou capenga
    }

    /// <summary>A3 (ALTO): linha criada NO HUB (origem 0) tem o guard de identidade LIGADO.</summary>
    [Fact]
    public async Task LinhaCriadaNoHub_TemGuardDeIdentidade()
    {
        long id;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["No:Modo"] = "Hub", ["No:Codigo"] = "0" }).Build();
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_pg.ConnTeste).Options;
        await using (var db = new AppDbContext(opts, null, config))
        {
            var fab = new Fabricante { Nome = "CRIADO NO HUB (GUARD)" };
            db.Fabricantes.Add(fab);
            await db.SaveChangesAsync();
            id = fab.Id;
            Assert.Equal(0, fab.NoOrigemId); // hub carimba 0 (nao null) — null desligava o guard
        }

        var impostor = new Fabricante
        {
            Id = id, Nome = "IMPOSTOR", CriadoEm = new DateTime(2026, 7, 4, 10, 0, 0),
            AtualizadoEm = new DateTime(2026, 7, 4, 12, 0, 0), NoOrigemId = 7, SyncGuid = Guid.NewGuid(), Ativo = true
        };
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            var res = await SyncApplicator.AplicarOperacaoAsync(db, "Fabricantes", "U", id,
                JsonSerializer.Serialize(impostor, _json), impostor.AtualizadoEm!.Value, 7);
            Assert.Equal(ResultadoSync.ColisaoIdentidade, res);
        }
    }

    /// <summary>M2 (kiosk): filho adicionado DIRETO no DbSet, pai sem Include — a op "U" tem que levar a colecao.</summary>
    [Fact]
    public async Task FilhoAdicionadoDiretoNoDbSet_OpDoPaiLevaAColecaoCompleta()
    {
        long clienteId;
        await using (var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1))
        {
            var pessoa = new Pessoa { Tipo = "F", Nome = $"KIOSK {Guid.NewGuid():N}", CpfCnpj = Guid.NewGuid().ToString("N")[..11] };
            db.Pessoas.Add(pessoa);
            await db.SaveChangesAsync();
            var cliente = new Cliente { PessoaId = pessoa.Id };
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync(); // pai persiste SEM filhos
            clienteId = cliente.Id;

            // padrao kiosk: filho adicionado DIRETO no DbSet, pai nao tocado, colecao nao carregada
            db.Set<ClienteAutorizacao>().Add(new ClienteAutorizacao { ClienteId = clienteId, Nome = "KIOSK" });
            await db.SaveChangesAsync();
        }

        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            var opU = await db.SyncFila.Where(f => f.Tabela == "Clientes" && f.RegistroId == clienteId && f.Operacao == "U")
                .OrderByDescending(f => f.Id).FirstOrDefaultAsync();
            Assert.True(opU != null, "mudanca so'-de-filho tem que gerar op U do pai (touch)");
            using var doc = JsonDocument.Parse(opU!.DadosJson!);
            Assert.True(doc.RootElement.TryGetProperty("autorizacoes", out var arr) && arr.GetArrayLength() == 1,
                "M2: sem o force-load, a op U sai com a colecao OMITIDA e o filho novo NUNCA viaja pro hub");
        }
    }
}
