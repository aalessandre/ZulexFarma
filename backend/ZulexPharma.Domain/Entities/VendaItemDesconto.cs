using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class VendaItemDesconto
{
    public long Id { get; set; }
    public long VendaItemId { get; set; }
    public VendaItem VendaItem { get; set; } = null!;

    /// <summary>Desconto (hierarquia) ou Promocao (fixa/progressiva).</summary>
    public TipoDescontoVenda Tipo { get; set; }

    /// <summary>Percentual aplicado por esta regra.</summary>
    public decimal Percentual { get; set; }

    /// <summary>Componente/origem: GrupoPrincipal, Cliente, Convenio, PromocaoFixa, PromocaoProgressiva, etc.</summary>
    public string Origem { get; set; } = string.Empty;

    /// <summary>Nome legível da regra: "Padrão", "Farmácia Popular", "Dia das Mães".</summary>
    public string Regra { get; set; } = string.Empty;

    /// <summary>ID da hierarquia, promoção ou entidade que originou o desconto.</summary>
    public long? OrigemId { get; set; }

    /// <summary>Colaborador que liberou via senha (quando desconto acima do máximo).</summary>
    public long? LiberadoPorId { get; set; }
    public Colaborador? LiberadoPor { get; set; }
}
