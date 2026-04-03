using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly int _filialCodigo;
    private const string TELA = "Produtos";
    private const string ENTIDADE = "Produto";

    public ProdutoService(AppDbContext db, ILogAcaoService log, IConfiguration config)
    {
        _db = db;
        _log = log;
        _filialCodigo = int.TryParse(config["Filial:Codigo"], out var f) ? f : 0;
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
            .Include(x => x.Fornecedores).ThenInclude(f => f.Fornecedor).ThenInclude(fn => fn.Pessoa)
            .Include(x => x.Fiscais).ThenInclude(f => f.Ncm)
            .Include(x => x.Dados).ThenInclude(d => d.ProdutoLocal)
            .Include(x => x.Dados).ThenInclude(d => d.Secao)
            .Include(x => x.Dados).ThenInclude(d => d.ProdutoFamilia)
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

        // Criar registros por filial para todas as filiais ativas que não vieram no DTO
        var todasFiliais = await _db.Filiais.Where(f => f.Ativo).Select(f => f.Id).ToListAsync();

        // ProdutoDados
        var filiaisDados = dto.Dados.Select(d => d.FilialId).ToHashSet();
        foreach (var filialId in todasFiliais.Where(fId => !filiaisDados.Contains(fId)))
            _db.ProdutosDados.Add(new ProdutoDados { ProdutoId = p.Id, FilialId = filialId });

        // ProdutoFiscal
        var filiaisFiscal = dto.Fiscais.Select(d => d.FilialId).ToHashSet();
        foreach (var filialId in todasFiliais.Where(fId => !filiaisFiscal.Contains(fId)))
            _db.ProdutosFiscal.Add(new ProdutoFiscal { ProdutoId = p.Id, FilialId = filialId });

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
            .Include(x => x.Fiscais)
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

        // Propagação de preço para outras filiais conforme regra configurada
        await PropagarPrecoAsync(p, dto);

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

        // Fornecedores (por filial — proteger outras filiais)
        SyncPerFilial(p.Fornecedores, dto.Fornecedores,
            d => d.Id, d => d.FilialId,
            (e, d) =>
            {
                e.FilialId = d.FilialId;
                e.FornecedorId = d.FornecedorId;
                e.CodigoProdutoFornecedor = d.CodigoProdutoFornecedor?.Trim();
                e.NomeProduto = d.NomeProduto?.Trim();
                e.Fracao = d.Fracao;
            },
            d => new ProdutoFornecedor
            {
                ProdutoId = p.Id, FilialId = d.FilialId,
                FornecedorId = d.FornecedorId,
                CodigoProdutoFornecedor = d.CodigoProdutoFornecedor?.Trim(),
                NomeProduto = d.NomeProduto?.Trim(),
                Fracao = d.Fracao
            },
            _db.ProdutosFornecedores);

        // Fiscal (por filial — proteger outras filiais)
        SyncFiscal(p, dto.Fiscais);

        // Dados (por filial)
        SyncDados(p, dto.Dados);
    }

    private void SyncDados(Produto p, List<ProdutoDadosDto> dtos)
    {
        // Proteger: nunca deletar ProdutoDados de outras filiais.
        // Apenas atualiza/cria registros que vieram no DTO.
        foreach (var d in dtos)
        {
            if (d.Id.HasValue)
            {
                var e = p.Dados.FirstOrDefault(x => x.Id == d.Id.Value);
                if (e != null) MapDados(e, d);
            }
            else
            {
                // Se já existe um ProdutoDados para esta filial, atualizar em vez de duplicar
                var existente = p.Dados.FirstOrDefault(x => x.FilialId == d.FilialId);
                if (existente != null)
                {
                    MapDados(existente, d);
                }
                else
                {
                    var e = new ProdutoDados { ProdutoId = p.Id };
                    MapDados(e, d);
                    _db.ProdutosDados.Add(e);
                }
            }
        }
    }

    private void SyncFiscal(Produto p, List<ProdutoFiscalDto> dtos)
    {
        // Proteger: nunca deletar Fiscal de outras filiais
        foreach (var d in dtos)
        {
            if (d.Id.HasValue)
            {
                var e = p.Fiscais.FirstOrDefault(x => x.Id == d.Id.Value);
                if (e != null) MapFiscal(e, d);
            }
            else
            {
                var existente = p.Fiscais.FirstOrDefault(x => x.FilialId == d.FilialId);
                if (existente != null)
                {
                    MapFiscal(existente, d);
                }
                else
                {
                    var e = new ProdutoFiscal { ProdutoId = p.Id, FilialId = d.FilialId };
                    MapFiscal(e, d);
                    _db.ProdutosFiscal.Add(e);
                }
            }
        }
    }

    /// <summary>Sync genérico para sub-tabelas por filial (não deleta registros de outras filiais).</summary>
    private void SyncPerFilial<TEntity, TDto>(
        ICollection<TEntity> existentes, List<TDto> dtos,
        Func<TDto, long?> getId, Func<TDto, long> getFilialId,
        Action<TEntity, TDto> update, Func<TDto, TEntity> create,
        DbSet<TEntity> dbSet) where TEntity : BaseEntity
    {
        // Filiais que vieram no DTO
        var filiaisDto = dtos.Select(getFilialId).ToHashSet();

        // Só remover registros das filiais que vieram no DTO e que não estão na lista
        var dtoIds = dtos.Where(d => getId(d).HasValue).Select(d => getId(d)!.Value).ToHashSet();
        foreach (var e in existentes.ToList())
        {
            var filialId = (long)e.GetType().GetProperty("FilialId")!.GetValue(e)!;
            if (filiaisDto.Contains(filialId) && !dtoIds.Contains(e.Id))
                dbSet.Remove(e);
        }

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

    private static void MapFiscal(ProdutoFiscal e, ProdutoFiscalDto d)
    {
        e.FilialId = d.FilialId;
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
        e.EstoqueDeposito = d.EstoqueDeposito;
        e.UltimaCompraUnitario = d.UltimaCompraUnitario; e.UltimaCompraSt = d.UltimaCompraSt;
        e.UltimaCompraOutros = d.UltimaCompraOutros; e.UltimaCompraIpi = d.UltimaCompraIpi;
        e.UltimaCompraFpc = d.UltimaCompraFpc; e.UltimaCompraBoleto = d.UltimaCompraBoleto;
        e.UltimaCompraDifal = d.UltimaCompraDifal; e.UltimaCompraFrete = d.UltimaCompraFrete;
        e.CustoMedio = d.CustoMedio; e.ProjecaoLucro = d.ProjecaoLucro;
        e.Markup = d.Markup; e.ValorVenda = d.ValorVenda; e.Pmc = d.Pmc;
        e.ValorPromocao = d.ValorPromocao; e.ValorPromocaoPrazo = d.ValorPromocaoPrazo;
        e.PromocaoInicio = d.PromocaoInicio;
        // Data fim = 23:59:59 do dia selecionado (promoção vale o dia inteiro)
        e.PromocaoFim = d.PromocaoFim.HasValue ? d.PromocaoFim.Value.Date.AddDays(1).AddSeconds(-1) : null;
        // Validar: fim não pode ser anterior ao início
        if (e.PromocaoInicio.HasValue && e.PromocaoFim.HasValue && e.PromocaoFim < e.PromocaoInicio)
            e.PromocaoFim = e.PromocaoInicio.Value.Date.AddDays(1).AddSeconds(-1);
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

    // ── Propagação de preço ────────────────────────────────────────

    /// <summary>Campos considerados "de preço" para propagação entre filiais.</summary>
    private static readonly string[] CamposPreco = {
        "ValorVenda", "ValorPromocao", "ValorPromocaoPrazo", "Pmc",
        "Markup", "ProjecaoLucro",
        "DescontoMinimo", "DescontoMaxSemSenha", "DescontoMaxComSenha",
        "Comissao", "ValorIncentivo"
    };

    private async Task PropagarPrecoAsync(Produto p, ProdutoFormDto dto)
    {
        // Buscar regra configurada
        var regraConfig = await _db.Configuracoes
            .FirstOrDefaultAsync(c => c.Chave == "produto.preco.regra");
        var regra = regraConfig?.Valor ?? "atual";

        // Se regra é "atual", não propaga
        if (regra == "atual") return;

        // Determinar filiais destino
        List<long> filiaisDestino;
        if (regra == "todas")
        {
            filiaisDestino = await _db.Filiais.Where(f => f.Ativo).Select(f => f.Id).ToListAsync();
        }
        else if (regra == "perguntar" && dto.FiliaisPrecoAplicar != null && dto.FiliaisPrecoAplicar.Count > 0)
        {
            filiaisDestino = dto.FiliaisPrecoAplicar;
        }
        else
        {
            return; // "perguntar" sem filiais selecionadas = não propagar
        }

        // Pegar os dados da filial que acabou de ser editada (primeiro do DTO que tenha valores)
        var dadosOrigem = dto.Dados.FirstOrDefault();
        if (dadosOrigem == null) return;

        // Filiais que já estão no DTO (já foram editadas diretamente) — não sobrescrever
        var filiaisNoDto = dto.Dados.Select(d => d.FilialId).ToHashSet();

        // Buscar ProdutoDados das filiais destino que NÃO estão no DTO
        var dadosOutras = await _db.ProdutosDados
            .Where(d => d.ProdutoId == p.Id && filiaisDestino.Contains(d.FilialId) && !filiaisNoDto.Contains(d.FilialId))
            .ToListAsync();

        foreach (var d in dadosOutras)
        {
            d.ValorVenda = dadosOrigem.ValorVenda;
            d.ValorPromocao = dadosOrigem.ValorPromocao;
            d.ValorPromocaoPrazo = dadosOrigem.ValorPromocaoPrazo;
            d.Pmc = dadosOrigem.Pmc;
            d.Markup = dadosOrigem.Markup;
            d.ProjecaoLucro = dadosOrigem.ProjecaoLucro;
            d.DescontoMinimo = dadosOrigem.DescontoMinimo;
            d.DescontoMaxSemSenha = dadosOrigem.DescontoMaxSemSenha;
            d.DescontoMaxComSenha = dadosOrigem.DescontoMaxComSenha;
            d.Comissao = dadosOrigem.Comissao;
            d.ValorIncentivo = dadosOrigem.ValorIncentivo;
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
            Id = f.Id, FilialId = f.FilialId, FornecedorId = f.FornecedorId,
            FornecedorNome = f.Fornecedor?.Pessoa?.Nome,
            CodigoProdutoFornecedor = f.CodigoProdutoFornecedor,
            NomeProduto = f.NomeProduto, Fracao = f.Fracao
        }).ToList(),
        Fiscais = p.Fiscais.OrderBy(f => f.FilialId).Select(f => new ProdutoFiscalDto
        {
            Id = f.Id, FilialId = f.FilialId, NcmId = f.NcmId,
            NcmCodigo = f.Ncm?.CodigoNcm,
            Cest = f.Cest, OrigemMercadoria = f.OrigemMercadoria,
            CstIcms = f.CstIcms, Csosn = f.Csosn, AliquotaIcms = f.AliquotaIcms,
            CstPis = f.CstPis, AliquotaPis = f.AliquotaPis,
            CstCofins = f.CstCofins, AliquotaCofins = f.AliquotaCofins,
            CstIpi = f.CstIpi, AliquotaIpi = f.AliquotaIpi
        }).ToList(),
        Dados = p.Dados.OrderBy(d => d.FilialId).Select(d => new ProdutoDadosDto
        {
            Id = d.Id, FilialId = d.FilialId,
            EstoqueAtual = d.EstoqueAtual, EstoqueMinimo = d.EstoqueMinimo,
            EstoqueMaximo = d.EstoqueMaximo, Demanda = d.Demanda, CurvaAbc = d.CurvaAbc,
            EstoqueDeposito = d.EstoqueDeposito,
            UltimaCompraUnitario = d.UltimaCompraUnitario, UltimaCompraSt = d.UltimaCompraSt,
            UltimaCompraOutros = d.UltimaCompraOutros, UltimaCompraIpi = d.UltimaCompraIpi,
            UltimaCompraFpc = d.UltimaCompraFpc, UltimaCompraBoleto = d.UltimaCompraBoleto,
            UltimaCompraDifal = d.UltimaCompraDifal, UltimaCompraFrete = d.UltimaCompraFrete,
            CustoMedio = d.CustoMedio, ProjecaoLucro = d.ProjecaoLucro,
            Markup = d.Markup, ValorVenda = d.ValorVenda, Pmc = d.Pmc,
            ValorPromocao = d.ValorPromocao, ValorPromocaoPrazo = d.ValorPromocaoPrazo,
            PromocaoInicio = d.PromocaoInicio, PromocaoFim = d.PromocaoFim,
            DescontoMinimo = d.DescontoMinimo, DescontoMaxSemSenha = d.DescontoMaxSemSenha,
            DescontoMaxComSenha = d.DescontoMaxComSenha,
            Comissao = d.Comissao, ValorIncentivo = d.ValorIncentivo,
            ProdutoLocalId = d.ProdutoLocalId, ProdutoLocalNome = d.ProdutoLocal?.Nome,
            SecaoId = d.SecaoId, SecaoNome = d.Secao?.Nome,
            ProdutoFamiliaId = d.ProdutoFamiliaId, ProdutoFamiliaNome = d.ProdutoFamilia?.Nome,
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
