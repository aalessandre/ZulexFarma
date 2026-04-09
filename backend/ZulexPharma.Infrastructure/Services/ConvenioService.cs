using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Convenios;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class ConvenioService : IConvenioService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Convênios";
    private const string ENTIDADE = "Convenio";

    public ConvenioService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<ConvenioListDto>> ListarAsync()
    {
        try
        {
            return await _db.Convenios
                .Include(c => c.Pessoa)
                .OrderBy(c => c.Pessoa.Nome)
                .Select(c => new ConvenioListDto
                {
                    Id = c.Id,
                    PessoaId = c.PessoaId,
                    PessoaNome = c.Pessoa.Nome,
                    PessoaCpfCnpj = c.Pessoa.CpfCnpj,
                    PessoaTipo = c.Pessoa.Tipo,
                    Aviso = c.Aviso,
                    ModoFechamento = c.ModoFechamento,
                    ModoFechamentoDescricao = c.ModoFechamento == ModoFechamento.DiasCorridos ? "Dias Corridos" : "Por Fechamento",
                    LimiteCredito = c.LimiteCredito,
                    Bloqueado = c.Bloqueado,
                    Ativo = c.Ativo,
                    CriadoEm = c.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ConvenioService.ListarAsync"); throw; }
    }

    public async Task<ConvenioDetalheDto?> ObterAsync(long id)
    {
        try
        {
            var c = await _db.Convenios
                .Include(x => x.Pessoa)
                .Include(x => x.Descontos)
                .Include(x => x.Bloqueios).ThenInclude(b => b.TipoPagamento)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return null;

            return new ConvenioDetalheDto
            {
                Id = c.Id,
                PessoaId = c.PessoaId,
                PessoaNome = c.Pessoa.Nome,
                PessoaCpfCnpj = c.Pessoa.CpfCnpj,
                PessoaTipo = c.Pessoa.Tipo,
                PessoaRazaoSocial = c.Pessoa.RazaoSocial,
                PessoaIeRg = c.Pessoa.Tipo == "J" ? c.Pessoa.InscricaoEstadual : c.Pessoa.Rg,
                Aviso = c.Aviso,
                Observacao = c.Observacao,
                ModoFechamento = c.ModoFechamento,
                DiasCorridos = c.DiasCorridos,
                DiaFechamento = c.DiaFechamento,
                DiaVencimento = c.DiaVencimento,
                MesesParaVencimento = c.MesesParaVencimento,
                QtdeViasCupom = c.QtdeViasCupom,
                Bloqueado = c.Bloqueado,
                BloquearVendaParcelada = c.BloquearVendaParcelada,
                BloquearDescontoParcelada = c.BloquearDescontoParcelada,
                BloquearComissao = c.BloquearComissao,
                VenderSomenteComSenha = c.VenderSomenteComSenha,
                CobrarJurosAtraso = c.CobrarJurosAtraso,
                DiasCarenciaBloqueio = c.DiasCarenciaBloqueio,
                LimiteCredito = c.LimiteCredito,
                MaximoParcelas = c.MaximoParcelas,
                Ativo = c.Ativo,
                CriadoEm = c.CriadoEm,
                Descontos = c.Descontos.Select(d => new ConvenioDescontoDto
                {
                    Id = d.Id,
                    TipoAgrupador = d.TipoAgrupador,
                    AgrupadorId = d.AgrupadorId,
                    AgrupadorNome = d.AgrupadorNome,
                    DescontoMinimo = d.DescontoMinimo,
                    DescontoMaxSemSenha = d.DescontoMaxSemSenha,
                    DescontoMaxComSenha = d.DescontoMaxComSenha
                }).ToList(),
                Bloqueios = c.Bloqueios.Select(b => new ConvenioBloqueioDto
                {
                    TipoPagamentoId = b.TipoPagamentoId,
                    TipoPagamentoNome = b.TipoPagamento.Nome
                }).ToList()
            };
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ConvenioService.ObterAsync | Id: {Id}", id); throw; }
    }

    public async Task<ConvenioListDto> CriarAsync(ConvenioFormDto dto)
    {
        try
        {
            Validar(dto);
            await ResolverPessoa(dto);
            var conv = new Convenio
            {
                PessoaId = dto.PessoaId,
                Aviso = dto.Aviso?.Trim(),
                Observacao = dto.Observacao?.Trim(),
                ModoFechamento = dto.ModoFechamento,
                DiasCorridos = dto.ModoFechamento == ModoFechamento.DiasCorridos ? dto.DiasCorridos : null,
                DiaFechamento = dto.ModoFechamento == ModoFechamento.PorFechamento ? dto.DiaFechamento : null,
                DiaVencimento = dto.ModoFechamento == ModoFechamento.PorFechamento ? dto.DiaVencimento : null,
                MesesParaVencimento = dto.MesesParaVencimento,
                QtdeViasCupom = Math.Clamp(dto.QtdeViasCupom, 1, 2),
                Bloqueado = dto.Bloqueado,
                BloquearVendaParcelada = dto.BloquearVendaParcelada,
                BloquearDescontoParcelada = dto.BloquearDescontoParcelada,
                BloquearComissao = dto.BloquearComissao,
                VenderSomenteComSenha = dto.VenderSomenteComSenha,
                CobrarJurosAtraso = dto.CobrarJurosAtraso,
                DiasCarenciaBloqueio = dto.DiasCarenciaBloqueio,
                LimiteCredito = dto.LimiteCredito,
                MaximoParcelas = Math.Max(1, dto.MaximoParcelas),
                Ativo = dto.Ativo
            };

            // Descontos
            foreach (var d in dto.Descontos)
            {
                conv.Descontos.Add(new ConvenioDesconto
                {
                    TipoAgrupador = d.TipoAgrupador,
                    AgrupadorId = d.AgrupadorId,
                    AgrupadorNome = d.AgrupadorNome,
                    DescontoMinimo = d.DescontoMinimo,
                    DescontoMaxSemSenha = d.DescontoMaxSemSenha,
                    DescontoMaxComSenha = d.DescontoMaxComSenha
                });
            }

            // Bloqueios
            foreach (var tpId in dto.BloqueioTipoPagamentoIds.Distinct())
            {
                conv.Bloqueios.Add(new ConvenioBloqueio { TipoPagamentoId = tpId });
            }

            _db.Convenios.Add(conv);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, conv.Id, novo: ParaDict(conv));

            var pessoa = await _db.Pessoas.FindAsync(conv.PessoaId);
            return new ConvenioListDto
            {
                Id = conv.Id, PessoaId = conv.PessoaId, PessoaNome = pessoa?.Nome ?? "",
                PessoaCpfCnpj = pessoa?.CpfCnpj, PessoaTipo = pessoa?.Tipo,
                Aviso = conv.Aviso, ModoFechamento = conv.ModoFechamento,
                ModoFechamentoDescricao = conv.ModoFechamento == ModoFechamento.DiasCorridos ? "Dias Corridos" : "Por Fechamento",
                LimiteCredito = conv.LimiteCredito, Bloqueado = conv.Bloqueado,
                Ativo = conv.Ativo, CriadoEm = conv.CriadoEm
            };
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em ConvenioService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, ConvenioFormDto dto)
    {
        try
        {
            Validar(dto);
            await ResolverPessoa(dto);
            var conv = await _db.Convenios
                .Include(c => c.Descontos)
                .Include(c => c.Bloqueios)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new KeyNotFoundException($"Convênio {id} não encontrado.");

            var anterior = ParaDict(conv);

            conv.PessoaId = dto.PessoaId;
            conv.Aviso = dto.Aviso?.Trim();
            conv.Observacao = dto.Observacao?.Trim();
            conv.ModoFechamento = dto.ModoFechamento;
            conv.DiasCorridos = dto.ModoFechamento == ModoFechamento.DiasCorridos ? dto.DiasCorridos : null;
            conv.DiaFechamento = dto.ModoFechamento == ModoFechamento.PorFechamento ? dto.DiaFechamento : null;
            conv.DiaVencimento = dto.ModoFechamento == ModoFechamento.PorFechamento ? dto.DiaVencimento : null;
            conv.MesesParaVencimento = dto.MesesParaVencimento;
            conv.QtdeViasCupom = Math.Clamp(dto.QtdeViasCupom, 1, 2);
            conv.Bloqueado = dto.Bloqueado;
            conv.BloquearVendaParcelada = dto.BloquearVendaParcelada;
            conv.BloquearDescontoParcelada = dto.BloquearDescontoParcelada;
            conv.BloquearComissao = dto.BloquearComissao;
            conv.VenderSomenteComSenha = dto.VenderSomenteComSenha;
            conv.CobrarJurosAtraso = dto.CobrarJurosAtraso;
            conv.DiasCarenciaBloqueio = dto.DiasCarenciaBloqueio;
            conv.LimiteCredito = dto.LimiteCredito;
            conv.MaximoParcelas = Math.Max(1, dto.MaximoParcelas);
            conv.Ativo = dto.Ativo;

            // Recriar descontos
            _db.Set<ConvenioDesconto>().RemoveRange(conv.Descontos);
            foreach (var d in dto.Descontos)
            {
                conv.Descontos.Add(new ConvenioDesconto
                {
                    TipoAgrupador = d.TipoAgrupador,
                    AgrupadorId = d.AgrupadorId,
                    AgrupadorNome = d.AgrupadorNome,
                    DescontoMinimo = d.DescontoMinimo,
                    DescontoMaxSemSenha = d.DescontoMaxSemSenha,
                    DescontoMaxComSenha = d.DescontoMaxComSenha
                });
            }

            // Recriar bloqueios
            _db.Set<ConvenioBloqueio>().RemoveRange(conv.Bloqueios);
            foreach (var tpId in dto.BloqueioTipoPagamentoIds.Distinct())
            {
                conv.Bloqueios.Add(new ConvenioBloqueio { TipoPagamentoId = tpId });
            }

            await _db.SaveChangesAsync();
            var novo = ParaDict(conv);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em ConvenioService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var conv = await _db.Convenios
                .Include(c => c.Descontos)
                .Include(c => c.Bloqueios)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new KeyNotFoundException($"Convênio {id} não encontrado.");
            var dados = ParaDict(conv);
            _db.Set<ConvenioDesconto>().RemoveRange(conv.Descontos);
            _db.Set<ConvenioBloqueio>().RemoveRange(conv.Bloqueios);
            _db.Convenios.Remove(conv);
            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var recarregado = await _db.Convenios.FindAsync(id);
                recarregado!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException) { Log.Error(ex, "Erro em ConvenioService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    /// <summary>
    /// Se PessoaId == 0, busca ou cria a Pessoa pelo CPF/CNPJ.
    /// </summary>
    private async Task ResolverPessoa(ConvenioFormDto dto)
    {
        if (dto.PessoaId > 0) return;

        var cpfCnpj = CpfCnpjHelper.SomenteDigitos(dto.CpfCnpj);
        if (string.IsNullOrWhiteSpace(cpfCnpj))
            throw new ArgumentException("CPF/CNPJ é obrigatório.");
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new ArgumentException(dto.Tipo == "J" ? "Nome Fantasia é obrigatório." : "Nome é obrigatório.");

        // Busca pessoa existente
        var existente = await _db.Pessoas.FirstOrDefaultAsync(p => p.CpfCnpj == cpfCnpj);
        if (existente != null)
        {
            dto.PessoaId = existente.Id;
            return;
        }

        // Cria nova pessoa
        var pessoa = new Pessoa
        {
            Tipo = dto.Tipo ?? "J",
            Nome = dto.Nome!.Trim().ToUpper(),
            RazaoSocial = dto.RazaoSocial?.Trim().ToUpper(),
            CpfCnpj = cpfCnpj,
            InscricaoEstadual = dto.InscricaoEstadual?.Trim(),
            Rg = dto.Rg?.Trim()
        };
        _db.Pessoas.Add(pessoa);
        await _db.SaveChangesAsync();
        dto.PessoaId = pessoa.Id;
    }

    private static void Validar(ConvenioFormDto dto)
    {
        if (dto.PessoaId <= 0 && string.IsNullOrWhiteSpace(dto.CpfCnpj))
            throw new ArgumentException("CPF/CNPJ é obrigatório.");
        if (dto.ModoFechamento == ModoFechamento.DiasCorridos && (dto.DiasCorridos == null || dto.DiasCorridos < 1))
            throw new ArgumentException("Informe a quantidade de dias corridos.");
        if (dto.ModoFechamento == ModoFechamento.PorFechamento)
        {
            if (dto.DiaFechamento == null || dto.DiaFechamento < 1 || dto.DiaFechamento > 28)
                throw new ArgumentException("Dia de fechamento deve ser entre 1 e 28.");
            if (dto.DiaVencimento == null || dto.DiaVencimento < 1 || dto.DiaVencimento > 28)
                throw new ArgumentException("Dia de vencimento deve ser entre 1 e 28.");
        }
    }

    private static Dictionary<string, string?> ParaDict(Convenio c) => new()
    {
        ["PessoaId"] = c.PessoaId.ToString(),
        ["ModoFechamento"] = c.ModoFechamento == ModoFechamento.DiasCorridos ? "Dias Corridos" : "Por Fechamento",
        ["DiasCorridos"] = c.DiasCorridos?.ToString(),
        ["DiaFechamento"] = c.DiaFechamento?.ToString(),
        ["DiaVencimento"] = c.DiaVencimento?.ToString(),
        ["MesesParaVencimento"] = c.MesesParaVencimento.ToString(),
        ["LimiteCredito"] = c.LimiteCredito.ToString("N2"),
        ["MaximoParcelas"] = c.MaximoParcelas.ToString(),
        ["Bloqueado"] = c.Bloqueado ? "Sim" : "Não",
        ["Ativo"] = c.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b) =>
        a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
