using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.HierarquiaComissoes;

public class HierarquiaComissaoListDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Padrao { get; set; }
    public int TotalItens { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class HierarquiaComissaoDetalheDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Padrao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public List<HierarquiaComissaoItemDto> Itens { get; set; } = new();
    public List<long> ColaboradorIds { get; set; } = new();
}

public class HierarquiaComissaoFormDto
{
    public string Nome { get; set; } = string.Empty;
    public bool Padrao { get; set; }
    public bool Ativo { get; set; } = true;
    public List<HierarquiaComissaoItemDto> Itens { get; set; } = new();
    public List<long> ColaboradorIds { get; set; } = new();
}

public class HierarquiaComissaoItemDto
{
    public int Ordem { get; set; }
    public ComponenteComissao Componente { get; set; }
    public List<long> SecaoIds { get; set; } = new();
}
