namespace ZulexPharma.Application.DTOs.Entregas;

public class GeocodingRequestDto
{
    public string Rua { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string? Cep { get; set; }
}

public class GeocodingResultDto
{
    public bool Encontrado { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? EnderecoEncontrado { get; set; }
    public string? Mensagem { get; set; }
}
