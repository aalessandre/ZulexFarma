namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Endereços de uma Pessoa.
/// Uma pessoa pode ter múltiplos endereços (entrega, cobrança, etc.).
/// </summary>
public class PessoaEndereco : BaseEntity
{
    public long PessoaId { get; set; }
    public Pessoa Pessoa { get; set; } = null!;

    /// <summary>Ex.: PRINCIPAL | ENTREGA | COBRANÇA | OUTRO</summary>
    public string Tipo { get; set; } = "PRINCIPAL";

    public string Cep { get; set; } = string.Empty;
    public string Rua { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;

    public bool Principal { get; set; } = false;
}
