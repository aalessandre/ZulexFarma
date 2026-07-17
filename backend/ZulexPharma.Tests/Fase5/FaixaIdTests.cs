using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Tests.Fase5;

/// <summary>
/// FASE 5b — decisao de reposicionamento da faixa de Id (achado CRITICO da revisao: colisao de PK
/// entre nos apos bootstrap). Testa a logica PURA que decide o RESTART da identity.
/// Faixa do no N: (offset, fimFaixa] = (N*1e9, (N+1)*1e9].
/// </summary>
public class FaixaIdTests
{
    private const long R = 1_000_000_000L;

    // no 2: offset 2e9, fim 3e9
    private const long Off = 2 * R;
    private const long Fim = 3 * R;

    [Fact]
    public void No_Saudavel_DentroDaFaixa_NaoMexe()
    {
        // sequence em 2e9+500, maior Id emitido 2e9+499 -> nao reposiciona
        Assert.Null(DatabaseSeeder.DecidirRestartSequence(Off, Fim, Off + 499, Off + 500));
    }

    [Fact]
    public void Restore_IdentityNaPosicaoDoHub_ReposicionaProMaxDaFaixa()
    {
        // dump trouxe rows do no 5 (nao contam) + rows do proprio no 2 (max 2e9+300); a identity
        // veio baixa (posicao do hub) -> RESTART pro topo da faixa do no 2.
        var alvo = DatabaseSeeder.DecidirRestartSequence(Off, Fim, Off + 300, 150);
        Assert.Equal(Off + 301, alvo);
    }

    [Fact]
    public void Restore_IdentityAlemDoFim_TrazDeVoltaProFaixa()
    {
        // identity herdada acima do fim da faixa (ex.: posicao 5e9 de outro no) -> volta pra faixa
        var alvo = DatabaseSeeder.DecidirRestartSequence(Off, Fim, Off + 300, 5 * R);
        Assert.Equal(Off + 301, alvo);
    }

    [Fact]
    public void NoNovo_SemRows_ComecaNoInicioDaFaixa()
    {
        // instalacao limpa: sem Id na faixa, sequence fresca (1)
        var alvo = DatabaseSeeder.DecidirRestartSequence(Off, Fim, 0, 1);
        Assert.Equal(Off + 1, alvo);
    }

    [Fact]
    public void IdentityAtrasDoMaxNaFaixa_AvancaSemReduzir()
    {
        // restore parcial: rows ate' 2e9+300 mas sequence em 2e9+100 -> avanca pra 2e9+301
        var alvo = DatabaseSeeder.DecidirRestartSequence(Off, Fim, Off + 300, Off + 100);
        Assert.Equal(Off + 301, alvo);
    }

    [Fact]
    public void IdentityAdianteNaFaixa_NaoReduz()
    {
        // sequence ja' a' frente do max (gaps por rollback) -> NAO reduzir (reuso ressuscitaria via lapide)
        Assert.Null(DatabaseSeeder.DecidirRestartSequence(Off, Fim, Off + 300, Off + 900));
    }

    [Fact]
    public void PosicaoExatamenteNoOffset_EhForaDaFaixa_Reposiciona()
    {
        // posicao == offset ainda esta' na faixa do no ANTERIOR (o proximo nextval daria offset, do vizinho)
        var alvo = DatabaseSeeder.DecidirRestartSequence(Off, Fim, 0, Off);
        Assert.Equal(Off + 1, alvo);
    }
}
