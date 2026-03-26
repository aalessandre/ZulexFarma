namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Controla o estado de sincronização entre filial local e servidor central.
/// </summary>
public class SyncControle
{
    public long Id { get; set; }

    /// <summary>Id da filial que está sincronizando.</summary>
    public long FilialId { get; set; }

    /// <summary>Nome da tabela/entidade.</summary>
    public string Tabela { get; set; } = string.Empty;

    /// <summary>Última VersaoSync recebida do servidor central.</summary>
    public long UltimaVersaoRecebida { get; set; } = 0;

    /// <summary>Última VersaoSync enviada para o servidor central.</summary>
    public long UltimaVersaoEnviada { get; set; } = 0;

    /// <summary>Timestamp da última sincronização bem-sucedida.</summary>
    public DateTime? UltimoSync { get; set; }

    /// <summary>Status: OK, ERRO, PENDENTE.</summary>
    public string Status { get; set; } = "PENDENTE";

    /// <summary>Mensagem de erro, se houver.</summary>
    public string? MensagemErro { get; set; }
}
