namespace ZulexPharma.Application.DTOs.Fabricantes;

public class FabricanteListDto
{
    public long Id { get; set; }
    public string? Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public bool Ativo { get; set; }
    public decimal DescontoMinimo { get; set; }
    public decimal DescontoMaximo { get; set; }
    public decimal DescontoMaximoComSenha { get; set; }
}

public class FabricanteFormDto
{
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public decimal DescontoMinimo { get; set; }
    public decimal DescontoMaximo { get; set; }
    public decimal DescontoMaximoComSenha { get; set; }
}
