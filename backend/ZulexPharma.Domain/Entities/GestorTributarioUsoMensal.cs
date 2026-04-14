namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Contador local de uso mensal da API do Gestor Tributário.
/// Usado pelo rate limit defensivo: antes de cada chamada, verifica o consumo
/// contra o limite configurado (padrão Avant: 50.000/mês).
/// Um registro por (Ano, Mes, Provider).
/// </summary>
public class GestorTributarioUsoMensal : BaseEntity
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public string Provider { get; set; } = "avant";
    public int RequisicoesUsadas { get; set; }
    public int RequisicoesRevisao { get; set; }
    public int RequisicoesAtualizacao { get; set; }
    public int RequisicoesDifal { get; set; }
    public DateTime? UltimaChamadaEm { get; set; }
}
