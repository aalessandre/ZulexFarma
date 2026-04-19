using ZulexPharma.Application.DTOs.Entregas;

namespace ZulexPharma.Application.Interfaces;

/// <summary>
/// Converte endereço textual em coordenadas (lat/lng). MVP usa Nominatim OSM.
/// Rate limit: 1 req/seg — chamadas concorrentes serializadas internamente.
/// </summary>
public interface IGeocodingService
{
    Task<GeocodingResultDto> GeocodificarAsync(GeocodingRequestDto request, CancellationToken ct = default);
}
