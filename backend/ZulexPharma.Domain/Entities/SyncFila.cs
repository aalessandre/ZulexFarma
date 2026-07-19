namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Fila de operações para sincronização.
/// Cada INSERT/UPDATE/DELETE gera um registro aqui.
/// </summary>
public class SyncFila
{
    public long Id { get; set; }
    public string Tabela { get; set; } = "";
    public string Operacao { get; set; } = ""; // I, U, D
    public long RegistroId { get; set; }
    public string? RegistroCodigo { get; set; }
    public string? DadosJson { get; set; }

    /// <summary>No/servidor de origem da operacao (eixo Origem/No). Usado pra anti-eco no PULL.</summary>
    public long NoOrigemId { get; set; }

    /// <summary>
    /// Filial-DONA do dado (eixo escopo, vem do usuario logado).
    /// Null = GLOBAL (replica pra todos os nos). Preenchido = POR-FILIAL (so' a filial dona + nuvem).
    /// Populado na Fase 0; o roteamento por-filial passa a USAR na Fase 3.
    /// </summary>
    public long? FilialDonoId { get; set; }

    /// <summary>
    /// Identidade GLOBAL e IMUTAVEL da operacao — chave de idempotencia fim-a-fim (Fase 4b).
    /// Nasce com a linha no no que GEROU a op (Guid novo no outbox) e viaja junto no PUSH; a central grava
    /// o MESMO OpUid na linha de redistribuicao, entao re-envio (PUSH ok + resposta perdida) e' reconhecido
    /// e nao duplica. Indice parcial UNICO (OpUid) WHERE OpUid IS NOT NULL.
    /// PROPOSITALMENTE um Guid, e NAO o Id/sequence local: identity e' RECICLAVEL (restore/recriacao do banco
    /// do no reinicia a sequence) e reusar a chave faria a central DESCARTAR op NOVA achando que e' duplicata.
    /// Null = op de no ANTIGO (pre-4b, nao manda a chave) -> sem dedup, comportamento de antes (nada descartado).
    /// </summary>
    public Guid? OpUid { get; set; }

    public DateTime CriadoEm { get; set; } = Helpers.DataHoraHelper.Agora();
    public bool Enviado { get; set; }
    public DateTime? EnviadoEm { get; set; }
    public string? Erro { get; set; }

    /// <summary>
    /// FASE 2 — numero de ENTREGA (so' no hub). Atribuido pelo publicador (nextval de
    /// seq_sync_entrega, sob advisory lock) SOMENTE a linhas ja' COMMITADAS — por isso o cursor do
    /// pull passou a ser SeqEntrega e o gap de visibilidade do Id morreu: linha que commita tarde
    /// pega um numero MAIOR na rodada seguinte (o Id e' alocado no INSERT, visivel so' no COMMIT —
    /// cursor por Id perdia essas ops pra sempre). Null = ainda nao numerada (invisivel pro pull)
    /// ou linha de no edge (edge nao numera: o push usa a flag Enviado, imune ao gap).
    /// </summary>
    public long? SeqEntrega { get; set; }
}
