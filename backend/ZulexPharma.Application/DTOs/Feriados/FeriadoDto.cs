using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.Feriados;

public class FeriadoDto
{
    public long Id { get; set; }
    public DateOnly Data { get; set; }
    public string Nome { get; set; } = string.Empty;
    public AmbitoFeriado Ambito { get; set; }
    public string? Uf { get; set; }
    public long? FilialId { get; set; }
    public string? FilialNome { get; set; }
    public OrigemFeriado Origem { get; set; }
    public bool Ativo { get; set; }
}

public class FeriadoFormDto
{
    public DateOnly Data { get; set; }
    public string Nome { get; set; } = string.Empty;
    public AmbitoFeriado Ambito { get; set; }
    public string? Uf { get; set; }
    public long? FilialId { get; set; }
    public bool Ativo { get; set; } = true;
}

public class FeriadoImportResultDto
{
    public int Importados { get; set; }
    public int JaExistentes { get; set; }
    public List<string> Nomes { get; set; } = new();
}
