namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Lapide (tombstone) de replicacao: marca que um registro foi DELETADO, pra impedir
/// RESSURREICAO (um INSERT/UPDATE velho de outro no que chega depois da morte nao pode
/// re-criar a linha). Guarda SO' o marcador — Tabela + RegistroId + quando morreu + qual no —
/// SEM nenhum dado de negocio (o dado deletado some fisicamente; nada de PII aqui = LGPD ok).
/// E' INFRA local (nao herda BaseEntity, nao replica): cada no crava a propria lapide ao
/// aplicar um DELETE. Purgada apos a retencao (SyncTombstoneRetencaoDias).
/// Chave logica: (Tabela, RegistroId).
/// </summary>
public class SyncTombstone
{
    public long Id { get; set; }
    public string Tabela { get; set; } = "";
    public long RegistroId { get; set; }
    public DateTime DeletadoEm { get; set; }   // timestamp da morte (op.CriadoEm de origem) — base do LWW
    public long NoOrigemId { get; set; }
    public DateTime CriadoEm { get; set; } = Helpers.DataHoraHelper.Agora();
}
