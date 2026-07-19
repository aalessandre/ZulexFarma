using Microsoft.EntityFrameworkCore;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.Fase1;

/// <summary>
/// FASE 1: credencial por no + anti-gemeo. Verdes apos a implementacao — provam o fluxo completo
/// de cadastro/handshake e os caminhos de rejeicao (chave errada, no desconhecido, gemeo, suspenso).
/// </summary>
[Collection("pg")]
public class SyncNoAuthTests
{
    private readonly PostgresFixture _pg;
    public SyncNoAuthTests(PostgresFixture pg) => _pg = pg;

    private async Task<(int codigo, string chave, Guid instancia)> CadastrarNoAsync(int codigo, string status = "Provisionando")
    {
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        await db.SyncNos.Where(n => n.NoCodigo == codigo).ExecuteDeleteAsync();
        var chave = SyncNoAuth.GerarChave();
        db.SyncNos.Add(new SyncNo { NoCodigo = codigo, Nome = $"Teste {codigo}", Status = status, ChaveHash = SyncNoAuth.HashChave(chave) });
        await db.SaveChangesAsync();
        return (codigo, chave, Guid.NewGuid());
    }

    [Fact]
    public async Task Handshake_FluxoCompleto_CravaInstanciaEDetectaGemeo()
    {
        var (codigo, chave, instancia) = await CadastrarNoAsync(9101);

        // 1o handshake: crava a instancia e ativa o no
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            var (res, no) = await SyncNoAuth.ValidarHandshakeAsync(db, codigo, instancia, chave, "1.0.0");
            Assert.Equal(HandshakeResultado.Ok, res);
            Assert.Equal(instancia, no!.InstanciaUid);
            Assert.Equal("Ativo", no.Status);
        }

        // Mesma instancia: continua Ok
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            var (res, _) = await SyncNoAuth.ValidarHandshakeAsync(db, codigo, instancia, chave, "1.0.0");
            Assert.Equal(HandshakeResultado.Ok, res);
        }

        // OUTRA instancia com o mesmo codigo (clone/reinstalacao sem reset) = GEMEO
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            var (res, _) = await SyncNoAuth.ValidarHandshakeAsync(db, codigo, Guid.NewGuid(), chave, "1.0.0");
            Assert.Equal(HandshakeResultado.Gemeo, res);
        }
    }

    [Fact]
    public async Task Handshake_ChaveErrada_OuNoDesconhecido_NaoVazaExistencia()
    {
        var (codigo, _, instancia) = await CadastrarNoAsync(9102);
        await using var db = _pg.CriarContexto(aplicandoSync: true);

        var (resChaveErrada, _) = await SyncNoAuth.ValidarHandshakeAsync(db, codigo, instancia, "chave-errada", null);
        Assert.Equal(HandshakeResultado.CredencialInvalida, resChaveErrada);

        var (resDesconhecido, _) = await SyncNoAuth.ValidarHandshakeAsync(db, 98765, instancia, "qualquer", null);
        Assert.Equal(HandshakeResultado.CredencialInvalida, resDesconhecido); // mesma resposta: nao vaza existencia
    }

    [Fact]
    public async Task Handshake_NoSuspenso_Rejeitado()
    {
        var (codigo, chave, instancia) = await CadastrarNoAsync(9103, status: "Suspenso");
        await using var db = _pg.CriarContexto(aplicandoSync: true);
        var (res, _) = await SyncNoAuth.ValidarHandshakeAsync(db, codigo, instancia, chave, null);
        Assert.Equal(HandshakeResultado.NoInativo, res);
    }

    [Fact]
    public async Task InstanciaUid_Local_EPersistente()
    {
        Guid primeira;
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
            primeira = await SyncNoAuth.ObterOuCriarInstanciaUidAsync(db);
        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            var segunda = await SyncNoAuth.ObterOuCriarInstanciaUidAsync(db);
            Assert.Equal(primeira, segunda); // mesma instalacao = mesma identidade
        }
    }
}
