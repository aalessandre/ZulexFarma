namespace ZulexPharma.Application.DTOs.Entregas;

public class EntregaPerfilDto
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public List<EntregaFaixaDto> Faixas { get; set; } = new();
}

public class EntregaPerfilFormDto
{
    public long FilialId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public List<EntregaFaixaFormDto> Faixas { get; set; } = new();
}

public class EntregaFaixaDto
{
    public long Id { get; set; }
    public long PerfilId { get; set; }
    public decimal RaioMaxKm { get; set; }
    public decimal Valor { get; set; }
    public int Ordem { get; set; }
}

public class EntregaFaixaFormDto
{
    public long? Id { get; set; }
    public decimal RaioMaxKm { get; set; }
    public decimal Valor { get; set; }
    public int Ordem { get; set; }
}
