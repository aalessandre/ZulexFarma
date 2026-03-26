namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Entidade base para qualquer pessoa — física ou jurídica.
/// Dados específicos (Cliente, Fornecedor, Colaborador, etc.) ficam em tabelas derivadas.
/// </summary>
public class Pessoa : BaseEntity
{
    /// <summary>F = Física | J = Jurídica</summary>
    public string Tipo { get; set; } = "F";

    /// <summary>Nome (PF) ou Nome Fantasia (PJ)</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Razão Social — preenchido apenas para PJ</summary>
    public string? RazaoSocial { get; set; }

    /// <summary>CPF (PF) ou CNPJ (PJ) — único na tabela</summary>
    public string CpfCnpj { get; set; } = string.Empty;

    public string? InscricaoEstadual { get; set; }

    /// <summary>RG — apenas PF</summary>
    public string? Rg { get; set; }

    /// <summary>Data de nascimento — apenas PF</summary>
    public DateTime? DataNascimento { get; set; }

    public string? Observacao { get; set; }

    public ICollection<PessoaContato>  Contatos  { get; set; } = new List<PessoaContato>();
    public ICollection<PessoaEndereco> Enderecos { get; set; } = new List<PessoaEndereco>();

    public Colaborador? Colaborador { get; set; }
    public Fornecedor? Fornecedor { get; set; }
}
