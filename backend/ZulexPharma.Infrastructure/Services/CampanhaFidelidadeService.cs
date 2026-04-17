using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Fidelidade;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class CampanhaFidelidadeService : ICampanhaFidelidadeService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Fidelidade";
    private const string ENTIDADE = "CampanhaFidelidade";

    public CampanhaFidelidadeService(AppDbContext db, ILogAcaoService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<List<CampanhaFidelidadeListDto>> ListarAsync(TipoFidelidade? tipo = null)
    {
        try
        {
            var q = _db.CampanhasFidelidade.AsQueryable();
            if (tipo.HasValue) q = q.Where(c => c.Tipo == tipo.Value);
            return await q.OrderByDescending(c => c.Ativo).ThenBy(c => c.Nome)
                .Select(c => new CampanhaFidelidadeListDto
                {
                    Id = c.Id, Codigo = c.Codigo, Nome = c.Nome,
                    Tipo = c.Tipo, ModoContagem = c.ModoContagem,
                    ValorBase = c.ValorBase, PontosGanhos = c.PontosGanhos,
                    PercentualCashback = c.PercentualCashback,
                    FormaRetirada = c.FormaRetirada,
                    DiasValidadePontos = c.DiasValidadePontos,
                    LimiarAlerta = c.LimiarAlerta,
                    DataHoraInicio = c.DataHoraInicio, DataHoraFim = c.DataHoraFim,
                    Ativo = c.Ativo, CriadoEm = c.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em CampanhaFidelidadeService.ListarAsync"); throw; }
    }

    public async Task<CampanhaFidelidadeDetalheDto?> ObterAsync(long id)
    {
        try
        {
            var c = await _db.CampanhasFidelidade
                .Include(x => x.Filiais)
                .Include(x => x.Pagamentos)
                .Include(x => x.Itens).ThenInclude(i => i.GrupoPrincipal)
                .Include(x => x.Itens).ThenInclude(i => i.GrupoProduto)
                .Include(x => x.Itens).ThenInclude(i => i.SubGrupo)
                .Include(x => x.Itens).ThenInclude(i => i.Secao)
                .Include(x => x.Itens).ThenInclude(i => i.ProdutoFamilia)
                .Include(x => x.Itens).ThenInclude(i => i.Fabricante)
                .Include(x => x.Itens).ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return null;

            return new CampanhaFidelidadeDetalheDto
            {
                Id = c.Id, Codigo = c.Codigo, Nome = c.Nome, Descricao = c.Descricao,
                Tipo = c.Tipo, ModoContagem = c.ModoContagem,
                ValorBase = c.ValorBase, PontosGanhos = c.PontosGanhos,
                PercentualCashback = c.PercentualCashback,
                FormaRetirada = c.FormaRetirada, ValorPorPonto = c.ValorPorPonto,
                DiasValidadePontos = c.DiasValidadePontos, LimiarAlerta = c.LimiarAlerta,
                DataHoraInicio = c.DataHoraInicio, DataHoraFim = c.DataHoraFim,
                DiaSemana = c.DiaSemana, HoraInicio = c.HoraInicio, HoraFim = c.HoraFim,
                Ativo = c.Ativo, CriadoEm = c.CriadoEm,
                FilialIds = c.Filiais.Select(f => f.FilialId).ToList(),
                TipoPagamentoIds = c.Pagamentos.Select(p => p.TipoPagamentoId).ToList(),
                Itens = c.Itens.Select(i => new CampanhaFidelidadeItemDto
                {
                    Id = i.Id,
                    GrupoPrincipalId = i.GrupoPrincipalId,
                    GrupoProdutoId = i.GrupoProdutoId,
                    SubGrupoId = i.SubGrupoId,
                    SecaoId = i.SecaoId,
                    ProdutoFamiliaId = i.ProdutoFamiliaId,
                    FabricanteId = i.FabricanteId,
                    ProdutoId = i.ProdutoId,
                    Incluir = i.Incluir,
                    Descricao = DescreverItem(i),
                    ValorVendaReferencia = i.ValorVendaReferencia,
                    PercentualCashbackItem = i.PercentualCashbackItem,
                    ValorCashbackItem = i.ValorCashbackItem
                }).ToList()
            };
        }
        catch (Exception ex) { Log.Error(ex, "Erro em CampanhaFidelidadeService.ObterAsync | Id: {Id}", id); throw; }
    }

    public async Task<CampanhaFidelidadeListDto> CriarAsync(CampanhaFidelidadeFormDto dto)
    {
        try
        {
            Validar(dto);
            var c = new CampanhaFidelidade();
            Aplicar(c, dto);
            AplicarRelacionamentos(c, dto);
            _db.CampanhasFidelidade.Add(c);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, c.Id, novo: ParaDict(c));
            return (await ListarAsync(null)).First(x => x.Id == c.Id);
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em CampanhaFidelidadeService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, CampanhaFidelidadeFormDto dto)
    {
        try
        {
            Validar(dto);
            var c = await _db.CampanhasFidelidade
                .Include(x => x.Filiais)
                .Include(x => x.Pagamentos)
                .Include(x => x.Itens)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException($"Campanha {id} não encontrada.");

            var anterior = ParaDict(c);
            Aplicar(c, dto);

            // Recria os relacionamentos do zero (mais simples que fazer diff)
            _db.CampanhasFidelidadeFiliais.RemoveRange(c.Filiais);
            _db.CampanhasFidelidadePagamentos.RemoveRange(c.Pagamentos);
            _db.CampanhasFidelidadeItens.RemoveRange(c.Itens);
            c.Filiais.Clear();
            c.Pagamentos.Clear();
            c.Itens.Clear();
            AplicarRelacionamentos(c, dto);

            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: ParaDict(c));
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em CampanhaFidelidadeService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var c = await _db.CampanhasFidelidade.FindAsync(id)
                ?? throw new KeyNotFoundException($"Campanha {id} não encontrada.");
            var dados = ParaDict(c);
            _db.CampanhasFidelidade.Remove(c);
            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var recarregado = await _db.CampanhasFidelidade.FindAsync(id);
                recarregado!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException) { Log.Error(ex, "Erro em CampanhaFidelidadeService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    // ─── Helpers ──────────────────────────────────────────────────

    private static void Validar(CampanhaFidelidadeFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
        if (dto.Tipo == TipoFidelidade.Pontos && dto.ValorBase <= 0)
            throw new ArgumentException("Valor base deve ser maior que zero.");

        if (dto.Tipo == TipoFidelidade.Pontos)
        {
            if (dto.PontosGanhos <= 0) throw new ArgumentException("Quantidade de pontos deve ser maior que zero.");
            if (dto.FormaRetirada == FormaRetiradaPontos.DescontoNaVenda && dto.ValorPorPonto <= 0)
                throw new ArgumentException("Para forma 'Desconto na Venda', informe o valor de cada ponto.");
        }
        // Cashback: validação de itens é no frontend (valores por item)

        if (dto.DataHoraFim.HasValue && dto.DataHoraFim.Value < dto.DataHoraInicio)
            throw new ArgumentException("Data fim não pode ser anterior à data início.");
    }

    private static void Aplicar(CampanhaFidelidade c, CampanhaFidelidadeFormDto dto)
    {
        c.Nome = dto.Nome.Trim().ToUpper();
        c.Descricao = dto.Descricao?.Trim();
        c.Tipo = dto.Tipo;
        c.ModoContagem = dto.ModoContagem;
        c.ValorBase = dto.ValorBase;
        c.PontosGanhos = dto.Tipo == TipoFidelidade.Pontos ? dto.PontosGanhos : 0;
        c.PercentualCashback = dto.Tipo == TipoFidelidade.Cashback ? dto.PercentualCashback : 0;
        c.FormaRetirada = dto.FormaRetirada;
        c.ValorPorPonto = dto.ValorPorPonto;
        c.DiasValidadePontos = dto.DiasValidadePontos;
        c.LimiarAlerta = dto.LimiarAlerta;
        c.DataHoraInicio = dto.DataHoraInicio;
        c.DataHoraFim = dto.DataHoraFim;
        c.DiaSemana = dto.DiaSemana == 0 ? 127 : dto.DiaSemana;
        c.HoraInicio = dto.HoraInicio;
        c.HoraFim = dto.HoraFim;
        c.Ativo = dto.Ativo;
    }

    private static void AplicarRelacionamentos(CampanhaFidelidade c, CampanhaFidelidadeFormDto dto)
    {
        foreach (var fid in dto.FilialIds.Distinct())
            c.Filiais.Add(new CampanhaFidelidadeFilial { FilialId = fid });
        foreach (var tp in dto.TipoPagamentoIds.Distinct())
            c.Pagamentos.Add(new CampanhaFidelidadePagamento { TipoPagamentoId = tp });
        foreach (var it in dto.Itens)
        {
            c.Itens.Add(new CampanhaFidelidadeItem
            {
                GrupoPrincipalId = it.GrupoPrincipalId,
                GrupoProdutoId = it.GrupoProdutoId,
                SubGrupoId = it.SubGrupoId,
                SecaoId = it.SecaoId,
                ProdutoFamiliaId = it.ProdutoFamiliaId,
                FabricanteId = it.FabricanteId,
                ProdutoId = it.ProdutoId,
                Incluir = it.Incluir,
                ValorVendaReferencia = it.ValorVendaReferencia,
                PercentualCashbackItem = it.PercentualCashbackItem,
                ValorCashbackItem = it.ValorCashbackItem
            });
        }
    }

    private static string? DescreverItem(CampanhaFidelidadeItem i)
    {
        if (i.Produto != null) return $"Produto: {i.Produto.Nome}";
        if (i.Fabricante != null) return $"Fabricante: {i.Fabricante.Nome}";
        if (i.GrupoPrincipal != null) return $"Grupo Principal: {i.GrupoPrincipal.Nome}";
        if (i.GrupoProduto != null) return $"Grupo: {i.GrupoProduto.Nome}";
        if (i.SubGrupo != null) return $"SubGrupo: {i.SubGrupo.Nome}";
        if (i.Secao != null) return $"Seção: {i.Secao.Nome}";
        if (i.ProdutoFamilia != null) return $"Família: {i.ProdutoFamilia.Nome}";
        return null;
    }

    private static Dictionary<string, string?> ParaDict(CampanhaFidelidade c) => new()
    {
        ["Nome"] = c.Nome,
        ["Tipo"] = c.Tipo.ToString(),
        ["Modo"] = c.ModoContagem.ToString(),
        ["Valor Base"] = c.ValorBase.ToString("N2"),
        ["Pontos"] = c.PontosGanhos.ToString(),
        ["Cashback %"] = c.PercentualCashback.ToString("N2"),
        ["Ativo"] = c.Ativo ? "Sim" : "Não"
    };
}
