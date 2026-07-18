namespace ZulexPharma.Domain.Entities;

// FASE 6 (b+c): promovido a BaseEntity — replica sozinho (op I/U/D propria + LWW por filho), entao
// dois nos adicionando convenios DIFERENTES ao mesmo cliente = UNIAO (nada se perde). Antes era POCO
// e viajava no JSON do pai sob delete-missing (o vencedor apagava o filho do perdedor em silencio).
public class ClienteConvenio : BaseEntity
{
    public long ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public long ConvenioId { get; set; }
    public Convenio Convenio { get; set; } = null!;
    public string? Matricula { get; set; }
    public string? Cartao { get; set; }
    public decimal Limite { get; set; }
}
