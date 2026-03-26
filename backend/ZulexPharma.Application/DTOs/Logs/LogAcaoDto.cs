namespace ZulexPharma.Application.DTOs.Logs;

public class LogAcaoListDto
{
    public long Id { get; set; }
    public DateTime RealizadoEm { get; set; }
    public string Acao { get; set; } = string.Empty;       // CRIAÇÃO | ALTERAÇÃO | EXCLUSÃO
    public string NomeUsuario { get; set; } = string.Empty;
    public List<LogCampoDto> Campos { get; set; } = new();
}

public class LogCampoDto
{
    public string Campo { get; set; } = string.Empty;
    public string? ValorAnterior { get; set; }
    public string? ValorAtual { get; set; }
}
