using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Filiais;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class FilialService : IFilialService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;

    private const string TELA    = "Filiais";
    private const string ENTIDADE = "Filial";

    public FilialService(AppDbContext db, ILogAcaoService log)
    {
        _db  = db;
        _log = log;
    }

    public async Task<List<FilialListDto>> ListarAsync()
    {
        try
        {
            return await _db.Filiais
                .OrderBy(f => f.NomeFilial)
                .Select(f => new FilialListDto
                {
                    Id               = f.Id,
                    NomeFilial       = f.NomeFilial,
                    RazaoSocial      = f.RazaoSocial,
                    NomeFantasia     = f.NomeFantasia,
                    Cnpj             = f.Cnpj,
                    InscricaoEstadual = f.InscricaoEstadual,
                    Cep              = f.Cep,
                    Rua              = f.Rua,
                    Numero           = f.Numero,
                    Bairro           = f.Bairro,
                    Cidade           = f.Cidade,
                    Uf               = f.Uf,
                    Telefone         = f.Telefone,
                    Email            = f.Email,
                    AliquotaIcms     = f.AliquotaIcms,
                    CriadoEm         = f.CriadoEm,
                    Ativo            = f.Ativo
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FilialService.ListarAsync");
            throw;
        }
    }

    public async Task<FilialListDto> CriarAsync(FilialFormDto dto)
    {
        try
        {
            ValidarCnpj(dto.Cnpj);

            var filial = new Filial
            {
                NomeFilial        = Mai(dto.NomeFilial),
                RazaoSocial       = Mai(dto.RazaoSocial),
                NomeFantasia      = Mai(dto.NomeFantasia),
                Cnpj              = dto.Cnpj.Trim(),
                InscricaoEstadual = dto.InscricaoEstadual?.Trim().ToUpper(),
                Cep               = dto.Cep.Trim(),
                Rua               = Mai(dto.Rua),
                Numero            = dto.Numero.Trim().ToUpper(),
                Bairro            = Mai(dto.Bairro),
                Cidade            = Mai(dto.Cidade),
                Uf                = dto.Uf.Trim().ToUpper(),
                Telefone          = dto.Telefone.Trim(),
                Email             = dto.Email.Trim().ToLower(),
                AliquotaIcms      = dto.AliquotaIcms,
                Ativo             = dto.Ativo
            };

            _db.Filiais.Add(filial);
            await _db.SaveChangesAsync();

            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, filial.Id,
                novo: FilialParaDict(filial));

            return MapToListDto(filial);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            Log.Error(ex, "Erro em FilialService.CriarAsync");
            throw;
        }
    }

    public async Task AtualizarAsync(long id, FilialFormDto dto)
    {
        try
        {
            var filial = await _db.Filiais.FindAsync(id)
                ?? throw new KeyNotFoundException($"Filial {id} não encontrada.");

            ValidarCnpj(dto.Cnpj, id);

            var anterior = FilialParaDict(filial);

            filial.NomeFilial        = Mai(dto.NomeFilial);
            filial.RazaoSocial       = Mai(dto.RazaoSocial);
            filial.NomeFantasia      = Mai(dto.NomeFantasia);
            filial.Cnpj              = dto.Cnpj.Trim();
            filial.InscricaoEstadual = dto.InscricaoEstadual?.Trim().ToUpper();
            filial.Cep               = dto.Cep.Trim();
            filial.Rua               = Mai(dto.Rua);
            filial.Numero            = dto.Numero.Trim().ToUpper();
            filial.Bairro            = Mai(dto.Bairro);
            filial.Cidade            = Mai(dto.Cidade);
            filial.Uf                = dto.Uf.Trim().ToUpper();
            filial.Telefone          = dto.Telefone.Trim();
            filial.Email             = dto.Email.Trim().ToLower();
            filial.AliquotaIcms      = dto.AliquotaIcms;
            filial.Ativo             = dto.Ativo;

            await _db.SaveChangesAsync();

            var novo = FilialParaDict(filial);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id,
                    anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException)
        {
            Log.Error(ex, "Erro em FilialService.AtualizarAsync | Id: {Id}", id);
            throw;
        }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var filial = await _db.Filiais.FindAsync(id)
                ?? throw new KeyNotFoundException($"Filial {id} não encontrada.");

            var dados = FilialParaDict(filial);

            // Tenta exclusão física
            _db.Filiais.Remove(filial);
            try
            {
                await _db.SaveChangesAsync();

                // Registra log com todos os dados do registro excluído
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);

                return "excluido";
            }
            catch (DbUpdateException)
            {
                // Erro de integridade referencial — registro em uso, apenas desativa
                _db.ChangeTracker.Clear();

                var filialRecarregada = await _db.Filiais.FindAsync(id);
                filialRecarregada!.Ativo = false;
                await _db.SaveChangesAsync();

                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);

                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            Log.Error(ex, "Erro em FilialService.ExcluirAsync | Id: {Id}", id);
            throw;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// Converte para CAIXA ALTA padronizada
    private static string Mai(string? s) => (s ?? string.Empty).Trim().ToUpper();

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b)
        => a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);

    private void ValidarCnpj(string cnpj, long? idExcluir = null)
    {
        var apenasDigitos = new string(cnpj.Where(char.IsDigit).ToArray());

        if (apenasDigitos.Length != 14)
            throw new ArgumentException("CNPJ inválido. Informe 14 dígitos.");

        if (apenasDigitos.Distinct().Count() == 1)
            throw new ArgumentException("CNPJ inválido.");

        int[] mult1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] mult2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var soma = 0;
        for (int i = 0; i < 12; i++) soma += (apenasDigitos[i] - '0') * mult1[i];
        var resto = soma % 11;
        var d1 = resto < 2 ? 0 : 11 - resto;

        soma = 0;
        for (int i = 0; i < 13; i++) soma += (apenasDigitos[i] - '0') * mult2[i];
        resto = soma % 11;
        var d2 = resto < 2 ? 0 : 11 - resto;

        if (apenasDigitos[12] - '0' != d1 || apenasDigitos[13] - '0' != d2)
            throw new ArgumentException("CNPJ inválido.");

        var query = _db.Filiais.Where(f => f.Cnpj == cnpj.Trim());
        if (idExcluir.HasValue) query = query.Where(f => f.Id != idExcluir.Value);
        if (query.Any())
            throw new ArgumentException("CNPJ já cadastrado.");
    }

    private static Dictionary<string, string?> FilialParaDict(Filial f) => new()
    {
        ["Apelido"]            = f.NomeFilial,
        ["Razão Social"]       = f.RazaoSocial,
        ["Nome Fantasia"]      = f.NomeFantasia,
        ["CNPJ"]               = f.Cnpj,
        ["Insc. Estadual"]     = f.InscricaoEstadual,
        ["CEP"]                = f.Cep,
        ["Rua"]                = f.Rua,
        ["Número"]             = f.Numero,
        ["Bairro"]             = f.Bairro,
        ["Cidade"]             = f.Cidade,
        ["UF"]                 = f.Uf,
        ["Telefone"]           = f.Telefone,
        ["E-mail"]             = f.Email,
        ["ICMS %"]             = f.AliquotaIcms.ToString("N2"),
        ["Ativo"]              = f.Ativo ? "Sim" : "Não"
    };

    private static FilialListDto MapToListDto(Filial f) => new()
    {
        Id               = f.Id,
        NomeFilial       = f.NomeFilial,
        RazaoSocial      = f.RazaoSocial,
        NomeFantasia     = f.NomeFantasia,
        Cnpj             = f.Cnpj,
        InscricaoEstadual = f.InscricaoEstadual,
        Cep              = f.Cep,
        Rua              = f.Rua,
        Numero           = f.Numero,
        Bairro           = f.Bairro,
        Cidade           = f.Cidade,
        Uf               = f.Uf,
        Telefone         = f.Telefone,
        Email            = f.Email,
        AliquotaIcms     = f.AliquotaIcms,
        CriadoEm         = f.CriadoEm,
        Ativo            = f.Ativo
    };
}
