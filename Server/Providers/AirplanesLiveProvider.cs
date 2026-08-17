using System.Text.Json;
using ErzurumFlight.Server.Helpers;
using Microsoft.Extensions.Logging;

namespace ErzurumFlight.Server.Providers;

/// <summary>
/// Airplanes.Live ücretsiz ADS-B REST API'sini kullanan canlı takip sağlayıcısı.
/// Frontend bu sınıfı asla doğrudan çağırmaz; yalnızca LiveTrackingWorker (BackgroundService)
/// üzerinden, tek kontrollü akışla ve rate limit'e uygun şekilde kullanılır.
/// API dokümantasyonu: https://airplanes.live/api-guide/ (production öncesi güncel şartlar tekrar kontrol edilmeli).
/// API anahtarı gerektirmez; anahtar frontend'e asla gönderilmez.
/// </summary>
public class AirplanesLiveProvider : ILiveTrackingProvider
{
    public string SourceName => "Airplanes.Live";

    private const string BaseUrl = "https://api.airplanes.live/v2";

    private readonly HttpClient _httpClient;
    private readonly ILogger<AirplanesLiveProvider> _logger;

    public AirplanesLiveProvider(HttpClient httpClient, ILogger<AirplanesLiveProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LiveAircraftCandidate>> GetAircraftNearAsync(
        double latitude, double longitude, double radiusNm, CancellationToken ct = default)
    {
        // GET /v2/point/{lat}/{lon}/{radiusNm} — yalnızca "ac" alanını döndürür.
        var url = $"{BaseUrl}/point/{latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}/" +
                   $"{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}/" +
                   $"{radiusNm.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        try
        {
            using var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("ac", out var acArray) || acArray.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<LiveAircraftCandidate>();
            }

            var result = new List<LiveAircraftCandidate>();
            foreach (var ac in acArray.EnumerateArray())
            {
                var candidate = ParseAircraft(ac);
                if (candidate is not null)
                {
                    result.Add(candidate);
                }
            }

            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Airplanes.Live isteği başarısız oldu: {Url}", url);
            return Array.Empty<LiveAircraftCandidate>();
        }
    }

    private static LiveAircraftCandidate? ParseAircraft(JsonElement ac)
    {
        if (!ac.TryGetProperty("hex", out var hexEl) || hexEl.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var hex = hexEl.GetString();
        if (string.IsNullOrWhiteSpace(hex))
        {
            return null;
        }

        if (!ac.TryGetProperty("lat", out var latEl) || !ac.TryGetProperty("lon", out var lonEl))
        {
            // Konumu olmayan (yalnızca Mode-S) uçaklar canlı harita için kullanılamaz.
            return null;
        }

        var lat = latEl.ValueKind == JsonValueKind.Number ? latEl.GetDouble() : (double?)null;
        var lon = lonEl.ValueKind == JsonValueKind.Number ? lonEl.GetDouble() : (double?)null;
        if (lat is null || lon is null)
        {
            return null;
        }

        string? callsign = ac.TryGetProperty("flight", out var flightEl) && flightEl.ValueKind == JsonValueKind.String
            ? flightEl.GetString()?.Trim()
            : null;

        string? registration = ac.TryGetProperty("r", out var regEl) && regEl.ValueKind == JsonValueKind.String
            ? regEl.GetString()
            : null;

        double? heading = ac.TryGetProperty("track", out var trackEl) && trackEl.ValueKind == JsonValueKind.Number
            ? trackEl.GetDouble()
            : null;

        return new LiveAircraftCandidate(
            IcaoHex: hex.ToUpperInvariant(),
            Callsign: string.IsNullOrWhiteSpace(callsign) ? null : callsign,
            Registration: registration,
            Latitude: lat.Value,
            Longitude: lon.Value,
            Heading: heading,
            ObservedUtc: DateTime.UtcNow
        );
    }

    /// <summary>alt_baro alanı sayı veya "ground" string'i olabilir; güvenli şekilde ayrıştırır.</summary>
    public static double? ParseAltitude(JsonElement ac)
    {
        if (!ac.TryGetProperty("alt_baro", out var altEl))
        {
            return null;
        }

        if (altEl.ValueKind == JsonValueKind.Number)
        {
            return altEl.GetDouble();
        }

        if (altEl.ValueKind == JsonValueKind.String && altEl.GetString() == "ground")
        {
            return 0;
        }

        return null;
    }
}
