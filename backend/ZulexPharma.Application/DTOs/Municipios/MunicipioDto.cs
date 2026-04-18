namespace ZulexPharma.Application.DTOs.Municipios;

public class MunicipioDto
{
    public long Id { get; set; }
    public string CodigoIbge { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
}
