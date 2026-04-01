using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Produtos;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class ProdutoService : IProdutoService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Produtos";
    private const string ENTIDADE = "Produto";

    public ProdutoService(AppDbContext db, ILogAcaoService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<List<ProdutoListDto>> ListarAsync(string? busca = null)
    {
        IQueryable<Produto> query = _db.Produtos
            .Include(p => p.Fabricante)
            .Include(p => p.GrupoPrincipal);

        if (!string.IsNullOrWhiteSpace(busca) && busca.Trim().Length >= 3)
        {
            var termo = busca.Trim().ToUpper();
            query = query.Where(p =>
                p.Nome.Contains(termo) ||
                (p.CodigoBarras != null && p.CodigoBarras.Contains(termo)) ||
                p.Id.ToString().Contains(termo));
        }
        else
        {
            return new List<ProdutoListDto>();
        }

        return await query
            .Where(p => !p.Eliminado)
            .OrderBy(p => p.Nome)
            .Take(200)
            .Select(p => new ProdutoListDto
            {
                Id = p.Id,
                Nome = p.Nome,
                CodigoBarras = p.CodigoBarras,
                FabricanteNome = p.Fabricante != null ? p.Fabricante.Nome : null,
                GrupoPrincipalNome = p.GrupoPrincipal != null ? p.GrupoPrincipal.Nome : null,
                Ativo = p.Ativo,
                Eliminado = p.Eliminado,
                CriadoEm = p.CriadoEm
            })
            .ToListAsync();
    }

    public async Task<ProdutoDetalheDto> ObterAsync(long id)
    {
        var p = await _db.Produtos
            .Include(x => x.Fabricante)
            .Include(x => x.GrupoPrincipal)
            .Include(x => x.GrupoProduto)
            .Include(x => x.SubGrupo)
            .Include(x => x.Ncm)
            .Include(x => x.Barras)
            .Include(x => x.RegistrosMs)
            .Include(x => x.Substancias).ThenInclude(s => s.Substancia)
            .Include(x => x.Fornecedores).ThenInclude(f => f.Fornecedor)
            .Include(x => x.Fiscal).ThenInclude(f => f!.Ncm)
            .Include(x => x.Dados)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Produto {id} não encontrado.");

        return MapDetalhe(p);
    }

    public async Task<ProdutoListDto> CriarAsync(ProdutoFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new ArgumentException("Nome é obrigatório.");

        var p = new Produto
        {
            Nome = dto.Nome.Trim().ToUpper(),
            CodigoBarras = dto.CodigoBarras?.Trim(),
            QtdeEmbalagem = dto.QtdeEmbalagem,
            PrecoFp = dto.PrecoFp,
            Lista = dto.Lista,
            Fracao = dto.Fracao > 0 ? dto.Fracao : (short)1,
            Ativo = dto.Ativo,
            Eliminado = dto.Eliminado,
            FabricanteId = dto.FabricanteId,
            GrupoPrincipalId = dto.GrupoPrincipalId,
            GrupoProdutoId = dto.GrupoProdutoId,
            SubGrupoId = dto.SubGrupoId,
            NcmId = dto.NcmId
        };

        _db.Produtos.Add(p);
        await _db.SaveChangesAsync();

        SincronizarSubTabelas(p, dto);
        await _db.SaveChangesAsync();

        await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, p.Id,
            novo: new Dictionary<string, string?> { ["Nome"] = p.Nome });

        return new ProdutoListDto
        {
            Id = p.Id, Nome = p.Nome, CodigoBarras = p.CodigoBarras,
            Ativo = p.Ativo, Eliminado = p.Eliminado, CriadoEm = p.CriadoEm
        };
    }

    public async Task AtualizarAsync(long id, ProdutoFormDto dto)
    {
        var p = await _db.Produtos
            .Include(x => x.Barras)
            .Include(x => x.RegistrosMs)
            .Include(x => x.Substancias)
            .Include(x => x.Fornecedores)
            .Include(x => x.Fiscal)
            .Include(x => x.Dados)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Produto {id} não encontrado.");

        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new ArgumentException("Nome é obrigatório.");

        var anterior = new Dictionary<string, string?> { ["Nome"] = p.Nome, ["Ativo"] = p.Ativo ? "Sim" : "Não" };

        p.Nome = dto.Nome.Trim().ToUpper();
        p.CodigoBarras = dto.CodigoBarras?.Trim();
        p.QtdeEmbalagem = dto.QtdeEmbalagem;
        p.PrecoFp = dto.PrecoFp;
        p.Lista = dto.Lista;
        p.Fracao = dto.Fracao > 0 ? dto.Fracao : (short)1;
        p.Ativo = dto.Ativo;
        p.Eliminado = dto.Eliminado;
        p.FabricanteId = dto.FabricanteId;
        p.GrupoPrincipalId = dto.GrupoPrincipalId;
        p.GrupoProdutoId = dto.GrupoProdutoId;
        p.SubGrupoId = dto.SubGrupoId;
        p.NcmId = dto.NcmId;

        SincronizarSubTabelas(p, dto);
        await _db.SaveChangesAsync();

        var novo = new Dictionary<string, string?> { ["Nome"] = p.Nome, ["Ativo"] = p.Ativo ? "Sim" : "Não" };
        await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
    }

    public async Task<string> ExcluirAsync(long id)
    {
        var p = await _db.Produtos.FindAsync(id)
            ?? throw new KeyNotFoundException($"Produto {id} não encontrado.");

        // Soft-delete: marca como eliminado
        p.Eliminado = true;
        p.Ativo = false;
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id,
            anterior: new Dictionary<string, string?> { ["Nome"] = p.Nome });
        return "eliminado";
    }

    // ── Sincronizar sub-tabelas ─────────────────────────────────────

    private void SincronizarSubTabelas(Produto p, ProdutoFormDto dto)
    {
        // Barras
        SyncCollection(
            p.Barras, dto.Barras,
            d => d.Id,
            (e, d) => { e.Barras = d.Barras.Trim(); },
            d => new ProdutoBarras { ProdutoId = p.Id, Barras = d.Barras.Trim() },
            _db.ProdutosBarras);

        // MS
        SyncCollection(
            p.RegistrosMs, dto.RegistrosMs,
            d => d.Id,
            (e, d) => { e.NumeroMs = d.NumeroMs.Trim(); },
            d => new ProdutoMs { ProdutoId = p.Id, NumeroMs = d.NumeroMs.Trim() },
            _db.ProdutosMs);

        // Substâncias
        SyncCollection(
            p.Substancias, dto.Substancias,
            d => d.Id,
            (e, d) => { e.SubstanciaId = d.SubstanciaId; },
            d => new ProdutoSubstancia { ProdutoId = p.Id, SubstanciaId = d.SubstanciaId },
            _db.ProdutosSubstancias);

        // Fornecedores
        SyncCollection(
            p.Fornecedores, dto.Fornecedores,
            d => d.Id,
            (e, d) =>
            {
                e.FornecedorId = d.FornecedorId;
                e.CodigoProdutoFornecedor = d.CodigoProdutoFornecedor?.Trim();
                e.NomeProduto = d.NomeProduto?.Trim();
                e.Fracao = d.Fracao;
            },
            d => new ProdutoFornecedor
            {
                ProdutoId = p.Id,
                FornecedorId = d.FornecedorId,
                CodigoProdutoFornecedor = d.CodigoProdutoFornecedor?.Trim(),
                NomeProduto = d.NomeProduto?.Trim(),
                Fracao = d.Fracao
            },
            _db.ProdutosFornecedores);

        // Fiscal (1:1)
        if (dto.Fiscal != null)
        {
            if (p.Fiscal == null)
            {
                p.Fiscal = new ProdutoFiscal { ProdutoId = p.Id };
                _db.ProdutosFiscal.Add(p.Fiscal);
            }
            MapFiscal(p.Fiscal, dto.Fiscal);
        }
        else if (p.Fiscal != null)
        {
            _db.ProdutosFiscal.Remove(p.Fiscal);
        }

        // Dados (por filial)
        SyncDados(p, dto.Dados);
    }

    private void SyncDados(Produto p, List<ProdutoDadosDto> dtos)
    {
        var dtoIds = dtos.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToHashSet();
        foreach (var e in p.Dados.Where(e => !dtoIds.Contains(e.Id)).ToList())
            _db.ProdutosDados.Remove(e);

        foreach (var d in dtos)
        {
            if (d.Id.HasValue)
            {
                var e = p.Dados.FirstOrDefault(x => x.Id == d.Id.Value);
                if (e != null) MapDados(e, d);
            }
            else
            {
                var e = new ProdutoDados { ProdutoId = p.Id };
                MapDados(e, d);
                _db.ProdutosDados.Add(e);
            }
        }
    }

    private static void MapFiscal(ProdutoFiscal e, ProdutoFiscalDto d)
    {
        e.NcmId = d.NcmId;
        e.Cest = d.Cest?.Trim();
        e.OrigemMercadoria = d.OrigemMercadoria?.Trim();
        e.CstIcms = d.CstIcms?.Trim();
        e.Csosn = d.Csosn?.Trim();
        e.AliquotaIcms = d.AliquotaIcms;
        e.CstPis = d.CstPis?.Trim();
        e.AliquotaPis = d.AliquotaPis;
        e.CstCofins = d.CstCofins?.Trim();
        e.AliquotaCofins = d.AliquotaCofins;
        e.CstIpi = d.CstIpi?.Trim();
        e.AliquotaIpi = d.AliquotaIpi;
    }

    private static void MapDados(ProdutoDados e, ProdutoDadosDto d)
    {
        e.FilialId = d.FilialId;
        e.EstoqueAtual = d.EstoqueAtual; e.EstoqueMinimo = d.EstoqueMinimo;
        e.EstoqueMaximo = d.EstoqueMaximo; e.Demanda = d.Demanda; e.CurvaAbc = d.CurvaAbc;
        e.UltimaCompraUnitario = d.UltimaCompraUnitario; e.UltimaCompraSt = d.UltimaCompraSt;
        e.UltimaCompraOutros = d.UltimaCompraOutros; e.UltimaCompraIpi = d.UltimaCompraIpi;
        e.UltimaCompraFpc = d.UltimaCompraFpc; e.UltimaCompraBoleto = d.UltimaCompraBoleto;
        e.UltimaCompraDifal = d.UltimaCompraDifal;
        e.CustoMedio = d.CustoMedio; e.ProjecaoLucro = d.ProjecaoLucro;
        e.Markup = d.Markup; e.ValorVenda = d.ValorVenda;
        e.ValorPromocao = d.ValorPromocao; e.PromocaoInicio = d.PromocaoInicio; e.PromocaoFim = d.PromocaoFim;
        e.DescontoMinimo = d.DescontoMinimo; e.DescontoMaxSemSenha = d.DescontoMaxSemSenha;
        e.DescontoMaxComSenha = d.DescontoMaxComSenha;
        e.Comissao = d.Comissao; e.ValorIncentivo = d.ValorIncentivo;
        e.ProdutoLocalId = d.ProdutoLocalId; e.SecaoId = d.SecaoId; e.ProdutoFamiliaId = d.ProdutoFamiliaId;
        e.NomeEtiqueta = d.NomeEtiqueta?.Trim(); e.Mensagem = d.Mensagem?.Trim();
        e.BloquearDesconto = d.BloquearDesconto; e.BloquearPromocao = d.BloquearPromocao;
        e.NaoAtualizarAbcfarma = d.NaoAtualizarAbcfarma;
        e.NaoAtualizarGestorTributario = d.NaoAtualizarGestorTributario;
        e.BloquearCompras = d.BloquearCompras; e.ProdutoFormula = d.ProdutoFormula;
        e.BloquearComissao = d.BloquearComissao; e.BloquearCoberturaOferta = d.BloquearCoberturaOferta;
        e.UsoContinuo = d.UsoContinuo; e.AvisoFracao = d.AvisoFracao;
        e.UltimaCompraEm = d.UltimaCompraEm; e.UltimaVendaEm = d.UltimaVendaEm;
    }

    // ── Helper genérico sync ────────────────────────────────────────

    private void SyncCollection<TEntity, TDto>(
        ICollection<TEntity> existentes,
        List<TDto> dtos,
        Func<TDto, long?> getId,
        Action<TEntity, TDto> update,
        Func<TDto, TEntity> create,
        DbSet<TEntity> dbSet) where TEntity : BaseEntity
    {
        var dtoIds = dtos.Where(d => getId(d).HasValue).Select(d => getId(d)!.Value).ToHashSet();
        foreach (var e in existentes.Where(e => !dtoIds.Contains(e.Id)).ToList())
            dbSet.Remove(e);

        foreach (var d in dtos)
        {
            var id = getId(d);
            if (id.HasValue)
            {
                var e = existentes.FirstOrDefault(x => x.Id == id.Value);
                if (e != null) update(e, d);
            }
            else
            {
                dbSet.Add(create(d));
            }
        }
    }

    // ── Mapping ─────────────────────────────────────────────────────

    private static ProdutoDetalheDto MapDetalhe(Produto p) => new()
    {
        Id = p.Id, Nome = p.Nome, CodigoBarras = p.CodigoBarras,
        QtdeEmbalagem = p.QtdeEmbalagem, PrecoFp = p.PrecoFp,
        Lista = p.Lista, Fracao = p.Fracao, Ativo = p.Ativo,
        Eliminado = p.Eliminado, CriadoEm = p.CriadoEm,
        FabricanteId = p.FabricanteId,
        GrupoPrincipalId = p.GrupoPrincipalId,
        GrupoProdutoId = p.GrupoProdutoId,
        SubGrupoId = p.SubGrupoId,
        NcmId = p.NcmId,
        FabricanteNome = p.Fabricante?.Nome,
        GrupoPrincipalNome = p.GrupoPrincipal?.Nome,
        GrupoProdutoNome = p.GrupoProduto?.Nome,
        SubGrupoNome = p.SubGrupo?.Nome,
        NcmCodigo = p.Ncm?.CodigoNcm,
        Barras = p.Barras.Select(b => new ProdutoBarrasDto { Id = b.Id, Barras = b.Barras }).ToList(),
        RegistrosMs = p.RegistrosMs.Select(m => new ProdutoMsDto { Id = m.Id, NumeroMs = m.NumeroMs }).ToList(),
        Substancias = p.Substancias.Select(s => new ProdutoSubstanciaDto
        {
            Id = s.Id, SubstanciaId = s.SubstanciaId, SubstanciaNome = s.Substancia?.Nome
        }).ToList(),
        Fornecedores = p.Fornecedores.Select(f => new ProdutoFornecedorDto
        {
            Id = f.Id, FornecedorId = f.FornecedorId,
            CodigoProdutoFornecedor = f.CodigoProdutoFornecedor,
            NomeProduto = f.NomeProduto, Fracao = f.Fracao
        }).ToList(),
        Fiscal = p.Fiscal == null ? null : new ProdutoFiscalDto
        {
            Id = p.Fiscal.Id, NcmId = p.Fiscal.NcmId,
            NcmCodigo = p.Fiscal.Ncm?.CodigoNcm,
            Cest = p.Fiscal.Cest, OrigemMercadoria = p.Fiscal.OrigemMercadoria,
            CstIcms = p.Fiscal.CstIcms, Csosn = p.Fiscal.Csosn, AliquotaIcms = p.Fiscal.AliquotaIcms,
            CstPis = p.Fiscal.CstPis, AliquotaPis = p.Fiscal.AliquotaPis,
            CstCofins = p.Fiscal.CstCofins, AliquotaCofins = p.Fiscal.AliquotaCofins,
            CstIpi = p.Fiscal.CstIpi, AliquotaIpi = p.Fiscal.AliquotaIpi
        },
        Dados = p.Dados.OrderBy(d => d.FilialId).Select(d => new ProdutoDadosDto
        {
            Id = d.Id, FilialId = d.FilialId,
            EstoqueAtual = d.EstoqueAtual, EstoqueMinimo = d.EstoqueMinimo,
            EstoqueMaximo = d.EstoqueMaximo, Demanda = d.Demanda, CurvaAbc = d.CurvaAbc,
            UltimaCompraUnitario = d.UltimaCompraUnitario, UltimaCompraSt = d.UltimaCompraSt,
            UltimaCompraOutros = d.UltimaCompraOutros, UltimaCompraIpi = d.UltimaCompraIpi,
            UltimaCompraFpc = d.UltimaCompraFpc, UltimaCompraBoleto = d.UltimaCompraBoleto,
            UltimaCompraDifal = d.UltimaCompraDifal,
            CustoMedio = d.CustoMedio, ProjecaoLucro = d.ProjecaoLucro,
            Markup = d.Markup, ValorVenda = d.ValorVenda,
            ValorPromocao = d.ValorPromocao, PromocaoInicio = d.PromocaoInicio, PromocaoFim = d.PromocaoFim,
            DescontoMinimo = d.DescontoMinimo, DescontoMaxSemSenha = d.DescontoMaxSemSenha,
            DescontoMaxComSenha = d.DescontoMaxComSenha,
            Comissao = d.Comissao, ValorIncentivo = d.ValorIncentivo,
            ProdutoLocalId = d.ProdutoLocalId, SecaoId = d.SecaoId, ProdutoFamiliaId = d.ProdutoFamiliaId,
            NomeEtiqueta = d.NomeEtiqueta, Mensagem = d.Mensagem,
            BloquearDesconto = d.BloquearDesconto, BloquearPromocao = d.BloquearPromocao,
            NaoAtualizarAbcfarma = d.NaoAtualizarAbcfarma,
            NaoAtualizarGestorTributario = d.NaoAtualizarGestorTributario,
            BloquearCompras = d.BloquearCompras, ProdutoFormula = d.ProdutoFormula,
            BloquearComissao = d.BloquearComissao, BloquearCoberturaOferta = d.BloquearCoberturaOferta,
            UsoContinuo = d.UsoContinuo, AvisoFracao = d.AvisoFracao,
            UltimaCompraEm = d.UltimaCompraEm, UltimaVendaEm = d.UltimaVendaEm
        }).ToList()
    };
}
