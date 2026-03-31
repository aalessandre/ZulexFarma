namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Nomenclatura Comum do Mercosul — classificação fiscal de produtos.
/// </summary>
public class Ncm : BaseEntity
{
    /// <summary>Código NCM sem máscara (ex: "30049069")</summary>
    public string CodigoNcm { get; set; } = "";

    /// <summary>Descrição oficial do NCM</summary>
    public string Descricao { get; set; } = "";

    /// <summary>Ex-TIPI (exceção da tabela IPI)</summary>
    public string? ExTipi { get; set; }

    /// <summary>Unidade tributável (ex: "KG", "UN")</summary>
    public string? UnidadeTributavel { get; set; }

    public ICollection<NcmFederal> Federais { get; set; } = new List<NcmFederal>();
    public ICollection<NcmIcmsUf> IcmsUfs { get; set; } = new List<NcmIcmsUf>();
    public ICollection<NcmStUf> StUfs { get; set; } = new List<NcmStUf>();
}
