using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.Compras;
using ZulexPharma.Application.DTOs.Logs;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.Tests.BugsAtivos;

/// <summary>Log stub que FLUSHA (SaveChanges) como o LogAcaoService real, sem depender de user context.
/// No codigo ANTIGO (sem transacao) e' isto que commitava o item por produto -> compra pela metade.
/// No codigo NOVO exercita o SaveChanges dos logs DEPOIS do commit (tx ja' fechada).</summary>
internal sealed class LogFlush : ILogAcaoService
{
    private readonly AppDbContext _db;
    public LogFlush(AppDbContext db) => _db = db;
    public Task RegistrarAsync(string tela, string acao, string entidade, long registroId,
        Dictionary<string, string?>? anterior = null, Dictionary<string, string?>? novo = null)
        => _db.SaveChangesAsync();
    public Task<List<LogAcaoListDto>> ListarPorRegistroAsync(string entidade, long registroId,
        DateTime? dataInicio = null, DateTime? dataFim = null) => Task.FromResult(new List<LogAcaoListDto>());
}

/// <summary>Lote stub que ESTOURA na entrada — simula uma falha DEPOIS de o item ja' ter mexido no
/// estoque/movimento (o RegistrarEntradaAsync e' chamado no fim do processamento do item).</summary>
internal sealed class LoteExplode : IProdutoLoteService
{
    public Task<ProdutoLote> RegistrarEntradaAsync(long produtoId, long filialId, string numeroLote,
        DateTime? dataFabricacao, DateTime? dataValidade, decimal quantidade, TipoMovimentoLote tipo,
        string? registroMs = null, long? fornecedorId = null, long? compraId = null,
        long? compraProdutoLoteId = null, long? usuarioId = null, string? observacao = null, bool ehLoteFicticio = false)
        => throw new InvalidOperationException("falha injetada no lote (teste de atomicidade da compra)");
    public Task<MovimentoLote> RegistrarSaidaAsync(long produtoLoteId, decimal quantidade, TipoMovimentoLote tipo,
        long? vendaId = null, long? usuarioId = null, string? observacao = null) => throw new NotImplementedException();
    public Task<List<ProdutoLote>> ListarLotesAtivosAsync(long produtoId, long filialId) => throw new NotImplementedException();
    public Task<int> GerarLotesFicticiosDoGrupoAsync(long grupoProdutoId, long? usuarioId = null) => throw new NotImplementedException();
    public Task<int> GerarLotesFicticiosSngpcAsync(long? usuarioId = null) => throw new NotImplementedException();
}

/// <summary>
/// P0.15-compra: a finalizacao da compra tem que ser ATOMICA (transacao unica). Cobre os dois lados:
/// falha no meio faz rollback TOTAL (nada da compra pela metade), e o caminho feliz commita tudo.
/// </summary>
[Collection("pg")]
public class CompraTransacaoTests
{
    private const long FilialId = 1;
    private const long PessoaId = 7_000_000_001, FornId = 7_000_000_002, PlanoId = 7_000_000_003;

    private readonly PostgresFixture _pg;
    public CompraTransacaoTests(PostgresFixture pg) => _pg = pg;

    private MovimentoEstoqueService MovReal(AppDbContext db)
        => new(db, new FilialContexto(new HttpContextAccessor()));

    /// <summary>Setup comum idempotente (os dois testes compartilham o mesmo banco por run).</summary>
    private async Task SeedComumAsync()
    {
        await using var seed = _pg.CriarContexto(aplicandoSync: true);
        if (!await seed.Filiais.AnyAsync(f => f.Id == FilialId))
            seed.Filiais.Add(new Filial
            {
                Id = FilialId, NomeFilial = "Filial Teste", RazaoSocial = "Teste LTDA", NomeFantasia = "Teste",
                Cnpj = "00.000.000/0001-00", Cep = "00000-000", Rua = "R", Numero = "1", Bairro = "C",
                Cidade = "SP", Uf = "SP", Telefone = "0", Email = "t@t.com"
            });
        if (!await seed.Pessoas.AnyAsync(p => p.Id == PessoaId))
            seed.Pessoas.Add(new Pessoa { Id = PessoaId, Tipo = "J", Nome = "FORN", CpfCnpj = "11111111000199" });
        if (!await seed.Fornecedores.AnyAsync(f => f.Id == FornId))
            seed.Fornecedores.Add(new Fornecedor { Id = FornId, PessoaId = PessoaId });
        if (!await seed.PlanosContas.AnyAsync(p => p.Id == PlanoId))
            seed.PlanosContas.Add(new PlanoConta { Id = PlanoId, Descricao = "Compra de Mercadorias" });
        if (!await seed.Configuracoes.AnyAsync(c => c.Chave == "pc.compra_mercadorias"))
            seed.Configuracoes.Add(new Configuracao { Chave = "pc.compra_mercadorias", Valor = PlanoId.ToString() });
        await seed.SaveChangesAsync();
    }

