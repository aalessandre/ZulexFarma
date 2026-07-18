using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.Fase6;

/// <summary>
/// FASE 6 (b+c) — as 5 folhas de Cliente promovidas a BaseEntity replicam sozinhas: uniao sob insert
/// concorrente, delete convergente, LWW por filho; FURO 1 curado; invariante de boot morde.
/// </summary>
[Collection("pg")]
public class PromocaoFilhosClienteTests
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    private static Guid G(long id) => new($"00000000-0000-0000-0000-{id:D12}");

    private readonly PostgresFixture _pg;
    public PromocaoFilhosClienteTests(PostgresFixture pg) => _pg = pg;

    private async Task<ResultadoSync> Aplicar(string tabela, string op, long id, object? payload, long noOrigem, DateTime ts)
    {
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        return await SyncApplicator.AplicarOperacaoAsync(db, tabela, op, id,
            payload == null ? null : JsonSerializer.Serialize(payload, _json), ts, noOrigem);
    }

    private async Task SemearPessoaConvenioClienteAsync(long pessoaId, long convenioId, long clienteId, DateTime t0)
    {
        var pessoa = new Pessoa { Id = pessoaId, Tipo = "F", Nome = "F6", CpfCnpj = pessoaId.ToString("D11"), CriadoEm = t0, NoOrigemId = 3, SyncGuid = G(pessoaId), Ativo = true };
        Assert.Equal(ResultadoSync.Aplicado, await Aplicar("Pessoas", "I", pessoaId, pessoa, 3, t0));
        var convenio = new Convenio { Id = convenioId, PessoaId = pessoaId, CriadoEm = t0, NoOrigemId = 3, SyncGuid = G(convenioId), Ativo = true };
        Assert.Equal(ResultadoSync.Aplicado, await Aplicar("Convenios", "I", convenioId, convenio, 3, t0));
        var cliente = new Cliente { Id = clienteId, PessoaId = pessoaId, CriadoEm = t0, NoOrigemId = 3, SyncGuid = G(clienteId), Ativo = true };
        Assert.Equal(ResultadoSync.Aplicado, await Aplicar("Clientes", "I", clienteId, cliente, 3, t0));
    }

    private static ClienteConvenio Cv(long id, long clienteId, long convenioId, string matricula, long no, DateTime t0, DateTime? atualizado = null) => new()
    { Id = id, ClienteId = clienteId, ConvenioId = convenioId, Matricula = matricula, CriadoEm = t0, AtualizadoEm = atualizado, NoOrigemId = no, AtualizadoPorNoId = no, SyncGuid = G(id), Ativo = true };

    [Fact]
    public async Task Uniao_DoisNosAdicionamConvenios_AmbosSobrevivem_MesmoComUdoPai()
    {
        var t0 = new DateTime(2026, 7, 6, 10, 0, 0);
        var t1 = new DateTime(2026, 7, 6, 12, 0, 0);
        const long pessoaId = 6_000_000_101, convenioId = 6_000_000_102, clienteId = 6_000_000_103;
        await SemearPessoaConvenioClienteAsync(pessoaId, convenioId, clienteId, t0);

        // no A (origem 2) add cv2; no B (origem 3) add cv3 — ops INDEPENDENTES (cada filho e' BaseEntity)
        Assert.Equal(ResultadoSync.Aplicado, await Aplicar("ClienteConvenios", "I", 2_000_000_201, Cv(2_000_000_201, clienteId, convenioId, "A", 2, t0), 2, t0));
        Assert.Equal(ResultadoSync.Aplicado, await Aplicar("ClienteConvenios", "I", 3_000_000_301, Cv(3_000_000_301, clienteId, convenioId, "B", 3, t0), 3, t0));

        // U do Cliente (no mundo POCO isto delete-missing'ava os convenios) — agora NAO os toca
        var clienteU = new Cliente { Id = clienteId, PessoaId = pessoaId, CriadoEm = t0, AtualizadoEm = t1, NoOrigemId = 3, AtualizadoPorNoId = 3, SyncGuid = G(clienteId), Ativo = true, Bloqueado = true };
        await Aplicar("Clientes", "U", clienteId, clienteU, 3, t1);

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var convenios = await db.Set<ClienteConvenio>().Where(c => c.ClienteId == clienteId).Select(c => c.Id).OrderBy(x => x).ToListAsync();
        Assert.Equal(new List<long> { 2_000_000_201, 3_000_000_301 }, convenios); // UNIAO — nada apagado pelo LWW do pai
    }

    [Fact]
    public async Task Delete_DoClientePorCascata_GeraDMaisLapide_NoFilho()
    {
        var t0 = new DateTime(2026, 7, 6, 10, 0, 0);
        var tD = new DateTime(2026, 7, 6, 13, 0, 0);
        const long filhoId = 2_000_000_211;
        // Aplica um ClienteConvenio, depois um D dele (como o cascade do Cliente geraria) -> lapide.
        var t1 = new DateTime(2026, 7, 6, 10, 0, 0);
        const long pessoaId = 6_000_000_111, convenioId = 6_000_000_112, clienteId = 6_000_000_113;
        await SemearPessoaConvenioClienteAsync(pessoaId, convenioId, clienteId, t1);
        Assert.Equal(ResultadoSync.Aplicado, await Aplicar("ClienteConvenios", "I", filhoId, Cv(filhoId, clienteId, convenioId, "X", 3, t0), 3, t0));

        Assert.Equal(ResultadoSync.Aplicado, await Aplicar("ClienteConvenios", "D", filhoId, null, 3, tD));
        // um I VELHO do mesmo filho (reenvio atrasado) NAO ressuscita — barrado pela lapide
        var resVelho = await Aplicar("ClienteConvenios", "I", filhoId, Cv(filhoId, clienteId, convenioId, "X", 3, t0), 3, t0);
        Assert.Equal(ResultadoSync.Stale, resVelho);

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        Assert.Null(await db.Set<ClienteConvenio>().FindAsync(filhoId));
        Assert.True(await db.SyncTombstones.AnyAsync(t => t.Tabela == "ClienteConvenios" && t.RegistroId == filhoId));
    }

    [Fact]
    public async Task Furo1_HardDeleteDoConvenioVinculado_EhRestrict_NaoCascataSemLapide()
    {
        var t0 = new DateTime(2026, 7, 6, 10, 0, 0);
        const long pessoaId = 6_000_000_121, convenioId = 6_000_000_122, clienteId = 6_000_000_123;
        await SemearPessoaConvenioClienteAsync(pessoaId, convenioId, clienteId, t0);
        Assert.Equal(ResultadoSync.Aplicado, await Aplicar("ClienteConvenios", "I", 2_000_000_221, Cv(2_000_000_221, clienteId, convenioId, "X", 3, t0), 3, t0));

        // hard-delete do Convenio referenciado: com a 2a FK = Restrict, o banco RECUSA (nao cascateia
        // o filho sem lapide). Sem o fix, isto cascatearia em silencio e ressuscitaria depois.
        await using var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1);
        var conv = await db.Convenios.FindAsync(convenioId);
        db.Convenios.Remove(conv!);
        await Assert.ThrowsAsync<DbUpdateException>(async () => await db.SaveChangesAsync());
    }

    [Fact]
    public async Task LwwDoFilho_EdicaoConcorrente_MaiorVersaoVence()
    {
        var t0 = new DateTime(2026, 7, 6, 10, 0, 0);
        var tVelha = new DateTime(2026, 7, 6, 11, 0, 0);
        var tNova = new DateTime(2026, 7, 6, 12, 0, 0);
        const long pessoaId = 6_000_000_131, convenioId = 6_000_000_132, clienteId = 6_000_000_133, filhoId = 2_000_000_231;
        await SemearPessoaConvenioClienteAsync(pessoaId, convenioId, clienteId, t0);
        await Aplicar("ClienteConvenios", "I", filhoId, Cv(filhoId, clienteId, convenioId, "V0", 3, t0), 3, t0);

        await Aplicar("ClienteConvenios", "U", filhoId, Cv(filhoId, clienteId, convenioId, "NOVA", 2, t0, tNova), 2, tNova);
        var resVelha = await Aplicar("ClienteConvenios", "U", filhoId, Cv(filhoId, clienteId, convenioId, "VELHA", 1, t0, tVelha), 1, tVelha);
        Assert.Equal(ResultadoSync.Stale, resVelha);

        await using var db = _pg.CriarContexto(aplicandoSync: true);
        Assert.Equal("NOVA", (await db.Set<ClienteConvenio>().FindAsync(filhoId))!.Matricula);
    }

    [Fact]
    public void Invariante_ModeloReal_Passa()
    {
        using var db = _pg.CriarContexto(aplicandoSync: true);
        var ex = Record.Exception(() => SyncRegistry.ValidarModelo(db.Model));
        Assert.True(ex == null, $"modelo real deveria passar apos b+c:\n{ex?.Message}");
    }

    [Fact]
    public void Invariante_ColecaoPocoDeGlobalForaDaWhitelist_DerrubaOBoot()
    {
        using var db = _pg.CriarContexto(aplicandoSync: true);
        // tira uma coleção POCO real da whitelist -> o boot tem que acusar NOMINALMENTE
        var removido = SyncRegistry.ColecoesPocoSubstituicaoAceitas.Remove(typeof(ConvenioDesconto));
        try
        {
            Assert.True(removido, "ConvenioDesconto deveria estar na whitelist");
            var ex = Assert.Throws<InvalidOperationException>(() => SyncRegistry.ValidarModelo(db.Model));
            Assert.Contains("ConvenioDesconto", ex.Message);
        }
        finally
        {
            SyncRegistry.ColecoesPocoSubstituicaoAceitas.Add(typeof(ConvenioDesconto));
        }
    }
}
