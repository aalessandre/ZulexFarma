namespace ZulexPharma.Domain.Entities;

public class Filial : BaseEntity
{
    public string NomeFilial { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string? InscricaoEstadual { get; set; }
    public string Cep { get; set; } = string.Empty;
    public string Rua { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    /// <summary>Código IBGE do município (7 dígitos, obrigatório para NFC-e).
    /// Snapshot denormalizado — valor autoritativo vem de <see cref="Municipio"/> via <see cref="MunicipioId"/>.</summary>
    public string? CodigoIbgeMunicipio { get; set; }

    /// <summary>FK para Municipio — garante código IBGE válido na emissão fiscal.</summary>
    public long? MunicipioId { get; set; }
    public Municipio? Municipio { get; set; }

    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Alíquota de ICMS interna do estado da filial (ex: 19.5 para PR).</summary>
    public decimal AliquotaIcms { get; set; }

    /// <summary>Incluir esta filial nas promoções fixas da matriz.</summary>
    public bool IncluirPromoFixa { get; set; } = true;

    /// <summary>Incluir esta filial nas promoções progressivas da matriz.</summary>
    public bool IncluirPromoProgressiva { get; set; } = true;

    /// <summary>Conta bancária que representa o cofre físico da filial.
    /// Obrigatória para abrir o caixa. Recebe sangrias e fornece suprimentos.</summary>
    public long? ContaCofreId { get; set; }
    public ContaBancaria? ContaCofre { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<UsuarioFilialGrupo> UsuarioFilialGrupos { get; set; } = new List<UsuarioFilialGrupo>();
}
