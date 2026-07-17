using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.BugsAtivos;

/// <summary>
/// Dois bugs ativos de ESCOPO/CLASSIFICACAO (plano §2, curas fases 2 e 4).
/// </summary>
[Collection("pg")]
public class EscopoTests
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly PostgresFixture _pg;
    public EscopoTests(PostgresFixture pg) => _pg = pg;

    /// <summary>
    /// BUG (cura fase 2, decisao A4): Configuracoes NAO esta em _tabelasSemSync e esta no dicionario
    /// do applicator — replica como GLOBAL. E o CURSOR do pull ('sync.ultimo.id.recebido') vive nessa
    /// tabela: o estado de sync de um no pode vazar pra outro e sobrescrever o cursor alheio.
    /// VERMELHO ate' a fase 2 (Configuracoes em _tabelasSemSync + estado do sync em tabela propria).
    /// </summary>
    [Fact]
    public async Task Configuracao_NaoDeveEntrarNaSyncFila()
    {
        await using var db = _pg.CriarContexto(modo: "Edge", noCodigo: 1);
        var config = new Configuracao { Chave = $"teste.fase0.{Guid.NewGuid():N}", Valor = "1" };
        db.Configuracoes.Add(config);
        await db.SaveChangesAsync();

        var enfileirada = await db.SyncFila.AnyAsync(s => s.Tabela == "Configuracoes" && s.RegistroId == config.Id);
        Assert.False(enfileirada,
            "VAZAMENTO DE ESTADO: Configuracao entrou na SyncFila (replica como GLOBAL hoje). O cursor " +
            "do pull ('sync.ultimo.id.recebido') mora nessa tabela — replicar Configuracao pode " +
            "sobrescrever o cursor de OUTRO no. Cura (fase 2): Configuracoes vira INFRA (_tabelasSemSync) " +
            "e o estado do sync sai pra tabela propria (SyncEstadoLocal).");
    }

    /// <summary>
    /// BUG (cura fase 4): MovimentoEstoque e' um LEDGER (fato imutavel), mas o applicator o trata
    /// como snapshot LWW — um "U" remoto sobrescreve a linha inteira (quantidade/saldo reescritos).
    /// LWW de dois incrementos perde um deles; saldo vira loteria de relogio.
    /// VERMELHO ate' a fase 4 (ledger append-only: applicator rejeita U/D em movimento).
    /// </summary>
    [Fact]
    public async Task MovimentoEstoque_EhLedger_NaoDeveAceitarUpdateRemoto()
    {
        var t0 = new DateTime(2026, 7, 1, 10, 0, 0);
        var t1 = new DateTime(2026, 7, 1, 12, 0, 0);
        const long id = 3_000_000_120;

        var movimento = new MovimentoEstoque
        {
            Id = id, ProdutoId = 3_000_000_900, FilialId = 1, Data = t0,
            Tipo = TipoMovimentoEstoque.Compra, Quantidade = 10, SaldoApos = 10,
            CriadoEm = t0, NoOrigemId = 3, Ativo = true
        };
        var resInsert = await Aplicar("I", id, JsonSerializer.Serialize(movimento, _json), t0);
        Assert.Equal(ResultadoSync.Aplicado, resInsert);

        movimento.Quantidade = 99; // reescrita de historico — nunca deveria ser aceita
        movimento.SaldoApos = 99;
        movimento.AtualizadoEm = t1;
        var resUpdate = await Aplicar("U", id, JsonSerializer.Serialize(movimento, _json), t1);

        Assert.True(resUpdate is ResultadoSync.Stale or ResultadoSync.Conflito or ResultadoSync.TipoDesconhecido,
            $"LEDGER MUTAVEL: o applicator aceitou UPDATE remoto em MovimentoEstoque (resultado: {resUpdate}) " +
            "— historico de estoque reescrito via LWW; dois incrementos concorrentes perdem um. " +
            "Cura (fase 4): movimento e' append-only — applicator aceita so' 'I' (dedup por Id); " +
            "correcao e' movimento de AJUSTE novo, nunca editar o fato.");
    }

    private async Task<ResultadoSync> Aplicar(string op, long id, string json, DateTime opCriadoEm)
    {
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        return await SyncApplicator.AplicarOperacaoAsync(db, "MovimentosEstoque", op, id, json, opCriadoEm, noOrigemId: 3);
    }
}
