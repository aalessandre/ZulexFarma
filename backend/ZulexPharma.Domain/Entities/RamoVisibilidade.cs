using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Override de visibilidade de um elemento de UI (tile/tela/seção/campo) por ramo.
/// Guarda só as EXCEÇÕES: se não houver linha, vale o default (a feature do
/// elemento ∈ features do ramo). Editado pelo configurador SH.
/// Ver docs/specs/configurador-ramo-visibilidade.md.
/// </summary>
public class RamoVisibilidade
{
    public long Id { get; set; }
    public RamoFilial Ramo { get; set; }
    /// <summary>Id do elemento no catálogo do frontend (ex.: "produto.campo.preco-fp-bolsa").</summary>
    public string ElementoId { get; set; } = string.Empty;
    /// <summary>true = força mostrar; false = força esconder (override do default por feature).</summary>
    public bool Visivel { get; set; }
}
