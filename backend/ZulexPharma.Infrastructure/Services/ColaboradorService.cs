using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Colaboradores;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class ColaboradorService : IColaboradorService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;

    private const string TELA     = "Colaboradores";
    private const string ENTIDADE = "Colaborador";

    public ColaboradorService(AppDbContext db, ILogAcaoService log)
    {
        _db  = db;
        _log = log;
    }

    // ── Listar ───────────────────────────────────────────────────────
    public async Task<List<ColaboradorListDto>> ListarAsync()
    {
        try
        {
            return await _db.Colaboradores
                .Include(c => c.Pessoa).ThenInclude(p => p.Contatos)
                .Include(c => c.Pessoa).ThenInclude(p => p.Enderecos)
                .OrderBy(c => c.Pessoa.Nome)
                .Select(c => new ColaboradorListDto
                {
                    Id             = c.Id,
                    Nome           = c.Pessoa.Nome,
                    Cpf            = c.Pessoa.CpfCnpj,
                    Rg             = c.Pessoa.Rg,
                    DataNascimento = c.Pessoa.DataNascimento,
                    Cargo          = c.Cargo,
                    Salario        = c.Salario,
                    Email          = c.Pessoa.Contatos
                                      .Where(ct => ct.Tipo == "EMAIL" && ct.Principal)
                                      .Select(ct => ct.Valor).FirstOrDefault()
                                   ?? c.Pessoa.Contatos
                                      .Where(ct => ct.Tipo == "EMAIL")
                                      .Select(ct => ct.Valor).FirstOrDefault(),
                    Telefone       = c.Pessoa.Contatos
                                      .Where(ct => (ct.Tipo == "CELULAR" || ct.Tipo == "TELEFONE") && ct.Principal)
                                      .Select(ct => ct.Valor).FirstOrDefault()
                                   ?? c.Pessoa.Contatos
                                      .Where(ct => ct.Tipo == "CELULAR" || ct.Tipo == "TELEFONE")
                                      .Select(ct => ct.Valor).FirstOrDefault(),
                    Cidade         = c.Pessoa.Enderecos
                                      .Where(e => e.Principal).Select(e => e.Cidade).FirstOrDefault()
                                   ?? c.Pessoa.Enderecos.Select(e => e.Cidade).FirstOrDefault(),
                    Uf             = c.Pessoa.Enderecos
                                      .Where(e => e.Principal).Select(e => e.Uf).FirstOrDefault()
                                   ?? c.Pessoa.Enderecos.Select(e => e.Uf).FirstOrDefault(),
                    CriadoEm       = c.CriadoEm,
                    Ativo          = c.Ativo
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em ColaboradorService.ListarAsync");
            throw;
        }
    }

    // ── Obter detalhe ────────────────────────────────────────────────
    public async Task<ColaboradorDetalheDto> ObterAsync(long id)
    {
        var c = await _db.Colaboradores
            .Include(x => x.Pessoa).ThenInclude(p => p.Contatos)
            .Include(x => x.Pessoa).ThenInclude(p => p.Enderecos)
            .Include(x => x.Usuario).ThenInclude(u => u!.Filial)
            .Include(x => x.Usuario).ThenInclude(u => u!.GrupoUsuario)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Colaborador {id} não encontrado.");

        var dto = new ColaboradorDetalheDto
        {
            Id             = c.Id,
            Nome           = c.Pessoa.Nome,
            Cpf            = c.Pessoa.CpfCnpj,
            Rg             = c.Pessoa.Rg,
            DataNascimento = c.Pessoa.DataNascimento,
            Cargo          = c.Cargo,
            DataAdmissao   = c.DataAdmissao,
            Salario        = c.Salario,
            Observacao     = c.Observacao,
            Ativo          = c.Ativo,
            CriadoEm       = c.CriadoEm,
            Enderecos = c.Pessoa.Enderecos.Select(e => new EnderecoFormDto
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
            Contatos = c.Pessoa.Contatos.Select(ct => new ContatoFormDto
            {
                Id        = ct.Id,
                Tipo      = ct.Tipo,
                Valor     = ct.Valor,
                Descricao = ct.Descricao,
                Principal = ct.Principal
            }).ToList()
        };

        if (c.Usuario != null)
        {
            var filialGrupos = await _db.UsuarioFilialGrupos
                .Include(ufg => ufg.Filial)
                .Include(ufg => ufg.GrupoUsuario)
                .Where(ufg => ufg.UsuarioId == c.Usuario.Id)
                .ToListAsync();

            dto.Acesso = new AcessoDetalheDto
            {
                UsuarioId       = c.Usuario.Id,
                Login           = c.Usuario.Login,
                IsAdministrador = c.Usuario.IsAdministrador,
                SessaoMaximaMinutos = c.Usuario.SessaoMaximaMinutos,
                InatividadeMinutos  = c.Usuario.InatividadeMinutos,
                FilialPadraoId  = c.Usuario.FilialId,
                NomeFilialPadrao = c.Usuario.Filial?.NomeFantasia ?? string.Empty,
                FilialGrupos    = filialGrupos.Select(fg => new FilialGrupoDetalheDto
                {
                    FilialId       = fg.FilialId,
                    NomeFilial     = fg.Filial.NomeFantasia,
                    GrupoUsuarioId = fg.GrupoUsuarioId,
                    NomeGrupo      = fg.GrupoUsuario.Nome
                }).ToList()
            };
        }

        return dto;
    }

    // ── Criar ────────────────────────────────────────────────────────
    public async Task<ColaboradorListDto> CriarAsync(ColaboradorFormDto dto)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            ValidarFormatoCpf(dto.Cpf);

            // Verificar se Pessoa já existe com este CPF
            var pessoaExistente = await _db.Pessoas
                .Include(p => p.Colaborador)
                .Include(p => p.Contatos)
                .Include(p => p.Enderecos)
                .FirstOrDefaultAsync(p => p.CpfCnpj == dto.Cpf.Trim());

            Pessoa pessoa;

            if (pessoaExistente != null)
            {
                // Pessoa existe — verificar se já tem Colaborador
                if (pessoaExistente.Colaborador != null)
                    throw new ArgumentException("Este CPF já possui um colaborador cadastrado.");

                // Reutilizar Pessoa existente (pode ser Fornecedor ou sem vínculo)
                pessoa = pessoaExistente;
                pessoa.Nome           = Mai(dto.Nome);
                pessoa.Rg             = dto.Rg?.Trim().ToUpper();
                pessoa.DataNascimento = ToUtc(dto.DataNascimento);
                pessoa.Observacao     = dto.Observacao?.Trim();
            }
            else
            {
                // Criar nova Pessoa
                pessoa = new Pessoa
                {
                    Tipo           = "F",
                    Nome           = Mai(dto.Nome),
                    CpfCnpj        = dto.Cpf.Trim(),
                    Rg             = dto.Rg?.Trim().ToUpper(),
                    DataNascimento = ToUtc(dto.DataNascimento),
                    Observacao     = dto.Observacao?.Trim()
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

            var colaborador = new Colaborador
            {
                PessoaId    = pessoa.Id,
                Cargo       = dto.Cargo?.Trim().ToUpper(),
                DataAdmissao = ToUtc(dto.DataAdmissao),
                Salario     = dto.Salario,
                Observacao  = dto.Observacao?.Trim(),
                Ativo       = dto.Ativo
            };

            _db.Colaboradores.Add(colaborador);
            await _db.SaveChangesAsync();

            // Acesso ao sistema
            if (dto.Acesso != null && !string.IsNullOrWhiteSpace(dto.Acesso.Login))
                await CriarOuAtualizarUsuario(colaborador, dto.Acesso, pessoa);

            await transaction.CommitAsync();

            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, colaborador.Id,
                novo: ColaboradorParaDict(colaborador, pessoa));

            return await ListarPorIdAsync(colaborador.Id);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            await transaction.RollbackAsync();
            Log.Error(ex, "Erro em ColaboradorService.CriarAsync");
            throw;
        }
    }

    // ── Atualizar ────────────────────────────────────────────────────
    public async Task AtualizarAsync(long id, ColaboradorFormDto dto)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var colaborador = await _db.Colaboradores
                .Include(c => c.Pessoa).ThenInclude(p => p.Contatos)
                .Include(c => c.Pessoa).ThenInclude(p => p.Enderecos)
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new KeyNotFoundException($"Colaborador {id} não encontrado.");

            ValidarFormatoCpf(dto.Cpf);
            ValidarCpfUnicidade(dto.Cpf, colaborador.Pessoa.Id);

            var anterior = ColaboradorParaDict(colaborador, colaborador.Pessoa);

            // Atualizar Pessoa
            var pessoa = colaborador.Pessoa;
            pessoa.Nome           = Mai(dto.Nome);
            pessoa.CpfCnpj        = dto.Cpf.Trim();
            pessoa.Rg             = dto.Rg?.Trim().ToUpper();
            pessoa.DataNascimento = ToUtc(dto.DataNascimento);
            pessoa.Observacao     = dto.Observacao?.Trim();

            // Atualizar Colaborador
            colaborador.Cargo       = dto.Cargo?.Trim().ToUpper();
            colaborador.DataAdmissao = ToUtc(dto.DataAdmissao);
            colaborador.Salario     = dto.Salario;
            colaborador.Observacao  = dto.Observacao?.Trim();
            colaborador.Ativo       = dto.Ativo;

            // Sincronizar endereços
            SincronizarEnderecos(pessoa, dto.Enderecos);

            // Sincronizar contatos
            SincronizarContatos(pessoa, dto.Contatos);

            await _db.SaveChangesAsync();

            // Acesso ao sistema
            if (dto.Acesso != null && !string.IsNullOrWhiteSpace(dto.Acesso.Login))
                await CriarOuAtualizarUsuario(colaborador, dto.Acesso, pessoa);
            else if (dto.Acesso == null || string.IsNullOrWhiteSpace(dto.Acesso.Login))
                await RemoverUsuario(colaborador);

            await transaction.CommitAsync();

            var novo = ColaboradorParaDict(colaborador, pessoa);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id,
                    anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException)
        {
            await transaction.RollbackAsync();
            Log.Error(ex, "Erro em ColaboradorService.AtualizarAsync | Id: {Id}", id);
            throw;
        }
    }

    // ── Excluir ──────────────────────────────────────────────────────
    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var colaborador = await _db.Colaboradores
                .Include(c => c.Pessoa).ThenInclude(p => p.Contatos)
                .Include(c => c.Pessoa).ThenInclude(p => p.Enderecos)
                .Include(c => c.Pessoa).ThenInclude(p => p.Fornecedor)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new KeyNotFoundException($"Colaborador {id} não encontrado.");

            var dados = ColaboradorParaDict(colaborador, colaborador.Pessoa);
            var pessoa = colaborador.Pessoa;
            var pessoaCompartilhada = pessoa.Fornecedor != null;

            _db.Colaboradores.Remove(colaborador);

            // Só remover Pessoa se não tem Fornecedor vinculado
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

                var recarregado = await _db.Colaboradores
                    .Include(c => c.Pessoa)
                    .FirstAsync(c => c.Id == id);
                recarregado.Ativo = false;
                if (!pessoaCompartilhada) recarregado.Pessoa.Ativo = false;
                await _db.SaveChangesAsync();

                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            Log.Error(ex, "Erro em ColaboradorService.ExcluirAsync | Id: {Id}", id);
            throw;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private async Task<ColaboradorListDto> ListarPorIdAsync(long id)
    {
        return await _db.Colaboradores
            .Include(c => c.Pessoa).ThenInclude(p => p.Contatos)
            .Include(c => c.Pessoa).ThenInclude(p => p.Enderecos)
            .Where(c => c.Id == id)
            .Select(c => new ColaboradorListDto
            {
                Id             = c.Id,
                Nome           = c.Pessoa.Nome,
                Cpf            = c.Pessoa.CpfCnpj,
                Rg             = c.Pessoa.Rg,
                DataNascimento = c.Pessoa.DataNascimento,
                Cargo          = c.Cargo,
                Salario        = c.Salario,
                Email          = c.Pessoa.Contatos
                                  .Where(ct => ct.Tipo == "EMAIL" && ct.Principal)
                                  .Select(ct => ct.Valor).FirstOrDefault()
                               ?? c.Pessoa.Contatos
                                  .Where(ct => ct.Tipo == "EMAIL")
                                  .Select(ct => ct.Valor).FirstOrDefault(),
                Telefone       = c.Pessoa.Contatos
                                  .Where(ct => (ct.Tipo == "CELULAR" || ct.Tipo == "TELEFONE") && ct.Principal)
                                  .Select(ct => ct.Valor).FirstOrDefault()
                               ?? c.Pessoa.Contatos
                                  .Where(ct => ct.Tipo == "CELULAR" || ct.Tipo == "TELEFONE")
                                  .Select(ct => ct.Valor).FirstOrDefault(),
                Cidade         = c.Pessoa.Enderecos
                                  .Where(e => e.Principal).Select(e => e.Cidade).FirstOrDefault()
                               ?? c.Pessoa.Enderecos.Select(e => e.Cidade).FirstOrDefault(),
                Uf             = c.Pessoa.Enderecos
                                  .Where(e => e.Principal).Select(e => e.Uf).FirstOrDefault()
                               ?? c.Pessoa.Enderecos.Select(e => e.Uf).FirstOrDefault(),
                CriadoEm       = c.CriadoEm,
                Ativo          = c.Ativo
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

    private async Task CriarOuAtualizarUsuario(Colaborador colaborador, AcessoFormDto acesso, Pessoa pessoa)
    {
        // Validate login length
        if (acesso.Login.Trim().Length < 6 || acesso.Login.Trim().Length > 24)
            throw new ArgumentException("O login deve ter entre 6 e 24 caracteres.");

        // Validate password length (only when creating new or changing password)
        if (!string.IsNullOrWhiteSpace(acesso.Senha))
        {
            if (acesso.Senha.Length < 4 || acesso.Senha.Length > 12)
                throw new ArgumentException("A senha deve ter entre 4 e 12 caracteres.");
        }

        var usuario = await _db.Usuarios
            .Include(u => u.FilialGrupos)
            .FirstOrDefaultAsync(u => u.ColaboradorId == colaborador.Id);

        // Determinar a filial/grupo principal (primeiro com acesso)
        var primeiroAcesso = acesso.FilialGrupos.FirstOrDefault();

        if (usuario == null)
        {
            if (await _db.Usuarios.AnyAsync(u => u.Login == acesso.Login.Trim()))
                throw new ArgumentException("Login já está em uso por outro usuário.");

            usuario = new Usuario
            {
                Nome            = pessoa.Nome,
                Login           = acesso.Login.Trim(),
                SenhaHash       = BCrypt.Net.BCrypt.HashPassword(acesso.Senha ?? throw new ArgumentException("Senha é obrigatória para novo acesso.")),
                FilialId        = acesso.FilialPadraoId > 0 ? acesso.FilialPadraoId : (primeiroAcesso?.FilialId ?? (await _db.Filiais.Select(f => f.Id).FirstOrDefaultAsync())),
                GrupoUsuarioId  = primeiroAcesso?.GrupoUsuarioId ?? (await _db.UsuariosGrupos.Select(g => g.Id).FirstOrDefaultAsync()),
                IsAdministrador = acesso.IsAdministrador,
                ColaboradorId   = colaborador.Id,
                SessaoMaximaMinutos = acesso.SessaoMaximaMinutos,
                InatividadeMinutos  = acesso.InatividadeMinutos
            };
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();
        }
        else
        {
            if (await _db.Usuarios.AnyAsync(u => u.Login == acesso.Login.Trim() && u.Id != usuario.Id))
                throw new ArgumentException("Login já está em uso por outro usuário.");

            usuario.Nome            = pessoa.Nome;
            usuario.Login           = acesso.Login.Trim();
            usuario.IsAdministrador = acesso.IsAdministrador;
            usuario.SessaoMaximaMinutos = acesso.SessaoMaximaMinutos;
            usuario.InatividadeMinutos  = acesso.InatividadeMinutos;

            if (acesso.FilialPadraoId > 0)
                usuario.FilialId = acesso.FilialPadraoId;
            else if (primeiroAcesso != null)
                usuario.FilialId = primeiroAcesso.FilialId;

            if (primeiroAcesso != null)
                usuario.GrupoUsuarioId = primeiroAcesso.GrupoUsuarioId;

            if (!string.IsNullOrWhiteSpace(acesso.Senha))
                usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(acesso.Senha);

            await _db.SaveChangesAsync();
        }

        // Sincronizar UsuarioFilialGrupo
        var existentes = await _db.UsuarioFilialGrupos
            .Where(ufg => ufg.UsuarioId == usuario.Id)
            .ToListAsync();

        _db.UsuarioFilialGrupos.RemoveRange(existentes);

        foreach (var fg in acesso.FilialGrupos)
        {
            _db.UsuarioFilialGrupos.Add(new UsuarioFilialGrupo
            {
                UsuarioId      = usuario.Id,
                FilialId       = fg.FilialId,
                GrupoUsuarioId = fg.GrupoUsuarioId
            });
        }

        await _db.SaveChangesAsync();
    }

    private async Task RemoverUsuario(Colaborador colaborador)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.FilialGrupos)
            .FirstOrDefaultAsync(u => u.ColaboradorId == colaborador.Id);
        if (usuario != null)
        {
            _db.UsuarioFilialGrupos.RemoveRange(usuario.FilialGrupos);
            usuario.ColaboradorId = null;
            usuario.Ativo = false;
            await _db.SaveChangesAsync();
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

    private void ValidarCpfUnicidade(string cpf, long pessoaIdExcluir)
    {
        if (_db.Pessoas.Any(p => p.CpfCnpj == cpf.Trim() && p.Id != pessoaIdExcluir))
            throw new ArgumentException("CPF já cadastrado para outra pessoa.");
    }

    private static Dictionary<string, string?> ColaboradorParaDict(Colaborador c, Pessoa p) => new()
    {
        ["Nome"]            = p.Nome,
        ["CPF"]             = p.CpfCnpj,
        ["RG"]              = p.Rg,
        ["Data Nascimento"] = p.DataNascimento?.ToString("dd/MM/yyyy"),
        ["Cargo"]           = c.Cargo,
        ["Data Admissão"]   = c.DataAdmissao?.ToString("dd/MM/yyyy"),
        ["Salário"]         = c.Salario?.ToString("N2"),
        ["Observação"]      = c.Observacao,
        ["Ativo"]           = c.Ativo ? "Sim" : "Não"
    };
}
