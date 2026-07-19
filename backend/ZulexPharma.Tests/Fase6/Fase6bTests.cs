using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.Clientes;
using ZulexPharma.Application.DTOs.Logs;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.Fase6;

/// <summary>Log no-op pra instanciar o ClienteService no teste.</summary>
internal sealed class LogNoop : ILogAcaoService
{
    public Task RegistrarAsync(string tela, string acao, string entidade, long registroId,
        Dictionary<string, string?>? anterior = null, Dictionary<string, string?>? novo = null) => Task.CompletedTask;
    public Task<List<LogAcaoListDto>> ListarPorRegistroAsync(string entidade, long registroId,
        DateTime? dataInicio = null, DateTime? dataFim = null) => Task.FromResult(new List<LogAcaoListDto>());
}

/// <summary>
/// FASE 6b — correcoes da revisao adversarial do b+c: A1 (diff-preserve no ClienteService — sem
/// churn/duplicacao) e M2 (D de pai referenciado faz retry, nao abandona).
/// </summary>
[Collection("pg")]
public class Fase6bTests
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    private static Guid G(long id) => new($"00000000-0000-0000-0000-{id:D12}");

    private readonly PostgresFixture _pg;
    public Fase6bTests(PostgresFixture pg) => _pg = pg;

    /// <summary>
    /// A1: editar um campo ESCALAR do cliente NAO pode recriar os filhos com Ids novos (churn) — o
    /// convenio mantem o MESMO Id. Sem isso, 2 nos editando o mesmo cliente convergiam com DUPLICATA.
    /// </summary>
    [Fact]
    public async Task DiffPreserve_EditarEscalar_PreservaOIdDoConvenio_SemChurn()
    {
        const long pessoaCli = 6_500_000_101, pessoaConv = 6_500_000_102, convenioId = 6_500_000_103, clienteId = 6_500_000_104, cvFilhoId = 6_500_000_105;
        await using (var seed = _pg.CriarContexto(modo: "Edge", noCodigo: 1))
        {
            seed.Pessoas.AddRange(
                new Pessoa { Id = pessoaCli, Tipo = "F", Nome = "CLI A1", CpfCnpj = "50500500501" },
                new Pessoa { Id = pessoaConv, Tipo = "F", Nome = "PESSOA CONV", CpfCnpj = "50500500502" });
            await seed.SaveChangesAsync();
            seed.Convenios.Add(new Convenio { Id = convenioId, PessoaId = pessoaConv });
            await seed.SaveChangesAsync();
            var cli = new Cliente { Id = clienteId, PessoaId = pessoaCli };
            cli.Convenios.Add(new ClienteConvenio { Id = cvFilhoId, ConvenioId = convenioId, Matricula = "M1" });
            seed.Clientes.Add(cli);
            await seed.SaveChangesAsync();
        }

        // Edita SO' um escalar (LimiteCredito), mantendo o MESMO convenio no dto
        await using (var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1))
        {
            var svc = new ClienteService(db, new LogNoop());
            var dto = new ClienteFormDto
            {
                Tipo = "F", Nome = "CLI A1", CpfCnpj = "50500500501", LimiteCredito = 99,
                Convenios = { new ClienteConvenioDto { ConvenioId = convenioId, Matricula = "M1" } }
            };
            await svc.AtualizarAsync(clienteId, dto);
        }

        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            var filhos = await db.Set<ClienteConvenio>().Where(c => c.ClienteId == clienteId).ToListAsync();
            Assert.Single(filhos);
            Assert.Equal(cvFilhoId, filhos[0].Id); // ID PRESERVADO — nao recriou (sem churn, sem duplicacao concorrente)
        }
    }

    /// <summary>A1 (diff): tirar o convenio do dto o REMOVE; trocar por outro adiciona/remove certo.</summary>
    [Fact]
    public async Task DiffPreserve_RemoverEAdicionar_ReconciliaCerto()
    {
        // Convenio.PessoaId e' UNIQUE -> cada convenio precisa de uma pessoa distinta.
        const long pessoaCli = 6_500_000_111, pessoaConv1 = 6_500_000_112, pessoaConv2 = 6_500_000_117, conv1 = 6_500_000_113, conv2 = 6_500_000_114, clienteId = 6_500_000_115, cv1Filho = 6_500_000_116;
        await using (var seed = _pg.CriarContexto(modo: "Edge", noCodigo: 1))
        {
            seed.Pessoas.AddRange(
                new Pessoa { Id = pessoaCli, Tipo = "F", Nome = "CLI A1b", CpfCnpj = "51500500501" },
                new Pessoa { Id = pessoaConv1, Tipo = "F", Nome = "PESSOA CONV b1", CpfCnpj = "51500500502" },
                new Pessoa { Id = pessoaConv2, Tipo = "F", Nome = "PESSOA CONV b2", CpfCnpj = "51500500503" });
            await seed.SaveChangesAsync();
            seed.Convenios.AddRange(new Convenio { Id = conv1, PessoaId = pessoaConv1 }, new Convenio { Id = conv2, PessoaId = pessoaConv2 });
            await seed.SaveChangesAsync();
            var cli = new Cliente { Id = clienteId, PessoaId = pessoaCli };
            cli.Convenios.Add(new ClienteConvenio { Id = cv1Filho, ConvenioId = conv1, Matricula = "M1" });
            seed.Clientes.Add(cli);
            await seed.SaveChangesAsync();
        }

        await using (var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1))
        {
            var svc = new ClienteService(db, new LogNoop());
            // dto agora tem SO' o conv2 (tirou o conv1)
            var dto = new ClienteFormDto
            {
                Tipo = "F", Nome = "CLI A1b", CpfCnpj = "51500500501",
                Convenios = { new ClienteConvenioDto { ConvenioId = conv2, Matricula = "M2" } }
            };
            await svc.AtualizarAsync(clienteId, dto);
        }

        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            var filhos = await db.Set<ClienteConvenio>().Where(c => c.ClienteId == clienteId).ToListAsync();
            Assert.Single(filhos);
            Assert.Equal(conv2, filhos[0].ConvenioId); // conv1 removido, conv2 adicionado
        }
    }

    /// <summary>
    /// M2: apos o fix FURO 1 (2a FK Restrict), um D de Convenio AINDA referenciado por ClienteConvenio
    /// neste no da' 23503 -> tem que ser PrecisaRetry (nao "Erro" teto 5 que ABANDONA -> divergencia).
    /// </summary>
    [Fact]
    public async Task M2_DeleteDeConvenioReferenciado_EhPrecisaRetry_NaoAbandona()
    {
        var t0 = new DateTime(2026, 7, 6, 10, 0, 0);
        const long pessoaCli = 6_500_000_121, pessoaConv = 6_500_000_122, convenioId = 6_500_000_123, clienteId = 6_500_000_124, cvFilho = 6_500_000_125;
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            await SyncApplicator.AplicarOperacaoAsync(db, "Pessoas", "I", pessoaCli, JsonSerializer.Serialize(new Pessoa { Id = pessoaCli, Tipo = "F", Nome = "M2 CLI", CpfCnpj = "52500500501", CriadoEm = t0, NoOrigemId = 3, SyncGuid = G(pessoaCli), Ativo = true }, _json), t0, 3);
            await SyncApplicator.AplicarOperacaoAsync(db, "Pessoas", "I", pessoaConv, JsonSerializer.Serialize(new Pessoa { Id = pessoaConv, Tipo = "F", Nome = "M2 CONV", CpfCnpj = "52500500502", CriadoEm = t0, NoOrigemId = 3, SyncGuid = G(pessoaConv), Ativo = true }, _json), t0, 3);
            await SyncApplicator.AplicarOperacaoAsync(db, "Convenios", "I", convenioId, JsonSerializer.Serialize(new Convenio { Id = convenioId, PessoaId = pessoaConv, CriadoEm = t0, NoOrigemId = 3, SyncGuid = G(convenioId), Ativo = true }, _json), t0, 3);
            await SyncApplicator.AplicarOperacaoAsync(db, "Clientes", "I", clienteId, JsonSerializer.Serialize(new Cliente { Id = clienteId, PessoaId = pessoaCli, CriadoEm = t0, NoOrigemId = 3, SyncGuid = G(clienteId), Ativo = true }, _json), t0, 3);
            await SyncApplicator.AplicarOperacaoAsync(db, "ClienteConvenios", "I", cvFilho, JsonSerializer.Serialize(new ClienteConvenio { Id = cvFilho, ClienteId = clienteId, ConvenioId = convenioId, Matricula = "M", CriadoEm = t0, NoOrigemId = 3, AtualizadoPorNoId = 3, SyncGuid = G(cvFilho), Ativo = true }, _json), t0, 3);
        }

        // D do Convenio referenciado -> Restrict -> 23503 -> PrecisaRetry (NAO abandona)
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            var res = await SyncApplicator.AplicarOperacaoAsync(db, "Convenios", "D", convenioId, null, t0.AddHours(1), 3);
            Assert.Equal(ResultadoSync.PrecisaRetry, res);
        }

        // removido o filho, o retry do D aplica
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            await SyncApplicator.AplicarOperacaoAsync(db, "ClienteConvenios", "D", cvFilho, null, t0.AddHours(2), 3);
            var res = await SyncApplicator.AplicarOperacaoAsync(db, "Convenios", "D", convenioId, null, t0.AddHours(3), 3);
            Assert.Equal(ResultadoSync.Aplicado, res);
        }
    }
}
