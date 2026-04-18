namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Município brasileiro com código IBGE (7 dígitos).
/// Tabela seed estática — carregada na migration a partir da API IBGE.
/// Usada como FK em PessoaEndereco e Filial para garantir código IBGE válido
/// na emissão fiscal (cMun/cMunFG na NFe).
/// </summary>
public class Municipio : BaseEntity
{
    /// <summary>Código IBGE de 7 dígitos (ex: "4119400" = Ponta Grossa/PR).</summary>
    public string CodigoIbge { get; set; } = string.Empty;

    /// <summary>Nome oficial IBGE (ex: "Ponta Grossa").</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Nome em maiúsculas sem acento, pra busca case/acento-insensitive.</summary>
    public string NomeNormalizado { get; set; } = string.Empty;

    /// <summary>Sigla UF (ex: "PR").</summary>
    public string Uf { get; set; } = string.Empty;
}
