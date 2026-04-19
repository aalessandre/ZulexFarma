namespace ZulexPharma.Application.DTOs.Entregas;

public class EntregaFaixaDto
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public decimal RaioMaxKm { get; set; }
    public decimal Valor { get; set; }
    public int Ordem { get; set; }
}

public class EntregaFaixaFormDto
{
    public long FilialId { get; set; }
    public decimal RaioMaxKm { get; set; }
    public decimal Valor { get; set; }
    public int Ordem { get; set; }
}
