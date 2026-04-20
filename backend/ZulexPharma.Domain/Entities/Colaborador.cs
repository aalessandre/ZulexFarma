namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Dados específicos de um colaborador (funcionário).
/// Dados pessoais (Nome, CPF, RG, DataNascimento) ficam em Pessoa.
/// Contatos e endereços ficam em PessoaContato / PessoaEndereco.
/// </summary>
public class Colaborador : BaseEntity
{
    public long PessoaId { get; set; }
    public Pessoa Pessoa { get; set; } = null!;

    public string? Apelido { get; set; }
    public string? Genero { get; set; }
    public string? Cargo { get; set; }
    public DateTime? DataAdmissao { get; set; }
    public decimal? Salario { get; set; }
    public string? Observacao { get; set; }
    public bool PermitirAbrirCaixa { get; set; }

    /// <summary>Senha do colaborador cadastrada no portal DATASUS Farmácia Popular (criptografada AES-256).</summary>
    public string? SenhaFarmaciaPopularCripto { get; set; }

    public Usuario? Usuario { get; set; }
}
