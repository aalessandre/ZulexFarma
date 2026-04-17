namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Comissão de um colaborador por agrupador de produto.
/// TipoAgrupador: 1=GrupoPrincipal, 2=GrupoProduto, 3=SubGrupo, 4=Secao.
/// </summary>
public class ColaboradorComissaoAgrupador : BaseEntity
{
    public long ColaboradorId { get; set; }
    public Colaborador Colaborador { get; set; } = null!;

    /// <summary>1=GrupoPrincipal, 2=GrupoProduto, 3=SubGrupo, 4=Secao</summary>
    public int TipoAgrupador { get; set; }

    /// <summary>Id do agrupador selecionado.</summary>
    public long AgrupadorId { get; set; }

    /// <summary>Nome do agrupador (desnormalizado para exibição).</summary>
    public string AgrupadorNome { get; set; } = string.Empty;

    /// <summary>Percentual de comissão para este agrupador.</summary>
    public decimal ComissaoPercentual { get; set; }
}
