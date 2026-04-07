using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.HierarquiaDescontos;

public class HierarquiaDescontoListDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Padrao { get; set; }
    public bool AplicarAutomatico { get; set; }
    public int TotalItens { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class HierarquiaDescontoDetalheDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Padrao { get; set; }
    public bool AplicarAutomatico { get; set; }
    public DescontoAutoTipo? DescontoAutoTipo { get; set; }
    public bool BuscarMenorValorPromocao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public List<HierarquiaItemDto> Itens { get; set; } = new();
    public List<long> ColaboradorIds { get; set; } = new();
    public List<long> ConvenioIds { get; set; } = new();
    public List<long> ClienteIds { get; set; } = new();
}

public class HierarquiaDescontoFormDto
{
    public string Nome { get; set; } = string.Empty;
    public bool Padrao { get; set; }
    public bool AplicarAutomatico { get; set; }
    public DescontoAutoTipo? DescontoAutoTipo { get; set; }
    public bool BuscarMenorValorPromocao { get; set; }
    public bool Ativo { get; set; } = true;
    public List<HierarquiaItemDto> Itens { get; set; } = new();
    public List<long> ColaboradorIds { get; set; } = new();
    public List<long> ConvenioIds { get; set; } = new();
    public List<long> ClienteIds { get; set; } = new();
}

public class HierarquiaItemDto
{
    public int Ordem { get; set; }
    public ComponenteDesconto Componente { get; set; }
    public List<long> SecaoIds { get; set; } = new();
}
