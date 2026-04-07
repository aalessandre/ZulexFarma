using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.TiposPagamento;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class TipoPagamentoService : ITipoPagamentoService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Tipos de Pagamento";
    private const string ENTIDADE = "TipoPagamento";

    public TipoPagamentoService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<TipoPagamentoListDto>> ListarAsync()
    {
        try
        {
            return await _db.TiposPagamento.OrderBy(t => t.Nome)
                .Select(t => new TipoPagamentoListDto
                {
                    Id = t.Id,
                    Nome = t.Nome,
                    Modalidade = t.Modalidade,
                    ModalidadeDescricao = ModalidadeParaTexto(t.Modalidade),
                    DescontoMinimo = t.DescontoMinimo,
                    DescontoMaxSemSenha = t.DescontoMaxSemSenha,
                    DescontoMaxComSenha = t.DescontoMaxComSenha,
                    AceitaPromocao = t.AceitaPromocao,
                    Ativo = t.Ativo,
                    CriadoEm = t.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em TipoPagamentoService.ListarAsync"); throw; }
    }

    public async Task<TipoPagamentoListDto> CriarAsync(TipoPagamentoFormDto dto)
    {
        try
        {
            Validar(dto);
            var tp = new TipoPagamento
            {
                Nome = dto.Nome.Trim().ToUpper(),
                Modalidade = dto.Modalidade,
                DescontoMinimo = dto.DescontoMinimo,
                DescontoMaxSemSenha = dto.DescontoMaxSemSenha,
                DescontoMaxComSenha = dto.DescontoMaxComSenha,
                AceitaPromocao = dto.AceitaPromocao,
                Ativo = dto.Ativo
            };
            _db.TiposPagamento.Add(tp);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, tp.Id, novo: ParaDict(tp));
            return new TipoPagamentoListDto
            {
                Id = tp.Id, Nome = tp.Nome, Modalidade = tp.Modalidade,
                ModalidadeDescricao = ModalidadeParaTexto(tp.Modalidade),
                DescontoMaxSemSenha = tp.DescontoMaxSemSenha, DescontoMaxComSenha = tp.DescontoMaxComSenha,
                AceitaPromocao = tp.AceitaPromocao, Ativo = tp.Ativo, CriadoEm = tp.CriadoEm
            };
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em TipoPagamentoService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, TipoPagamentoFormDto dto)
    {
        try
        {
            Validar(dto);
            var tp = await _db.TiposPagamento.FindAsync(id)
                ?? throw new KeyNotFoundException($"Tipo de pagamento {id} não encontrado.");
            var anterior = ParaDict(tp);
            tp.Nome = dto.Nome.Trim().ToUpper();
            tp.Modalidade = dto.Modalidade;
            tp.DescontoMinimo = dto.DescontoMinimo;
            tp.DescontoMaxSemSenha = dto.DescontoMaxSemSenha;
            tp.DescontoMaxComSenha = dto.DescontoMaxComSenha;
            tp.AceitaPromocao = dto.AceitaPromocao;
            tp.Ativo = dto.Ativo;
            await _db.SaveChangesAsync();
            var novo = ParaDict(tp);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em TipoPagamentoService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var tp = await _db.TiposPagamento.FindAsync(id)
                ?? throw new KeyNotFoundException($"Tipo de pagamento {id} não encontrado.");
            var dados = ParaDict(tp);
            _db.TiposPagamento.Remove(tp);
            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var recarregado = await _db.TiposPagamento.FindAsync(id);
                recarregado!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException) { Log.Error(ex, "Erro em TipoPagamentoService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    private static void Validar(TipoPagamentoFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
        if (!Enum.IsDefined(dto.Modalidade)) throw new ArgumentException("Modalidade inválida.");
        if (dto.DescontoMaxSemSenha < 0 || dto.DescontoMaxSemSenha > 90) throw new ArgumentException("Desconto sem senha deve ser entre 0% e 90%.");
        if (dto.DescontoMaxComSenha < 0 || dto.DescontoMaxComSenha > 90) throw new ArgumentException("Desconto com senha deve ser entre 0% e 90%.");
    }

    private static string ModalidadeParaTexto(ModalidadePagamento m) => m switch
    {
        ModalidadePagamento.VendaVista => "Venda à Vista",
        ModalidadePagamento.VendaCartao => "Venda Cartão",
        ModalidadePagamento.VendaPix => "Venda PIX",
        ModalidadePagamento.VendaPrazo => "Venda a Prazo",
        ModalidadePagamento.Voucher => "Voucher",
        ModalidadePagamento.Outros => "Outros",
        _ => ""
    };

    private static Dictionary<string, string?> ParaDict(TipoPagamento t) => new()
    {
        ["Nome"] = t.Nome,
        ["Modalidade"] = ModalidadeParaTexto(t.Modalidade),
        ["DescontoMinimo"] = $"{t.DescontoMinimo:N1}%",
        ["DescontoMaxSemSenha"] = $"{t.DescontoMaxSemSenha:N1}%",
        ["DescontoMaxComSenha"] = $"{t.DescontoMaxComSenha:N1}%",
        ["AceitaPromocao"] = t.AceitaPromocao ? "Sim" : "Não",
        ["Ativo"] = t.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b) =>
        a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
