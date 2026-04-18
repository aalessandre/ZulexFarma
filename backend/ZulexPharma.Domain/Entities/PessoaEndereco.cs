namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Endereços de uma Pessoa.
/// Uma pessoa pode ter múltiplos endereços (entrega, cobrança, etc.).
/// </summary>
public class PessoaEndereco : BaseEntity
{
    public long PessoaId { get; set; }
    public Pessoa Pessoa { get; set; } = null!;

    /// <summary>Ex.: CASA | ENTREGA | COBRANÇA | OUTRO</summary>
    public string Tipo { get; set; } = "CASA";

    public string Cep { get; set; } = string.Empty;
    public string Rua { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;

    /// <summary>Codigo IBGE do municipio (7 digitos, obrigatorio para NF-e).
    /// Snapshot denormalizado — valor autoritativo vem de <see cref="Municipio"/> via <see cref="MunicipioId"/>.</summary>
    public string? CodigoIbgeMunicipio { get; set; }

    /// <summary>FK para Municipio — garante código IBGE válido na emissão fiscal.</summary>
    public long? MunicipioId { get; set; }
    public Municipio? Municipio { get; set; }

    public bool Principal { get; set; } = false;
}
