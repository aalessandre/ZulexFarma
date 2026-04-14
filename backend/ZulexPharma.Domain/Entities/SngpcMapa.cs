using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Mapa mensal SNGPC — o XML oficial enviado à Anvisa consolidando entradas, saídas,
/// perdas e receitas do mês. Um mapa por filial por competência (mês/ano).
/// </summary>
public class SngpcMapa : BaseEntity
{
    public long FilialId { get; set; }

    public int CompetenciaMes { get; set; }   // 1..12
    public int CompetenciaAno { get; set; }   // ex: 2026

    public StatusSngpcMapa Status { get; set; } = StatusSngpcMapa.Rascunho;

    public DateTime? DataGeracao { get; set; }
    public DateTime? DataEnvio { get; set; }

    public string? XmlConteudo { get; set; }
    public string? ProtocoloAnvisa { get; set; }
    public string? Observacao { get; set; }

    // Totais para dashboard
    public int TotalEntradas { get; set; }
    public int TotalSaidas { get; set; }
    public int TotalReceitas { get; set; }
    public int TotalPerdas { get; set; }

    public long? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
