namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Fornecedor. Dados pessoais/empresariais ficam em Pessoa.
/// Contatos e endereços ficam em PessoaContato / PessoaEndereco.
/// </summary>
public class Fornecedor : BaseEntity
{
    public long PessoaId { get; set; }
    public Pessoa Pessoa { get; set; } = null!;
}
