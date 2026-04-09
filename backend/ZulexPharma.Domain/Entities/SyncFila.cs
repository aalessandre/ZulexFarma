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
    public long FilialOrigemId { get; set; }
    public DateTime CriadoEm { get; set; } = Helpers.DataHoraHelper.Agora();
    public bool Enviado { get; set; }
    public DateTime? EnviadoEm { get; set; }
    public string? Erro { get; set; }
}
