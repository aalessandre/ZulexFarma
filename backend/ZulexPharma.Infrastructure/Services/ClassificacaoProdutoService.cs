using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Produtos;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class ClassificacaoProdutoService<T> where T : ClassificacaoProdutoBase, new()
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private readonly IProdutoLoteService _loteService;
    private readonly string _tela;
    private readonly string _entidade;

    public ClassificacaoProdutoService(AppDbContext db, ILogAcaoService log, IProdutoLoteService loteService, string tela, string entidade)
    {
        _db = db;
        _log = log;
        _loteService = loteService;
        _tela = tela;
        _entidade = entidade;
    }

    private DbSet<T> Set => _db.Set<T>();

    public async Task<List<ClassificacaoListDto>> ListarAsync()
    {
        return await Set.OrderBy(x => x.Nome)
            .Select(x => new ClassificacaoListDto
            {
                Id = x.Id, Nome = x.Nome, ComissaoPercentual = x.ComissaoPercentual,
                MarkupPadrao = x.MarkupPadrao, ProjecaoLucro = x.ProjecaoLucro,
                CriadoEm = x.CriadoEm, Ativo = x.Ativo
            }).ToListAsync();
    }

    public async Task<ClassificacaoDetalheDto> ObterAsync(long id)
    {
        var e = await Set.FindAsync(id) ?? throw new KeyNotFoundException($"{_entidade} {id} não encontrado.");
        var dto = MapToDetalhe(e);
        dto.ComissaoFaixas = await _db.ComissaoFaixasDesconto
            .Where(f => f.TipoEntidade == _entidade && f.EntidadeId == id)
            .OrderBy(f => f.Ordem)
            .Select(f => new ComissaoFaixaDescontoDto
            {
                Id = f.Id, DescontoInicial = f.DescontoInicial,
                DescontoFinal = f.DescontoFinal, ComissaoPercentual = f.ComissaoPercentual,
                Ordem = f.Ordem
            }).ToListAsync();
        return dto;
    }

    public async Task<ClassificacaoListDto> CriarAsync(ClassificacaoFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
        var e = new T();
        MapFromDto(e, dto);
        Set.Add(e);
        await _db.SaveChangesAsync();
        await SalvarFaixasAsync(e.Id, dto.ComissaoFaixas);
        await _log.RegistrarAsync(_tela, "CRIAÇÃO", _entidade, e.Id, novo: ParaDict(e));
        return new ClassificacaoListDto { Id = e.Id, Nome = e.Nome, ComissaoPercentual = e.ComissaoPercentual, MarkupPadrao = e.MarkupPadrao, ProjecaoLucro = e.ProjecaoLucro, CriadoEm = e.CriadoEm, Ativo = e.Ativo };
    }

    public async Task AtualizarAsync(long id, ClassificacaoFormDto dto)
    {
        var e = await Set.FindAsync(id) ?? throw new KeyNotFoundException($"{_entidade} {id} não encontrado.");
        var anterior = ParaDict(e);
        bool controleLotesAntes = e.ControlarLotesVencimento;
        MapFromDto(e, dto);
        await _db.SaveChangesAsync();
        await SalvarFaixasAsync(id, dto.ComissaoFaixas);
        var novo = ParaDict(e);
        if (!DictsIguais(anterior, novo))
            await _log.RegistrarAsync(_tela, "ALTERAÇÃO", _entidade, id, anterior: anterior, novo: novo);

        // ── Hook: ativação retroativa do controle de lotes ────────────
        // Quando o usuário LIGA ControlarLotesVencimento em um GrupoProduto,
        // gera lotes fictícios para todos os produtos do grupo que tenham estoque > 0.
        // (Só implementado para GrupoProduto por enquanto — SubGrupo/Secao ficam pra depois.)
        if (!controleLotesAntes && e.ControlarLotesVencimento && typeof(T) == typeof(GrupoProduto))
        {
            var criados = await _loteService.GerarLotesFicticiosDoGrupoAsync(id, null);
            if (criados > 0)
            {
                await _log.RegistrarAsync(_tela, "ATIVAÇÃO RASTREIO LOTES", _entidade, id,
                    novo: new Dictionary<string, string?>
                    {
                        ["Grupo"] = e.Nome,
                        ["Lotes fictícios criados"] = criados.ToString(),
                        ["Observação"] = "Lotes gerados automaticamente a partir do estoque atual. Recomenda-se fazer balanço físico."
                    });
            }
        }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        var e = await Set.FindAsync(id) ?? throw new KeyNotFoundException($"{_entidade} {id} não encontrado.");
        var dados = ParaDict(e);
        var faixas = await _db.ComissaoFaixasDesconto
            .Where(f => f.TipoEntidade == _entidade && f.EntidadeId == id).ToListAsync();
        _db.ComissaoFaixasDesconto.RemoveRange(faixas);
        Set.Remove(e);
        try
        {
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(_tela, "EXCLUSÃO", _entidade, id, anterior: dados);
            return "excluido";
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            var r = await Set.FindAsync(id);
            r!.Ativo = false;
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(_tela, "DESATIVAÇÃO", _entidade, id);
            return "desativado";
        }
    }

    private async Task SalvarFaixasAsync(long entidadeId, List<ComissaoFaixaDescontoDto> faixasDto)
    {
        var existentes = await _db.ComissaoFaixasDesconto
            .Where(f => f.TipoEntidade == _entidade && f.EntidadeId == entidadeId)
            .ToListAsync();

        _db.ComissaoFaixasDesconto.RemoveRange(existentes);

        if (faixasDto?.Count > 0)
        {
            for (int i = 0; i < faixasDto.Count; i++)
            {
                var fd = faixasDto[i];
                _db.ComissaoFaixasDesconto.Add(new ComissaoFaixaDesconto
                {
                    TipoEntidade = _entidade,
                    EntidadeId = entidadeId,
                    DescontoInicial = fd.DescontoInicial,
                    DescontoFinal = fd.DescontoFinal,
                    ComissaoPercentual = fd.ComissaoPercentual,
                    Ordem = i
                });
            }
            await _db.SaveChangesAsync();
        }
    }

    private void MapFromDto(T e, ClassificacaoFormDto dto)
    {
        e.Nome = dto.Nome.Trim().ToUpper();
        e.ComissaoPercentual = dto.ComissaoPercentual;
        e.DescontoMinimo = dto.DescontoMinimo;
        e.DescontoMaximo = dto.DescontoMaximo;
        e.DescontoMaximoComSenha = dto.DescontoMaximoComSenha;
        e.ProjecaoLucro = dto.ProjecaoLucro;
        e.MarkupPadrao = dto.MarkupPadrao;
        e.BaseCalculo = dto.BaseCalculo;
        e.AtualizarAbcFarma = dto.AtualizarAbcFarma;
        e.ControlarLotesVencimento = dto.ControlarLotesVencimento;
        e.InformarPrescritorVenda = dto.InformarPrescritorVenda;
        e.ImprimirEtiqueta = dto.ImprimirEtiqueta;
        e.PermitirDescontoPrazo = dto.PermitirDescontoPrazo;
        e.PermitirPromocao = dto.PermitirPromocao;
        e.PermitirDescontosProgressivos = dto.PermitirDescontosProgressivos;
        e.Ativo = dto.Ativo;
    }

    private ClassificacaoDetalheDto MapToDetalhe(T e) => new()
    {
        Id = e.Id, Nome = e.Nome, ComissaoPercentual = e.ComissaoPercentual,
        DescontoMinimo = e.DescontoMinimo, DescontoMaximo = e.DescontoMaximo,
        DescontoMaximoComSenha = e.DescontoMaximoComSenha, ProjecaoLucro = e.ProjecaoLucro,
        MarkupPadrao = e.MarkupPadrao, BaseCalculo = e.BaseCalculo, AtualizarAbcFarma = e.AtualizarAbcFarma,
        ControlarLotesVencimento = e.ControlarLotesVencimento, InformarPrescritorVenda = e.InformarPrescritorVenda,
        ImprimirEtiqueta = e.ImprimirEtiqueta, PermitirDescontoPrazo = e.PermitirDescontoPrazo,
        PermitirPromocao = e.PermitirPromocao, PermitirDescontosProgressivos = e.PermitirDescontosProgressivos,
        Ativo = e.Ativo, CriadoEm = e.CriadoEm
    };

    private static Dictionary<string, string?> ParaDict(T e) => new()
    {
        ["Nome"] = e.Nome, ["% Comissão"] = e.ComissaoPercentual.ToString("N2"),
        ["% Desc Mínimo"] = e.DescontoMinimo.ToString("N2"), ["% Desc Máximo"] = e.DescontoMaximo.ToString("N2"),
        ["% Desc Máx c/ Senha"] = e.DescontoMaximoComSenha.ToString("N2"),
        ["% Projeção Lucro"] = e.ProjecaoLucro.ToString("N2"), ["% Markup"] = e.MarkupPadrao.ToString("N2"),
        ["Base Cálculo"] = e.BaseCalculo, ["ABCFarma"] = e.AtualizarAbcFarma ? "Sim" : "Não",
        ["Controlar Lotes"] = e.ControlarLotesVencimento ? "Sim" : "Não",
        ["Prescritor"] = e.InformarPrescritorVenda ? "Sim" : "Não", ["Etiqueta"] = e.ImprimirEtiqueta ? "Sim" : "Não",
        ["Desc Prazo"] = e.PermitirDescontoPrazo ? "Sim" : "Não", ["Promoção"] = e.PermitirPromocao ? "Sim" : "Não",
        ["Desc Progressivo"] = e.PermitirDescontosProgressivos ? "Sim" : "Não", ["Ativo"] = e.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b) =>
        a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
