namespace ZulexPharma.Domain.Entities;

public abstract class BaseEntity
{
    public long Id { get; set; }
    public string? Codigo { get; set; }
    public DateTime CriadoEm { get; set; } = Helpers.DataHoraHelper.Agora();
    public DateTime? AtualizadoEm { get; set; }
    public bool Ativo { get; set; } = true;

    /// <summary>
    /// No/servidor de ORIGEM do registro (codigo do no, vem do appsettings "No:Codigo").
    /// E' o eixo Origem/No (onde foi criado), NAO a filial-dona do dado.
    /// Usado pra faixa de Id, anti-eco no sync e auditoria de origem.
    /// </summary>
    public long? NoOrigemId { get; set; }

    /// <summary>
    /// FASE 3 — no que ESCREVEU a versao ATUAL da linha (o escritor real do LWW). O NoOrigemId acima
    /// e' o CRIADOR (imutavel) — desempatar por ele fazia o empate de timestamp virar "primeiro que
    /// chegou vence" e os nos divergiam. Carimbado pelo outbox (escrita local = No:Codigo) e pelo
    /// applicator (escrita remota = no da op). Null = versao legada (o comparador cai pro NoOrigemId).
    /// </summary>
    public long? AtualizadoPorNoId { get; set; }

    /// <summary>GUID auxiliar para reconciliação no sync. Não é PK, sem FK.</summary>
    public Guid SyncGuid { get; set; } = Guid.NewGuid();
}
