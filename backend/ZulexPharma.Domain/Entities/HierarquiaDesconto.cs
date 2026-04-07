using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class HierarquiaDesconto : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public bool Padrao { get; set; }
    public bool AplicarAutomatico { get; set; }
    public DescontoAutoTipo? DescontoAutoTipo { get; set; }
    public bool BuscarMenorValorPromocao { get; set; }

    public ICollection<HierarquiaDescontoItem> Itens { get; set; } = new List<HierarquiaDescontoItem>();
    public ICollection<HierarquiaDescontoColaborador> Colaboradores { get; set; } = new List<HierarquiaDescontoColaborador>();
    public ICollection<HierarquiaDescontoConvenio> Convenios { get; set; } = new List<HierarquiaDescontoConvenio>();
    public ICollection<HierarquiaDescontoCliente> Clientes { get; set; } = new List<HierarquiaDescontoCliente>();
}

public class HierarquiaDescontoItem
{
    public long Id { get; set; }
    public long HierarquiaDescontoId { get; set; }
    public HierarquiaDesconto HierarquiaDesconto { get; set; } = null!;
    public int Ordem { get; set; }
    public ComponenteDesconto Componente { get; set; }
    public ICollection<HierarquiaDescontoSecao> Secoes { get; set; } = new List<HierarquiaDescontoSecao>();
}

/// <summary>Quando componente = SecaoEscolhida, quais seções foram selecionadas.</summary>
public class HierarquiaDescontoSecao
{
    public long Id { get; set; }
    public long HierarquiaDescontoItemId { get; set; }
    public HierarquiaDescontoItem HierarquiaDescontoItem { get; set; } = null!;
    public long SecaoId { get; set; }
    public Secao Secao { get; set; } = null!;
}

public class HierarquiaDescontoColaborador
{
    public long Id { get; set; }
    public long HierarquiaDescontoId { get; set; }
    public HierarquiaDesconto HierarquiaDesconto { get; set; } = null!;
    public long ColaboradorId { get; set; }
    public Colaborador Colaborador { get; set; } = null!;
}

public class HierarquiaDescontoConvenio
{
    public long Id { get; set; }
    public long HierarquiaDescontoId { get; set; }
    public HierarquiaDesconto HierarquiaDesconto { get; set; } = null!;
    public long ConvenioId { get; set; }
    public Convenio Convenio { get; set; } = null!;
}

public class HierarquiaDescontoCliente
{
    public long Id { get; set; }
    public long HierarquiaDescontoId { get; set; }
    public HierarquiaDesconto HierarquiaDesconto { get; set; } = null!;
    public long ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
}
