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

    public DateTime CriadoEm { get; set; } = Helpers.DataHoraHelper.Agora();
    public bool Enviado { get; set; }
    public DateTime? EnviadoEm { get; set; }
    public string? Erro { get; set; }
}
