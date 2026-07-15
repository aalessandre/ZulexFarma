namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Dead-letter do sync (Fase 1): operacoes recebidas que NAO puderam ser aplicadas
/// (dependencia faltando, conflito de chave, tipo desconhecido, erro). Ficam aqui pra
/// RETRY (drenagem a cada ciclo) ate' sucesso ou teto de tentativas — em vez de serem
/// perdidas em silencio ao avancar o ponteiro. E' INFRA local (nao herda BaseEntity,
/// nao replica). Chave logica: (Tabela, RegistroId, Operacao).
/// </summary>
public class SyncQuarentena
{
    public long Id { get; set; }
    public string Tabela { get; set; } = "";
    public string Operacao { get; set; } = ""; // I, U, D
    public long RegistroId { get; set; }
    public string? DadosJson { get; set; }
    public DateTime OpCriadoEm { get; set; }    // timestamp da op de origem (pro LWW no retry)
    public long NoOrigemId { get; set; }
    public string Motivo { get; set; } = "";    // PrecisaRetry | Conflito | TipoDesconhecido | Erro
    public int Tentativas { get; set; }
    public string? UltimoErro { get; set; }
    public bool Resolvido { get; set; }
    public DateTime CriadoEm { get; set; } = Helpers.DataHoraHelper.Agora();
    public DateTime AtualizadoEm { get; set; } = Helpers.DataHoraHelper.Agora();
}
