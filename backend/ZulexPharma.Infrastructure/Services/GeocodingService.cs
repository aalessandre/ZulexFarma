using System.Globalization;
using System.Text.Json;
using Serilog;
using ZulexPharma.Application.DTOs.Entregas;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Implementação com Nominatim (OpenStreetMap) — grátis, sem chave de API.
/// Termos de uso: máximo 1 requisição/segundo + User-Agent identificando a aplicação.
/// Ver: https://operations.osmfoundation.org/policies/nominatim/
/// </summary>
public class GeocodingService : IGeocodingService
{
    private const string BaseUrl = "https://nominatim.openstreetmap.org/search";
    private const string UserAgent = "ZulexPharma-ERP/1.0 (contato-tecnico@zulexpharma.com)";

    private readonly IHttpClientFactory _httpFactory;
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static DateTime _ultimaChamada = DateTime.MinValue;

    public GeocodingService(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<GeocodingResultDto> GeocodificarAsync(GeocodingRequestDto request, CancellationToken ct = default)
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Rua))
        {
            var rua = request.Rua.Trim();
            if (!string.IsNullOrWhiteSpace(request.Numero)) rua += " " + request.Numero.Trim();
            partes.Add(rua);
        }
        if (!string.IsNullOrWhiteSpace(request.Bairro)) partes.Add(request.Bairro.Trim());
        if (!string.IsNullOrWhiteSpace(request.Cidade)) partes.Add(request.Cidade.Trim());
        if (!string.IsNullOrWhiteSpace(request.Uf)) partes.Add(request.Uf.Trim());
        if (!string.IsNullOrWhiteSpace(request.Cep)) partes.Add(request.Cep.Trim());

        if (partes.Count == 0)
            return new GeocodingResultDto { Encontrado = false, Mensagem = "Endereço vazio." };

        var query = string.Join(", ", partes);
        var url = $"{BaseUrl}?format=json&countrycodes=br&limit=1&addressdetails=0&q={Uri.EscapeDataString(query)}";

        await AguardarRateLimitAsync(ct);

        try
        {
            using var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            client.DefaultRequestHeaders.Add("Accept-Language", "pt-BR");
            client.Timeout = TimeSpan.FromSeconds(15);

            var resp = await client.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                Log.Warning("Nominatim {Status}: {Query}", resp.StatusCode, query);
                return new GeocodingResultDto { Encontrado = false, Mensagem = $"Nominatim retornou {resp.StatusCode}." };
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            {
                return new GeocodingResultDto { Encontrado = false, Mensagem = "Endereço não encontrado. Ajuste os dados ou informe coordenadas manualmente." };
            }

            var primeiro = doc.RootElement[0];
            if (!primeiro.TryGetProperty("lat", out var latEl) || !primeiro.TryGetProperty("lon", out var lonEl))
                return new GeocodingResultDto { Encontrado = false, Mensagem = "Resposta Nominatim sem coordenadas." };

            var latStr = latEl.GetString();
            var lonStr = lonEl.GetString();
            if (!decimal.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat)
             || !decimal.TryParse(lonStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                return new GeocodingResultDto { Encontrado = false, Mensagem = "Coordenadas Nominatim em formato inesperado." };

            var displayName = primeiro.TryGetProperty("display_name", out var dn) ? dn.GetString() : null;
            return new GeocodingResultDto
            {
                Encontrado = true,
                Latitude = Math.Round(lat, 7),
                Longitude = Math.Round(lon, 7),
                EnderecoEncontrado = displayName
            };
        }
        catch (TaskCanceledException)
        {
            return new GeocodingResultDto { Encontrado = false, Mensagem = "Timeout na consulta ao Nominatim." };
        }
    }

    /// <summary>Garante 1 segundo entre chamadas (termos de uso Nominatim).</summary>
    private static async Task AguardarRateLimitAsync(CancellationToken ct)
    {
        await _rateLimiter.WaitAsync(ct);
        try
        {
            var desde = DateTime.UtcNow - _ultimaChamada;
            var espera = TimeSpan.FromMilliseconds(1100) - desde;
            if (espera > TimeSpan.Zero) await Task.Delay(espera, ct);
            _ultimaChamada = DateTime.UtcNow;
        }
        finally { _rateLimiter.Release(); }
    }
}