    // Cria produto rastreavel (ClasseTerapeutica psicotropico => chama RegistrarEntradaAsync) + dados +
    // compra com 1 item vinculado (Lote setado). Ids distintos por teste (banco compartilhado).
    private async Task SeedCompraAsync(long prodId, long dadosId, long compraId, long itemId)
    {
        await using var seed = _pg.CriarContexto(aplicandoSync: true);
        seed.Produtos.Add(new Produto { Id = prodId, Nome = "PROD RASTREAVEL", ClasseTerapeutica = "Psicotrópicos" });
        seed.ProdutosDados.Add(new ProdutoDados { Id = dadosId, ProdutoId = prodId, FilialId = FilialId, EstoqueAtual = 0, CustoMedio = 0 });
        var compra = new Compra
        {
            Id = compraId, FilialId = FilialId, FornecedorId = FornId,
            ChaveNfe = $"CHAVE{compraId}", // ChaveNfe tem indice unico — distinta por compra
            NumeroNf = compraId.ToString(), Status = CompraStatus.Conferencia, ValorNota = 50
        };
        compra.Produtos.Add(new CompraProduto
        {
            Id = itemId, CompraId = compraId, ProdutoId = prodId, NumeroItem = 1,
            Quantidade = 10, ValorUnitario = 5, ValorItemNota = 50, Vinculado = true, Fracao = 1, Lote = "L1"
        });
        seed.Compras.Add(compra);
        await seed.SaveChangesAsync();
    }

    /// <summary>Falha DEPOIS de mexer no estoque (lote estoura) -> rollback TOTAL, nada commitado.</summary>
    [Fact]
    public async Task FinalizarAsync_FalhaAposMexerNoEstoque_FazRollbackTotal()
    {
        const long prodId = 7_000_000_010, dadosId = 7_000_000_020, compraId = 7_000_000_030, itemId = 7_000_000_031;
        await SeedComumAsync();
        await SeedCompraAsync(prodId, dadosId, compraId, itemId);

        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            var svc = new CompraService(db, new LogFlush(db), new LoteExplode(), MovReal(db));
            var req = new FinalizarCompraRequest { CompraId = compraId, NomeUsuario = "TESTE" };
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.FinalizarAsync(req));
        }

        await using (var check = _pg.CriarContexto(aplicandoSync: true))
        {
            var d = await check.ProdutosDados.FirstAsync(x => x.Id == dadosId);
            Assert.Equal(0m, d.EstoqueAtual);                                                       // estoque NAO entrou
            Assert.Equal(0, await check.MovimentosEstoque.CountAsync(m => m.ProdutoId == prodId));   // sem movimento
            Assert.Equal(0, await check.ContasPagar.CountAsync(c => c.CompraId == compraId));        // sem conta a pagar
            var compra = await check.Compras.FirstAsync(c => c.Id == compraId);
            Assert.NotEqual(CompraStatus.Finalizada, compra.Status);                                 // segue nao-finalizada
        }
    }

    /// <summary>Caminho feliz: finaliza sem falha -> commita TUDO (estoque, custo, movimento, lote,
    /// conta a pagar, status) e os logs pos-commit rodam sem quebrar (tx ja' fechada).</summary>
    [Fact]
    public async Task FinalizarAsync_Sucesso_CommitaTudoAtomico()
    {
        const long prodId = 8_000_000_010, dadosId = 8_000_000_020, compraId = 8_000_000_030, itemId = 8_000_000_031;
        await SeedComumAsync();
        await SeedCompraAsync(prodId, dadosId, compraId, itemId);

        await using (var db = _pg.CriarContexto(aplicandoSync: true))
        {
            // ProdutoLoteService REAL (o lote entra de verdade) + LogFlush (exercita o log pos-commit).
            var svc = new CompraService(db, new LogFlush(db), new ProdutoLoteService(db), MovReal(db));
            var req = new FinalizarCompraRequest { CompraId = compraId, NomeUsuario = "TESTE" };
            var res = await svc.FinalizarAsync(req);
            Assert.Equal(1, res.ProdutosAtualizados);
        }

        await using (var check = _pg.CriarContexto(aplicandoSync: true))
        {
            var d = await check.ProdutosDados.FirstAsync(x => x.Id == dadosId);
            Assert.Equal(10m, d.EstoqueAtual);                                                       // estoque entrou
            Assert.Equal(5m, d.CustoMedio);                                                          // custo medio = 50/10
            Assert.Equal(1, await check.MovimentosEstoque.CountAsync(m => m.ProdutoId == prodId));    // 1 movimento
            Assert.True(await check.Set<ProdutoLote>().AnyAsync(l => l.ProdutoId == prodId));         // lote registrado
            Assert.Equal(1, await check.ContasPagar.CountAsync(c => c.CompraId == compraId));         // 1 conta a pagar
            var compra = await check.Compras.FirstAsync(c => c.Id == compraId);
            Assert.Equal(CompraStatus.Finalizada, compra.Status);                                     // finalizada
        }
    }
}
