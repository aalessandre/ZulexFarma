using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.PlanosContas;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class PlanoContaService : IPlanoContaService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Plano de Contas";
    private const string ENTIDADE = "PlanoConta";

    public PlanoContaService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<PlanoContaListDto>> ListarAsync()
    {
        try
        {
            var todos = await _db.PlanosContas
                .Include(p => p.ContaPai)
                .OrderBy(p => p.Ordem)
                .ToListAsync();

            // Monta códigos hierárquicos em memória
            var codigos = MontarCodigosHierarquicos(todos);

            return todos.Select(p => new PlanoContaListDto
            {
                Id = p.Id,
                Descricao = p.Descricao,
                Nivel = p.Nivel,
                NivelDescricao = p.Nivel switch
                {
                    NivelConta.Grupo => "Grupo",
                    NivelConta.SubGrupo => "SubGrupo",
                    NivelConta.PlanoConta => "Plano de Contas",
                    _ => ""
                },
                Natureza = p.Natureza,
                NaturezaDescricao = p.Natureza == NaturezaConta.Credito ? "Crédito" : "Débito",
                ContaPaiId = p.ContaPaiId,
                ContaPaiDescricao = p.ContaPai?.Descricao,
                Ordem = p.Ordem,
                CodigoHierarquico = codigos.GetValueOrDefault(p.Id, ""),
                VisivelRelatorio = p.VisivelRelatorio,
                Ativo = p.Ativo,
                CriadoEm = p.CriadoEm
            })
            .OrderBy(p => p.CodigoHierarquico)
            .ToList();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em PlanoContaService.ListarAsync"); throw; }
    }

    public async Task<PlanoContaListDto> CriarAsync(PlanoContaFormDto dto)
    {
        try
        {
            Validar(dto);
            await ValidarHierarquia(dto, null);

            var pc = new PlanoConta
            {
                Descricao = dto.Descricao.Trim().ToUpper(),
                Nivel = dto.Nivel,
                Natureza = dto.Natureza,
                ContaPaiId = dto.ContaPaiId,
                Ordem = dto.Ordem,
                VisivelRelatorio = dto.VisivelRelatorio,
                Ativo = dto.Ativo
            };
            _db.PlanosContas.Add(pc);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, pc.Id, novo: ParaDict(pc));

            return new PlanoContaListDto
            {
                Id = pc.Id,
                Descricao = pc.Descricao,
                Nivel = pc.Nivel,
                NivelDescricao = NivelParaTexto(pc.Nivel),
                Natureza = pc.Natureza,
                NaturezaDescricao = pc.Natureza == NaturezaConta.Credito ? "Crédito" : "Débito",
                ContaPaiId = pc.ContaPaiId,
                Ordem = pc.Ordem,
                VisivelRelatorio = pc.VisivelRelatorio,
                Ativo = pc.Ativo,
                CriadoEm = pc.CriadoEm
            };
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em PlanoContaService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, PlanoContaFormDto dto)
    {
        try
        {
            Validar(dto);
            var pc = await _db.PlanosContas.FindAsync(id)
                ?? throw new KeyNotFoundException($"Plano de Contas {id} não encontrado.");
            await ValidarHierarquia(dto, id);

            var anterior = ParaDict(pc);
            pc.Descricao = dto.Descricao.Trim().ToUpper();
            pc.Nivel = dto.Nivel;
            pc.Natureza = dto.Natureza;
            pc.ContaPaiId = dto.ContaPaiId;
            pc.Ordem = dto.Ordem;
            pc.VisivelRelatorio = dto.VisivelRelatorio;
            pc.Ativo = dto.Ativo;
            await _db.SaveChangesAsync();

            var novo = ParaDict(pc);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em PlanoContaService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var pc = await _db.PlanosContas.FindAsync(id)
                ?? throw new KeyNotFoundException($"Plano de Contas {id} não encontrado.");

            // Verifica se tem filhos
            var temFilhos = await _db.PlanosContas.AnyAsync(x => x.ContaPaiId == id);
            if (temFilhos) throw new ArgumentException("Não é possível excluir: existem registros filhos vinculados.");

            var dados = ParaDict(pc);
            _db.PlanosContas.Remove(pc);
            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var recarregado = await _db.PlanosContas.FindAsync(id);
                recarregado!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em PlanoContaService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    // ── Validações ─────────────────────────────────────────────────
    private static void Validar(PlanoContaFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Descricao)) throw new ArgumentException("Descrição é obrigatória.");
        if (dto.Ordem < 1) throw new ArgumentException("Ordem deve ser maior que zero.");
        if (!Enum.IsDefined(dto.Nivel)) throw new ArgumentException("Nível inválido.");
        if (!Enum.IsDefined(dto.Natureza)) throw new ArgumentException("Natureza inválida.");
    }

    private async Task ValidarHierarquia(PlanoContaFormDto dto, long? idAtual)
    {
        if (dto.Nivel == NivelConta.Grupo)
        {
            if (dto.ContaPaiId.HasValue) throw new ArgumentException("Grupo não pode ter conta pai.");
            return;
        }

        if (!dto.ContaPaiId.HasValue) throw new ArgumentException("SubGrupo e Plano de Contas precisam de uma conta pai.");

        if (idAtual.HasValue && dto.ContaPaiId == idAtual)
            throw new ArgumentException("Uma conta não pode ser pai de si mesma.");

        var pai = await _db.PlanosContas.FindAsync(dto.ContaPaiId.Value)
            ?? throw new ArgumentException("Conta pai não encontrada.");

        if (dto.Nivel == NivelConta.SubGrupo && pai.Nivel != NivelConta.Grupo)
            throw new ArgumentException("SubGrupo deve estar vinculado a um Grupo.");

        if (dto.Nivel == NivelConta.PlanoConta && pai.Nivel != NivelConta.SubGrupo)
            throw new ArgumentException("Plano de Contas deve estar vinculado a um SubGrupo.");
    }

    // ── Código hierárquico ─────────────────────────────────────────
    private static Dictionary<long, string> MontarCodigosHierarquicos(List<PlanoConta> todos)
    {
        var result = new Dictionary<long, string>();

        // Grupos (nível 1) — código = ordem
        var grupos = todos.Where(p => p.Nivel == NivelConta.Grupo).OrderBy(p => p.Ordem).ToList();
        foreach (var g in grupos)
        {
            result[g.Id] = $"{g.Ordem}";

            // SubGrupos (nível 2) — código = grupo.ordem.subgrupo.ordem
            var subs = todos.Where(p => p.Nivel == NivelConta.SubGrupo && p.ContaPaiId == g.Id).OrderBy(p => p.Ordem).ToList();
            foreach (var s in subs)
            {
                result[s.Id] = $"{g.Ordem}.{s.Ordem}";

                // Planos (nível 3) — código = grupo.ordem.sub.ordem.plano.ordem(2 dígitos)
                var planos = todos.Where(p => p.Nivel == NivelConta.PlanoConta && p.ContaPaiId == s.Id).OrderBy(p => p.Ordem).ToList();
                foreach (var p in planos)
                {
                    result[p.Id] = $"{g.Ordem}.{s.Ordem}.{p.Ordem:D2}";
                }
            }
        }

        return result;
    }

    // ── Helpers ─────────────────────────────────────────────────────
    private static string NivelParaTexto(NivelConta nivel) => nivel switch
    {
        NivelConta.Grupo => "Grupo",
        NivelConta.SubGrupo => "SubGrupo",
        NivelConta.PlanoConta => "Plano de Contas",
        _ => ""
    };

    private static Dictionary<string, string?> ParaDict(PlanoConta p) => new()
    {
        ["Descricao"] = p.Descricao,
        ["Nivel"] = NivelParaTexto(p.Nivel),
        ["Natureza"] = p.Natureza == NaturezaConta.Credito ? "Crédito" : "Débito",
        ["ContaPaiId"] = p.ContaPaiId?.ToString(),
        ["Ordem"] = p.Ordem.ToString(),
        ["VisivelRelatorio"] = p.VisivelRelatorio ? "Sim" : "Não",
        ["Ativo"] = p.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b) =>
        a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
