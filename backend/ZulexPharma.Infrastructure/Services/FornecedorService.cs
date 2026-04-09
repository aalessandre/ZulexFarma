using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Fornecedores;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class FornecedorService : IFornecedorService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;

    private const string TELA     = "Fornecedores";
    private const string ENTIDADE = "Fornecedor";

    public FornecedorService(AppDbContext db, ILogAcaoService log)
    {
        _db  = db;
        _log = log;
    }

    // ── Listar ───────────────────────────────────────────────────────
    public async Task<List<FornecedorListDto>> ListarAsync()
    {
        try
        {
            return await _db.Fornecedores
                .Include(f => f.Pessoa).ThenInclude(p => p.Contatos)
                .Include(f => f.Pessoa).ThenInclude(p => p.Enderecos)
                .OrderBy(f => f.Pessoa.Nome)
                .Select(f => new FornecedorListDto
                {
                    Id                = f.Id,
                    Tipo              = f.Pessoa.Tipo,
                    Nome              = f.Pessoa.Nome,
                    RazaoSocial       = f.Pessoa.RazaoSocial,
                    CpfCnpj           = f.Pessoa.CpfCnpj,
                    InscricaoEstadual = f.Pessoa.InscricaoEstadual,
                    Email             = f.Pessoa.Contatos
                                          .Where(ct => ct.Tipo == "EMAIL" && ct.Principal)
                                          .Select(ct => ct.Valor).FirstOrDefault()
                                       ?? f.Pessoa.Contatos
                                          .Where(ct => ct.Tipo == "EMAIL")
                                          .Select(ct => ct.Valor).FirstOrDefault(),
                    Telefone          = f.Pessoa.Contatos
                                          .Where(ct => (ct.Tipo == "CELULAR" || ct.Tipo == "TELEFONE") && ct.Principal)
                                          .Select(ct => ct.Valor).FirstOrDefault()
                                       ?? f.Pessoa.Contatos
                                          .Where(ct => ct.Tipo == "CELULAR" || ct.Tipo == "TELEFONE")
                                          .Select(ct => ct.Valor).FirstOrDefault(),
                    Cidade            = f.Pessoa.Enderecos
                                          .Where(e => e.Principal).Select(e => e.Cidade).FirstOrDefault()
                                       ?? f.Pessoa.Enderecos.Select(e => e.Cidade).FirstOrDefault(),
                    Uf                = f.Pessoa.Enderecos
                                          .Where(e => e.Principal).Select(e => e.Uf).FirstOrDefault()
                                       ?? f.Pessoa.Enderecos.Select(e => e.Uf).FirstOrDefault(),
                    CriadoEm          = f.CriadoEm,
                    Ativo             = f.Ativo
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FornecedorService.ListarAsync");
            throw;
        }
    }

    // ── Obter detalhe ────────────────────────────────────────────────
    public async Task<FornecedorDetalheDto> ObterAsync(long id)
    {
        var f = await _db.Fornecedores
            .Include(x => x.Pessoa).ThenInclude(p => p.Contatos)
            .Include(x => x.Pessoa).ThenInclude(p => p.Enderecos)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Fornecedor {id} não encontrado.");

        return new FornecedorDetalheDto
        {
            Id                = f.Id,
            Tipo              = f.Pessoa.Tipo,
            Nome              = f.Pessoa.Nome,
            RazaoSocial       = f.Pessoa.RazaoSocial,
            CpfCnpj           = f.Pessoa.CpfCnpj,
            InscricaoEstadual = f.Pessoa.InscricaoEstadual,
            Rg                = f.Pessoa.Rg,
            DataNascimento    = f.Pessoa.DataNascimento,
            Observacao        = f.Pessoa.Observacao,
            Ativo             = f.Ativo,
            CriadoEm          = f.CriadoEm,
            Enderecos = f.Pessoa.Enderecos.Select(e => new EnderecoFormDto
            {
                Id          = e.Id,
                Tipo        = e.Tipo,
                Cep         = e.Cep,
                Rua         = e.Rua,
                Numero      = e.Numero,
                Complemento = e.Complemento,
                Bairro      = e.Bairro,
                Cidade      = e.Cidade,
                Uf          = e.Uf,
                Principal   = e.Principal
            }).ToList(),
            Contatos = f.Pessoa.Contatos.Select(ct => new ContatoFormDto
            {
                Id        = ct.Id,
                Tipo      = ct.Tipo,
                Valor     = ct.Valor,
                Descricao = ct.Descricao,
                Principal = ct.Principal
            }).ToList()
        };
    }

    // ── Criar ────────────────────────────────────────────────────────
    public async Task<FornecedorListDto> CriarAsync(FornecedorFormDto dto)
    {
        try
        {
            var tipoUpper = dto.Tipo?.Trim().ToUpper();
            if (tipoUpper != "F" && tipoUpper != "J")
                throw new ArgumentException("Tipo deve ser 'F' (Pessoa Física) ou 'J' (Pessoa Jurídica).");

            var tipo = tipoUpper;

            if (tipo == "F")
                ValidarFormatoCpf(dto.CpfCnpj);
            else
                ValidarFormatoCnpj(dto.CpfCnpj);

            // Verificar se Pessoa já existe com este CPF/CNPJ
            var pessoaExistente = await _db.Pessoas
                .Include(p => p.Fornecedor)
                .Include(p => p.Contatos)
                .Include(p => p.Enderecos)
                .FirstOrDefaultAsync(p => p.CpfCnpj == CpfCnpjHelper.SomenteDigitos(dto.CpfCnpj));

            Pessoa pessoa;

            if (pessoaExistente != null)
            {
                if (pessoaExistente.Fornecedor != null)
                    throw new ArgumentException("Este CPF/CNPJ já possui um fornecedor cadastrado.");

                // Reutilizar Pessoa existente
                pessoa = pessoaExistente;
                pessoa.Tipo              = tipo;
                pessoa.Nome              = Mai(dto.Nome);
                pessoa.RazaoSocial       = tipo == "J" ? Mai(dto.RazaoSocial) : null;
                pessoa.InscricaoEstadual = dto.InscricaoEstadual?.Trim().ToUpper();
                pessoa.Rg                = tipo == "F" ? dto.Rg?.Trim().ToUpper() : null;
                pessoa.DataNascimento    = tipo == "F" ? ToUtc(dto.DataNascimento) : null;
                pessoa.Observacao        = dto.Observacao?.Trim();
            }
            else
            {
                pessoa = new Pessoa
                {
                    Tipo              = tipo,
                    Nome              = Mai(dto.Nome),
                    RazaoSocial       = tipo == "J" ? Mai(dto.RazaoSocial) : null,
                    CpfCnpj           = CpfCnpjHelper.SomenteDigitos(dto.CpfCnpj),
                    InscricaoEstadual = dto.InscricaoEstadual?.Trim().ToUpper(),
                    Rg                = tipo == "F" ? dto.Rg?.Trim().ToUpper() : null,
                    DataNascimento    = tipo == "F" ? ToUtc(dto.DataNascimento) : null,
                    Observacao        = dto.Observacao?.Trim()
                };
                _db.Pessoas.Add(pessoa);
            }

            await _db.SaveChangesAsync();

            // Endereços (só adicionar se Pessoa é nova ou não tem endereços)
            if (pessoaExistente == null || pessoaExistente.Enderecos.Count == 0)
            {
                foreach (var e in dto.Enderecos)
                {
                    _db.PessoasEndereco.Add(new PessoaEndereco
                    {
                        PessoaId    = pessoa.Id,
                        Tipo        = e.Tipo.Trim().ToUpper(),
                        Cep         = e.Cep.Trim(),
                        Rua         = Mai(e.Rua),
                        Numero      = e.Numero.Trim().ToUpper(),
                        Complemento = e.Complemento?.Trim().ToUpper(),
                        Bairro      = Mai(e.Bairro),
                        Cidade      = Mai(e.Cidade),
                        Uf          = e.Uf.Trim().ToUpper(),
                        Principal   = e.Principal
                    });
                }
            }

            // Contatos (só adicionar se Pessoa é nova ou não tem contatos)
            if (pessoaExistente == null || pessoaExistente.Contatos.Count == 0)
            {
                foreach (var ct in dto.Contatos)
                {
                    _db.PessoasContato.Add(new PessoaContato
                    {
                        PessoaId  = pessoa.Id,
                        Tipo      = ct.Tipo.Trim().ToUpper(),
                        Valor     = ct.Valor.Trim(),
                        Descricao = ct.Descricao?.Trim().ToUpper(),
                        Principal = ct.Principal
                    });
                }
            }

            var fornecedor = new Fornecedor
            {
                PessoaId = pessoa.Id,
                Ativo    = dto.Ativo
            };

            _db.Fornecedores.Add(fornecedor);
            await _db.SaveChangesAsync();

            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, fornecedor.Id,
                novo: FornecedorParaDict(fornecedor, pessoa));

            return await ListarPorIdAsync(fornecedor.Id);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            Log.Error(ex, "Erro em FornecedorService.CriarAsync");
            throw;
        }
    }

    // ── Atualizar ────────────────────────────────────────────────────
    public async Task AtualizarAsync(long id, FornecedorFormDto dto)
    {
        try
        {
            var fornecedor = await _db.Fornecedores
                .Include(f => f.Pessoa).ThenInclude(p => p.Contatos)
                .Include(f => f.Pessoa).ThenInclude(p => p.Enderecos)
                .FirstOrDefaultAsync(f => f.Id == id)
                ?? throw new KeyNotFoundException($"Fornecedor {id} não encontrado.");

            var tipoUpper = dto.Tipo?.Trim().ToUpper();
            if (tipoUpper != "F" && tipoUpper != "J")
                throw new ArgumentException("Tipo deve ser 'F' (Pessoa Física) ou 'J' (Pessoa Jurídica).");

            var tipo = tipoUpper;

            if (tipo == "F")
                ValidarFormatoCpf(dto.CpfCnpj);
            else
                ValidarFormatoCnpj(dto.CpfCnpj);
            ValidarCpfCnpjUnicidade(CpfCnpjHelper.SomenteDigitos(dto.CpfCnpj), fornecedor.Pessoa.Id);

            var anterior = FornecedorParaDict(fornecedor, fornecedor.Pessoa);

            // Atualizar Pessoa
            var pessoa = fornecedor.Pessoa;
            pessoa.Tipo              = tipo;
            pessoa.Nome              = Mai(dto.Nome);
            pessoa.RazaoSocial       = tipo == "J" ? Mai(dto.RazaoSocial) : null;
            pessoa.CpfCnpj           = CpfCnpjHelper.SomenteDigitos(dto.CpfCnpj);
            pessoa.InscricaoEstadual = dto.InscricaoEstadual?.Trim().ToUpper();
            pessoa.Rg                = tipo == "F" ? dto.Rg?.Trim().ToUpper() : null;
            pessoa.DataNascimento    = tipo == "F" ? ToUtc(dto.DataNascimento) : null;
            pessoa.Observacao        = dto.Observacao?.Trim();

            // Atualizar Fornecedor
            fornecedor.Ativo = dto.Ativo;

            // Sincronizar endereços
            SincronizarEnderecos(pessoa, dto.Enderecos);

            // Sincronizar contatos
            SincronizarContatos(pessoa, dto.Contatos);

            await _db.SaveChangesAsync();

            var novo = FornecedorParaDict(fornecedor, pessoa);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id,
                    anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException)
        {
            Log.Error(ex, "Erro em FornecedorService.AtualizarAsync | Id: {Id}", id);
            throw;
        }
    }

    // ── Excluir ──────────────────────────────────────────────────────
    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var fornecedor = await _db.Fornecedores
                .Include(f => f.Pessoa).ThenInclude(p => p.Contatos)
                .Include(f => f.Pessoa).ThenInclude(p => p.Enderecos)
                .Include(f => f.Pessoa).ThenInclude(p => p.Colaborador)
                .FirstOrDefaultAsync(f => f.Id == id)
                ?? throw new KeyNotFoundException($"Fornecedor {id} não encontrado.");

            var dados = FornecedorParaDict(fornecedor, fornecedor.Pessoa);
            var pessoa = fornecedor.Pessoa;
            var pessoaCompartilhada = pessoa.Colaborador != null;

            _db.Fornecedores.Remove(fornecedor);

            // Só remover Pessoa se não tem Colaborador vinculado
            if (!pessoaCompartilhada)
                _db.Pessoas.Remove(pessoa);

            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();

                var recarregado = await _db.Fornecedores
                    .Include(f => f.Pessoa)
                    .FirstAsync(f => f.Id == id);
                recarregado.Ativo = false;
                if (!pessoaCompartilhada) recarregado.Pessoa.Ativo = false;
                await _db.SaveChangesAsync();

                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            Log.Error(ex, "Erro em FornecedorService.ExcluirAsync | Id: {Id}", id);
            throw;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private async Task<FornecedorListDto> ListarPorIdAsync(long id)
    {
        return await _db.Fornecedores
            .Include(f => f.Pessoa).ThenInclude(p => p.Contatos)
            .Include(f => f.Pessoa).ThenInclude(p => p.Enderecos)
            .Where(f => f.Id == id)
            .Select(f => new FornecedorListDto
            {
                Id                = f.Id,
                Tipo              = f.Pessoa.Tipo,
                Nome              = f.Pessoa.Nome,
                RazaoSocial       = f.Pessoa.RazaoSocial,
                CpfCnpj           = f.Pessoa.CpfCnpj,
                InscricaoEstadual = f.Pessoa.InscricaoEstadual,
                Email             = f.Pessoa.Contatos
                                      .Where(ct => ct.Tipo == "EMAIL" && ct.Principal)
                                      .Select(ct => ct.Valor).FirstOrDefault()
                                   ?? f.Pessoa.Contatos
                                      .Where(ct => ct.Tipo == "EMAIL")
                                      .Select(ct => ct.Valor).FirstOrDefault(),
                Telefone          = f.Pessoa.Contatos
                                      .Where(ct => (ct.Tipo == "CELULAR" || ct.Tipo == "TELEFONE") && ct.Principal)
                                      .Select(ct => ct.Valor).FirstOrDefault()
                                   ?? f.Pessoa.Contatos
                                      .Where(ct => ct.Tipo == "CELULAR" || ct.Tipo == "TELEFONE")
                                      .Select(ct => ct.Valor).FirstOrDefault(),
                Cidade            = f.Pessoa.Enderecos
                                      .Where(e => e.Principal).Select(e => e.Cidade).FirstOrDefault()
                                   ?? f.Pessoa.Enderecos.Select(e => e.Cidade).FirstOrDefault(),
                Uf                = f.Pessoa.Enderecos
                                      .Where(e => e.Principal).Select(e => e.Uf).FirstOrDefault()
                                   ?? f.Pessoa.Enderecos.Select(e => e.Uf).FirstOrDefault(),
                CriadoEm          = f.CriadoEm,
                Ativo             = f.Ativo
            })
            .FirstAsync();
    }

    private void SincronizarEnderecos(Pessoa pessoa, List<EnderecoFormDto> dtos)
    {
        var idsDto = dtos.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToHashSet();

        // Remover os que não vieram no DTO
        var paraRemover = pessoa.Enderecos.Where(e => !idsDto.Contains(e.Id)).ToList();
        foreach (var e in paraRemover)
            _db.PessoasEndereco.Remove(e);

        foreach (var dto in dtos)
        {
            if (dto.Id.HasValue)
            {
                // Atualizar existente
                var existente = pessoa.Enderecos.FirstOrDefault(e => e.Id == dto.Id.Value);
                if (existente != null)
                {
                    existente.Tipo        = dto.Tipo.Trim().ToUpper();
                    existente.Cep         = dto.Cep.Trim();
                    existente.Rua         = Mai(dto.Rua);
                    existente.Numero      = dto.Numero.Trim().ToUpper();
                    existente.Complemento = dto.Complemento?.Trim().ToUpper();
                    existente.Bairro      = Mai(dto.Bairro);
                    existente.Cidade      = Mai(dto.Cidade);
                    existente.Uf          = dto.Uf.Trim().ToUpper();
                    existente.Principal   = dto.Principal;
                }
            }
            else
            {
                // Novo endereço
                _db.PessoasEndereco.Add(new PessoaEndereco
                {
                    PessoaId    = pessoa.Id,
                    Tipo        = dto.Tipo.Trim().ToUpper(),
                    Cep         = dto.Cep.Trim(),
                    Rua         = Mai(dto.Rua),
                    Numero      = dto.Numero.Trim().ToUpper(),
                    Complemento = dto.Complemento?.Trim().ToUpper(),
                    Bairro      = Mai(dto.Bairro),
                    Cidade      = Mai(dto.Cidade),
                    Uf          = dto.Uf.Trim().ToUpper(),
                    Principal   = dto.Principal
                });
            }
        }
    }

    private void SincronizarContatos(Pessoa pessoa, List<ContatoFormDto> dtos)
    {
        var idsDto = dtos.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToHashSet();

        var paraRemover = pessoa.Contatos.Where(c => !idsDto.Contains(c.Id)).ToList();
        foreach (var c in paraRemover)
            _db.PessoasContato.Remove(c);

        foreach (var dto in dtos)
        {
            if (dto.Id.HasValue)
            {
                var existente = pessoa.Contatos.FirstOrDefault(c => c.Id == dto.Id.Value);
                if (existente != null)
                {
                    existente.Tipo      = dto.Tipo.Trim().ToUpper();
                    existente.Valor     = dto.Valor.Trim();
                    existente.Descricao = dto.Descricao?.Trim().ToUpper();
                    existente.Principal = dto.Principal;
                }
            }
            else
            {
                _db.PessoasContato.Add(new PessoaContato
                {
                    PessoaId  = pessoa.Id,
                    Tipo      = dto.Tipo.Trim().ToUpper(),
                    Valor     = dto.Valor.Trim(),
                    Descricao = dto.Descricao?.Trim().ToUpper(),
                    Principal = dto.Principal
                });
            }
        }
    }

    private static string Mai(string? s) => (s ?? string.Empty).Trim().ToUpper();

    private static DateTime? ToUtc(DateTime? dt)
        => dt.HasValue ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc) : null;

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b)
        => a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);

    private static void ValidarFormatoCpf(string cpf)
    {
        var d = new string(cpf.Where(char.IsDigit).ToArray());

        if (d.Length != 11)
            throw new ArgumentException("CPF inválido. Informe 11 dígitos.");

        if (d.Distinct().Count() == 1)
            throw new ArgumentException("CPF inválido.");

        var soma = 0;
        for (int i = 0; i < 9; i++) soma += (d[i] - '0') * (10 - i);
        var resto = soma % 11;
        var d1 = resto < 2 ? 0 : 11 - resto;

        soma = 0;
        for (int i = 0; i < 10; i++) soma += (d[i] - '0') * (11 - i);
        resto = soma % 11;
        var d2 = resto < 2 ? 0 : 11 - resto;

        if (d[9] - '0' != d1 || d[10] - '0' != d2)
            throw new ArgumentException("CPF inválido.");
    }

    private static void ValidarFormatoCnpj(string cnpj)
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
    }

    private void ValidarCpfCnpjUnicidade(string cpfCnpj, long pessoaIdExcluir)
    {
        if (_db.Pessoas.Any(p => p.CpfCnpj == CpfCnpjHelper.SomenteDigitos(cpfCnpj) && p.Id != pessoaIdExcluir))
            throw new ArgumentException("CPF/CNPJ já cadastrado para outra pessoa.");
    }

    private static Dictionary<string, string?> FornecedorParaDict(Fornecedor f, Pessoa p) => new()
    {
        ["Tipo"]              = p.Tipo == "F" ? "Pessoa Física" : "Pessoa Jurídica",
        ["Nome"]              = p.Nome,
        ["Razão Social"]      = p.RazaoSocial,
        ["CPF/CNPJ"]          = p.CpfCnpj,
        ["Insc. Estadual"]    = p.InscricaoEstadual,
        ["RG"]                = p.Rg,
        ["Observação"]        = p.Observacao,
        ["Ativo"]             = f.Ativo ? "Sim" : "Não"
    };
}
