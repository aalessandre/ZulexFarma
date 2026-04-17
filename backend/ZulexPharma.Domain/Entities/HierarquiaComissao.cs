using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class HierarquiaComissao : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public bool Padrao { get; set; }

    public ICollection<HierarquiaComissaoItem> Itens { get; set; } = new List<HierarquiaComissaoItem>();
    public ICollection<HierarquiaComissaoColaborador> Colaboradores { get; set; } = new List<HierarquiaComissaoColaborador>();
}

public class HierarquiaComissaoItem
{
    public long Id { get; set; }
    public long HierarquiaComissaoId { get; set; }
    public HierarquiaComissao HierarquiaComissao { get; set; } = null!;
    public int Ordem { get; set; }
    public ComponenteComissao Componente { get; set; }
    public ICollection<HierarquiaComissaoSecao> Secoes { get; set; } = new List<HierarquiaComissaoSecao>();
}

/// <summary>Quando componente = SecaoEscolhida, quais seções foram selecionadas.</summary>
public class HierarquiaComissaoSecao
{
    public long Id { get; set; }
    public long HierarquiaComissaoItemId { get; set; }
    public HierarquiaComissaoItem HierarquiaComissaoItem { get; set; } = null!;
    public long SecaoId { get; set; }
    public Secao Secao { get; set; } = null!;
}

public class HierarquiaComissaoColaborador
{
    public long Id { get; set; }
    public long HierarquiaComissaoId { get; set; }
    public HierarquiaComissao HierarquiaComissao { get; set; } = null!;
    public long ColaboradorId { get; set; }
    public Colaborador Colaborador { get; set; } = null!;
}
