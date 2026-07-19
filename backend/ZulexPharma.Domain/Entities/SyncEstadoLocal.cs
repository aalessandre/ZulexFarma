namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Estado LOCAL do sync deste deployment (chave/valor). INFRA — nao herda BaseEntity, NUNCA replica.
/// Nasce na fase 1 pro InstanciaUid ('sync.instancia.uid'); a fase 2 move pra ca' o cursor do pull
/// (hoje em Configuracoes, que replica — bug com teste vermelho: Configuracao_NaoDeveEntrarNaSyncFila).
/// </summary>
public class SyncEstadoLocal
{
    public string Chave { get; set; } = "";
    public string Valor { get; set; } = "";
    public DateTime AtualizadoEm { get; set; } = Helpers.DataHoraHelper.Agora();
}
