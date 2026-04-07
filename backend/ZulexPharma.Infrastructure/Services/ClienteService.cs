using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Clientes;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class ClienteService : IClienteService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Clientes";
    private const string ENTIDADE = "Cliente";

    public ClienteService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<ClienteListDto>> ListarAsync()
    {
        try
        {
            return await _db.Set<Cliente>()
                .Include(c => c.Pessoa).ThenInclude(p => p.Contatos)
                .Include(c => c.Pessoa).ThenInclude(p => p.Enderecos)
                .OrderBy(c => c.Pessoa.Nome)
                .Select(c => new ClienteListDto
                {
                    Id = c.Id,
                    Nome = c.Pessoa.Nome,
                    RazaoSocial = c.Pessoa.RazaoSocial,
                    Tipo = c.Pessoa.Tipo,
                    CpfCnpj = c.Pessoa.CpfCnpj,
                    Telefone = c.Pessoa.Contatos.Where(ct => ct.Tipo != "EMAIL").Select(ct => ct.Valor).FirstOrDefault(),
                    Email = c.Pessoa.Contatos.Where(ct => ct.Tipo == "EMAIL").Select(ct => ct.Valor).FirstOrDefault(),
                    Cidade = c.Pessoa.Enderecos.Where(e => e.Principal).Select(e => e.Cidade).FirstOrDefault(),
                    Uf = c.Pessoa.Enderecos.Where(e => e.Principal).Select(e => e.Uf).FirstOrDefault(),
                    Bloqueado = c.Bloqueado,
                    Ativo = c.Ativo,
                    CriadoEm = c.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ClienteService.ListarAsync"); throw; }
    }

    public async Task<ClienteDetalheDto?> ObterAsync(long id)
    {
        try
        {
            var c = await _db.Set<Cliente>()
                .Include(x => x.Pessoa).ThenInclude(p => p.Contatos)
                .Include(x => x.Pessoa).ThenInclude(p => p.Enderecos)
                .Include(x => x.Convenios).ThenInclude(cv => cv.Convenio).ThenInclude(cv => cv.Pessoa)
                .Include(x => x.Autorizacoes)
                .Include(x => x.Descontos)
                .Include(x => x.UsosContinuos).ThenInclude(u => u.Produto)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return null;

            return new ClienteDetalheDto
            {
                Id = c.Id, PessoaId = c.PessoaId,
                Tipo = c.Pessoa.Tipo, Nome = c.Pessoa.Nome, RazaoSocial = c.Pessoa.RazaoSocial,
                CpfCnpj = c.Pessoa.CpfCnpj, InscricaoEstadual = c.Pessoa.InscricaoEstadual,
                Rg = c.Pessoa.Rg, DataNascimento = c.Pessoa.DataNascimento?.ToString("yyyy-MM-dd"),
                LimiteCredito = c.LimiteCredito, DescontoGeral = c.DescontoGeral,
                PermiteFidelidade = c.PermiteFidelidade, PrazoPagamento = c.PrazoPagamento,
                QtdeDias = c.QtdeDias, DiaFechamento = c.DiaFechamento, DiaVencimento = c.DiaVencimento,
                QtdeMeses = c.QtdeMeses, PermiteVendaParcelada = c.PermiteVendaParcelada,
                QtdeMaxParcelas = c.QtdeMaxParcelas, PermiteVendaPrazo = c.PermiteVendaPrazo,
                PermiteVendaVista = c.PermiteVendaVista, Bloqueado = c.Bloqueado,
                CalcularJuros = c.CalcularJuros, BloquearComissao = c.BloquearComissao,
                PedirSenhaVendaPrazo = c.PedirSenhaVendaPrazo, SenhaVendaPrazo = c.SenhaVendaPrazo,
                Aviso = c.Aviso, Observacao = c.Observacao, Ativo = c.Ativo, CriadoEm = c.CriadoEm,
                Enderecos = c.Pessoa.Enderecos.Select(e => new EnderecoDto
                {
                    Id = e.Id, Tipo = e.Tipo, Cep = e.Cep, Rua = e.Rua, Numero = e.Numero,
                    Complemento = e.Complemento, Bairro = e.Bairro, Cidade = e.Cidade, Uf = e.Uf, Principal = e.Principal
                }).ToList(),
                Contatos = c.Pessoa.Contatos.Select(ct => new ContatoDto
                {
                    Id = ct.Id, Tipo = ct.Tipo, Valor = ct.Valor, Descricao = ct.Descricao, Principal = ct.Principal
                }).ToList(),
                Convenios = c.Convenios.Select(cv => new ClienteConvenioDto
                {
                    Id = cv.Id, ConvenioId = cv.ConvenioId, ConvenioNome = cv.Convenio?.Pessoa?.Nome,
                    Matricula = cv.Matricula, Cartao = cv.Cartao, Limite = cv.Limite
                }).ToList(),
                Autorizacoes = c.Autorizacoes.Select(a => new ClienteAutorizacaoDto { Id = a.Id, Nome = a.Nome }).ToList(),
                Descontos = c.Descontos.Select(d => new ClienteDescontoDto
                {
                    Id = d.Id, ProdutoId = d.ProdutoId, TipoAgrupador = d.TipoAgrupador,
                    AgrupadorId = d.AgrupadorId, AgrupadorOuProdutoNome = d.AgrupadorOuProdutoNome,
                    DescontoMinimo = d.DescontoMinimo, DescontoMaxSemSenha = d.DescontoMaxSemSenha, DescontoMaxComSenha = d.DescontoMaxComSenha
                }).ToList(),
                UsosContinuos = c.UsosContinuos.Select(u => new ClienteUsoContinuoDto
                {
                    Id = u.Id, ProdutoId = u.ProdutoId, ProdutoCodigo = u.Produto?.Codigo,
                    ProdutoNome = u.Produto?.Nome, Fabricante = u.Fabricante,
                    Apresentacao = u.Apresentacao, QtdeAoDia = u.QtdeAoDia,
                    UltimaCompra = u.UltimaCompra?.ToString("yyyy-MM-dd"),
                    ProximaCompra = u.ProximaCompra?.ToString("yyyy-MM-dd"),
                    ColaboradorNome = u.ColaboradorNome
                }).ToList()
            };
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ClienteService.ObterAsync | Id: {Id}", id); throw; }
    }

    public async Task<ClienteListDto> CriarAsync(ClienteFormDto dto)
    {
        try
        {
            Validar(dto);
            var pessoa = await ResolverPessoa(dto);
            if (pessoa.Cliente != null) throw new ArgumentException("Este CPF/CNPJ já possui um cliente cadastrado.");

            var cli = MapearCliente(dto);
            cli.PessoaId = pessoa.Id;
            _db.Set<Cliente>().Add(cli);

            AtualizarPessoaEnderecos(pessoa, dto);
            AtualizarPessoaContatos(pessoa, dto);

            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, cli.Id, novo: ParaDict(cli, pessoa));

            return new ClienteListDto { Id = cli.Id, Nome = pessoa.Nome, Tipo = pessoa.Tipo, CpfCnpj = pessoa.CpfCnpj, Ativo = cli.Ativo, CriadoEm = cli.CriadoEm };
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em ClienteService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, ClienteFormDto dto)
    {
        try
        {
            Validar(dto);
            var cli = await _db.Set<Cliente>()
                .Include(c => c.Pessoa).ThenInclude(p => p.Enderecos)
                .Include(c => c.Pessoa).ThenInclude(p => p.Contatos)
                .Include(c => c.Convenios).Include(c => c.Autorizacoes)
                .Include(c => c.Descontos).Include(c => c.UsosContinuos)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new KeyNotFoundException($"Cliente {id} não encontrado.");

            var anterior = ParaDict(cli, cli.Pessoa);
            var pessoa = cli.Pessoa;

            // Atualizar dados pessoa
            pessoa.Tipo = dto.Tipo; pessoa.Nome = dto.Nome.Trim().ToUpper();
            pessoa.RazaoSocial = dto.RazaoSocial?.Trim().ToUpper();
            pessoa.CpfCnpj = dto.CpfCnpj.Replace(".", "").Replace("-", "").Replace("/", "").Trim();
            pessoa.InscricaoEstadual = dto.InscricaoEstadual?.Trim();
            pessoa.Rg = dto.Rg?.Trim();
            pessoa.DataNascimento = string.IsNullOrEmpty(dto.DataNascimento) ? null : DateTime.Parse(dto.DataNascimento);

            // Atualizar cliente
            AtualizarCamposCliente(cli, dto);

            // Enderecos e contatos
            AtualizarPessoaEnderecos(pessoa, dto);
            AtualizarPessoaContatos(pessoa, dto);

            // Recriar sub-tabelas
            _db.Set<ClienteConvenio>().RemoveRange(cli.Convenios);
            _db.Set<ClienteAutorizacao>().RemoveRange(cli.Autorizacoes);
            _db.Set<ClienteDesconto>().RemoveRange(cli.Descontos);
            _db.Set<ClienteUsoContinuo>().RemoveRange(cli.UsosContinuos);

            foreach (var cv in dto.Convenios)
                cli.Convenios.Add(new ClienteConvenio { ConvenioId = cv.ConvenioId, Matricula = cv.Matricula, Cartao = cv.Cartao, Limite = cv.Limite });
            foreach (var a in dto.Autorizacoes)
                cli.Autorizacoes.Add(new ClienteAutorizacao { Nome = a.Nome.Trim().ToUpper() });
            foreach (var d in dto.Descontos)
                cli.Descontos.Add(new ClienteDesconto { ProdutoId = d.ProdutoId, TipoAgrupador = d.TipoAgrupador, AgrupadorId = d.AgrupadorId, AgrupadorOuProdutoNome = d.AgrupadorOuProdutoNome, DescontoMaxSemSenha = d.DescontoMaxSemSenha, DescontoMaxComSenha = d.DescontoMaxComSenha });
            foreach (var u in dto.UsosContinuos)
                cli.UsosContinuos.Add(new ClienteUsoContinuo { ProdutoId = u.ProdutoId, Fabricante = u.Fabricante, Apresentacao = u.Apresentacao, QtdeAoDia = u.QtdeAoDia, UltimaCompra = string.IsNullOrEmpty(u.UltimaCompra) ? null : DateTime.Parse(u.UltimaCompra), ProximaCompra = string.IsNullOrEmpty(u.ProximaCompra) ? null : DateTime.Parse(u.ProximaCompra), ColaboradorNome = u.ColaboradorNome });

            await _db.SaveChangesAsync();
            var novo = ParaDict(cli, pessoa);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em ClienteService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var cli = await _db.Set<Cliente>()
                .Include(c => c.Convenios).Include(c => c.Autorizacoes)
                .Include(c => c.Descontos).Include(c => c.UsosContinuos)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new KeyNotFoundException($"Cliente {id} não encontrado.");
            var dados = ParaDict(cli, null);
            _db.Set<ClienteConvenio>().RemoveRange(cli.Convenios);
            _db.Set<ClienteAutorizacao>().RemoveRange(cli.Autorizacoes);
            _db.Set<ClienteDesconto>().RemoveRange(cli.Descontos);
            _db.Set<ClienteUsoContinuo>().RemoveRange(cli.UsosContinuos);
            _db.Set<Cliente>().Remove(cli);
            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var rec = await _db.Set<Cliente>().FindAsync(id);
                rec!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException) { Log.Error(ex, "Erro em ClienteService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    // ── Helpers ─────────────────────────────────────────────────────
    private async Task<Pessoa> ResolverPessoa(ClienteFormDto dto)
    {
        var cpfCnpj = dto.CpfCnpj.Replace(".", "").Replace("-", "").Replace("/", "").Trim();
        var existente = await _db.Pessoas.Include(p => p.Cliente).FirstOrDefaultAsync(p => p.CpfCnpj == cpfCnpj);
        if (existente != null)
        {
            existente.Nome = dto.Nome.Trim().ToUpper();
            existente.RazaoSocial = dto.RazaoSocial?.Trim().ToUpper();
            existente.InscricaoEstadual = dto.InscricaoEstadual?.Trim();
            existente.Rg = dto.Rg?.Trim();
            existente.DataNascimento = string.IsNullOrEmpty(dto.DataNascimento) ? null : DateTime.Parse(dto.DataNascimento);
            return existente;
        }
        var nova = new Pessoa
        {
            Tipo = dto.Tipo, Nome = dto.Nome.Trim().ToUpper(), RazaoSocial = dto.RazaoSocial?.Trim().ToUpper(),
            CpfCnpj = cpfCnpj, InscricaoEstadual = dto.InscricaoEstadual?.Trim(), Rg = dto.Rg?.Trim(),
            DataNascimento = string.IsNullOrEmpty(dto.DataNascimento) ? null : DateTime.Parse(dto.DataNascimento)
        };
        _db.Pessoas.Add(nova);
        await _db.SaveChangesAsync();
        return nova;
    }

    private static Cliente MapearCliente(ClienteFormDto dto)
    {
        var cli = new Cliente();
        AtualizarCamposCliente(cli, dto);
        foreach (var cv in dto.Convenios) cli.Convenios.Add(new ClienteConvenio { ConvenioId = cv.ConvenioId, Matricula = cv.Matricula, Cartao = cv.Cartao, Limite = cv.Limite });
        foreach (var a in dto.Autorizacoes) cli.Autorizacoes.Add(new ClienteAutorizacao { Nome = a.Nome.Trim().ToUpper() });
        foreach (var d in dto.Descontos) cli.Descontos.Add(new ClienteDesconto { ProdutoId = d.ProdutoId, TipoAgrupador = d.TipoAgrupador, AgrupadorId = d.AgrupadorId, AgrupadorOuProdutoNome = d.AgrupadorOuProdutoNome, DescontoMaxSemSenha = d.DescontoMaxSemSenha, DescontoMaxComSenha = d.DescontoMaxComSenha });
        foreach (var u in dto.UsosContinuos) cli.UsosContinuos.Add(new ClienteUsoContinuo { ProdutoId = u.ProdutoId, Fabricante = u.Fabricante, Apresentacao = u.Apresentacao, QtdeAoDia = u.QtdeAoDia, UltimaCompra = string.IsNullOrEmpty(u.UltimaCompra) ? null : DateTime.Parse(u.UltimaCompra), ProximaCompra = string.IsNullOrEmpty(u.ProximaCompra) ? null : DateTime.Parse(u.ProximaCompra), ColaboradorNome = u.ColaboradorNome });
        return cli;
    }

    private static void AtualizarCamposCliente(Cliente cli, ClienteFormDto dto)
    {
        cli.LimiteCredito = dto.LimiteCredito; cli.DescontoGeral = dto.DescontoGeral;
        cli.PermiteFidelidade = dto.PermiteFidelidade; cli.PrazoPagamento = dto.PrazoPagamento;
        cli.QtdeDias = dto.PrazoPagamento == ModoFechamento.DiasCorridos ? dto.QtdeDias : null;
        cli.DiaFechamento = dto.PrazoPagamento == ModoFechamento.PorFechamento ? dto.DiaFechamento : null;
        cli.DiaVencimento = dto.PrazoPagamento == ModoFechamento.PorFechamento ? dto.DiaVencimento : null;
        cli.QtdeMeses = dto.PrazoPagamento == ModoFechamento.PorFechamento ? dto.QtdeMeses : null;
        cli.PermiteVendaParcelada = dto.PermiteVendaParcelada; cli.QtdeMaxParcelas = Math.Max(1, dto.QtdeMaxParcelas);
        cli.PermiteVendaPrazo = dto.PermiteVendaPrazo; cli.PermiteVendaVista = dto.PermiteVendaVista;
        cli.Bloqueado = dto.Bloqueado; cli.CalcularJuros = dto.CalcularJuros;
        cli.BloquearComissao = dto.BloquearComissao; cli.PedirSenhaVendaPrazo = dto.PedirSenhaVendaPrazo;
        cli.SenhaVendaPrazo = dto.SenhaVendaPrazo; cli.Aviso = dto.Aviso?.Trim();
        cli.Observacao = dto.Observacao?.Trim(); cli.Ativo = dto.Ativo;
    }

    private void AtualizarPessoaEnderecos(Pessoa pessoa, ClienteFormDto dto)
    {
        _db.PessoasEndereco.RemoveRange(pessoa.Enderecos);
        foreach (var e in dto.Enderecos)
            pessoa.Enderecos.Add(new PessoaEndereco { PessoaId = pessoa.Id, Tipo = e.Tipo, Cep = e.Cep, Rua = e.Rua, Numero = e.Numero, Complemento = e.Complemento, Bairro = e.Bairro, Cidade = e.Cidade, Uf = e.Uf, Principal = e.Principal });
    }

    private void AtualizarPessoaContatos(Pessoa pessoa, ClienteFormDto dto)
    {
        _db.PessoasContato.RemoveRange(pessoa.Contatos);
        foreach (var ct in dto.Contatos)
            pessoa.Contatos.Add(new PessoaContato { PessoaId = pessoa.Id, Tipo = ct.Tipo, Valor = ct.Valor, Descricao = ct.Descricao, Principal = ct.Principal });
    }

    private static void Validar(ClienteFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CpfCnpj)) throw new ArgumentException("CPF/CNPJ é obrigatório.");
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException(dto.Tipo == "J" ? "Nome Fantasia é obrigatório." : "Nome é obrigatório.");
    }

    private static Dictionary<string, string?> ParaDict(Cliente c, Pessoa? p) => new()
    {
        ["Nome"] = p?.Nome, ["CpfCnpj"] = p?.CpfCnpj, ["Tipo"] = p?.Tipo,
        ["LimiteCredito"] = c.LimiteCredito.ToString("N2"), ["Bloqueado"] = c.Bloqueado ? "Sim" : "Não",
        ["Ativo"] = c.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b) =>
        a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
