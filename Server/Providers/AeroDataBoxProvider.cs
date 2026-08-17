using System.Globalization;
using System.Text.Json;
using ErzurumFlight.Server.Models;

namespace ErzurumFlight.Server.Providers;

/// <summary>
/// AeroDataBox FIDS (Flight Information Display System) API'sini kullanan gerçek veri sağlayıcısı.
/// Resmi/yarı-resmi kaynaklardan beslenir; kalkış-varış saatlerini, gecikme/iptal durumunu içerir.
///
/// Kurulum: https://rapidapi.com/aedbx-aedbx/api/aerodatabox üzerinden ücretsiz bir plana kaydolup
/// bir API anahtarı almanız gerekir (bkz. README "Gerçek uçuş verisi kurulumu"). Anahtar asla
/// frontend'e gönderilmez; yalnızca bu sınıf üzerinden, sunucu tarafında kullanılır.
///
/// Uç nokta: GET https://aerodatabox.p.rapidapi.com/flights/airports/icao/{icao}/{fromLocal}/{toLocal}
/// Dokümantasyon: https://doc.aerodatabox.com/#operation/GetAirportFlights
///
/// ÖNEMLİ: Bu entegrasyon resmi AeroDataBox dokümantasyonuna göre yazılmıştır ancak bu proje
/// gerçek bir API anahtarıyla test edilememiştir (geliştirme ortamının ağ erişimi kısıtlı).
/// Alan ayrıştırma (ParseFlight) kasıtlı olarak savunmacı yazılmıştır: eksik/beklenmeyen bir alan
/// tüm senkronizasyonu düşürmez, yalnızca o tek uçuş kaydı atlanır ve loglanır. İlk kurulumda
/// loglarda "AeroDataBox uçuş ayrıştırma hatası" uyarısı görürseniz, RapidAPI konsolundaki
/// "Test Endpoint" ile gerçek yanıtı inceleyip bu dosyadaki alan adlarını güncelleyin.
/// </summary>
public class AeroDataBoxProvider : IFlightScheduleDataProvider
{
    public string SourceName => "AeroDataBox";

    private const string Host = "aerodatabox.p.rapidapi.com";

    private readonly HttpClient _httpClient;
    private readonly ILogger<AeroDataBoxProvider> _logger;

    public AeroDataBoxProvider(HttpClient httpClient, ILogger<AeroDataBoxProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AirportFlightRecord>> GetAirportFlightsAsync(
        string airportIcao, string airportIata, DateTime fromLocal, DateTime toLocal, CancellationToken ct = default)
    {
        // AeroDataBox API requires colon (:) in timestamps to be URL encoded (%3A)
        var fromStr = Uri.EscapeDataString(fromLocal.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture));
        var toStr = Uri.EscapeDataString(toLocal.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture));

        var url = $"https://{Host}/flights/airports/icao/{airportIcao}/{fromStr}/{toStr}" +
                   "?withLeg=true&withCancelled=true&withCodeshared=true&withCargo=false&withPrivate=false&withLocation=false";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // X-RapidAPI-Key / X-RapidAPI-Host başlıkları Program.cs'de AddHttpClient ile varsayılan olarak eklenir.

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"AeroDataBox isteği başarısız: {(int)response.StatusCode} {response.ReasonPhrase} — {Truncate(errorBody, 300)}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var result = new List<AirportFlightRecord>();
        ParseSection(doc.RootElement, "departures", FlightDirection.Departure, result);
        ParseSection(doc.RootElement, "arrivals", FlightDirection.Arrival, result);
        return result;
    }

    private void ParseSection(JsonElement root, string propertyName, FlightDirection direction, List<AirportFlightRecord> result)
    {
        if (!root.TryGetProperty(propertyName, out var arrayEl) || arrayEl.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in arrayEl.EnumerateArray())
        {
            try
            {
                var record = ParseFlight(item, direction);
                if (record is not null)
                {
                    result.Add(record);
                }
            }
            catch (Exception ex)
            {
                // Tek bir kaydın ayrıştırılamaması tüm senkronizasyonu düşürmemeli.
                _logger.LogWarning(ex, "AeroDataBox uçuş ayrıştırma hatası, bu kayıt atlanıyor.");
            }
        }
    }

    private static AirportFlightRecord? ParseFlight(JsonElement item, FlightDirection direction)
    {
        var flightNumber = GetString(item, "number");
        if (string.IsNullOrWhiteSpace(flightNumber))
        {
            return null;
        }
        flightNumber = flightNumber.Replace(" ", "").ToUpperInvariant();

        string? airlineName = null, airlineIata = null, airlineIcao = null;
        if (item.TryGetProperty("airline", out var airlineEl))
        {
            airlineName = GetString(airlineEl, "name");
            airlineIata = GetString(airlineEl, "iata");
            airlineIcao = GetString(airlineEl, "icao");
        }

        if (!item.TryGetProperty("departure", out var depEl) || !item.TryGetProperty("arrival", out var arrEl))
        {
            return null;
        }

        var depScheduledUtc = GetUtcTime(depEl, "scheduledTime");
        var arrScheduledUtc = GetUtcTime(arrEl, "scheduledTime");
        if (depScheduledUtc is null || arrScheduledUtc is null)
        {
            return null;
        }

        var depRevisedUtc = GetUtcTime(depEl, "revisedTime") ?? GetUtcTime(depEl, "predictedTime");
        var arrRevisedUtc = GetUtcTime(arrEl, "revisedTime") ?? GetUtcTime(arrEl, "predictedTime");
        var depActualUtc = GetUtcTime(depEl, "actualTime") ?? GetUtcTime(depEl, "runwayTime");
        var arrActualUtc = GetUtcTime(arrEl, "actualTime") ?? GetUtcTime(arrEl, "runwayTime");

        var rawStatus = GetString(item, "status") ?? "Unknown";

        string? aircraftModel = null, aircraftReg = null;
        if (item.TryGetProperty("aircraft", out var acEl))
        {
            aircraftModel = GetString(acEl, "model");
            aircraftReg = GetString(acEl, "reg");
        }

        // "Diğer havalimanı": kalkış listesindeysek varış havalimanı, varış listesindeysek kalkış havalimanı.
        var otherAirportEl = direction == FlightDirection.Departure ? arrEl : depEl;
        JsonElement? otherAirport = otherAirportEl.TryGetProperty("airport", out var apEl) ? apEl : null;

        var scheduledUtc = direction == FlightDirection.Departure ? depScheduledUtc.Value : arrScheduledUtc.Value;
        var revisedUtc = direction == FlightDirection.Departure ? depRevisedUtc : arrRevisedUtc;
        var actualUtc = direction == FlightDirection.Departure ? depActualUtc : arrActualUtc;

        return new AirportFlightRecord(
            FlightNumber: flightNumber,
            AirlineName: airlineName,
            AirlineIata: airlineIata,
            AirlineIcao: airlineIcao,
            OtherAirportIata: otherAirport is { } oa1 ? GetString(oa1, "iata") : null,
            OtherAirportIcao: otherAirport is { } oa2 ? GetString(oa2, "icao") : null,
            OtherAirportName: otherAirport is { } oa3 ? GetString(oa3, "name") : null,
            OtherAirportLat: otherAirport is { } oa4 && oa4.TryGetProperty("location", out var locEl) && locEl.TryGetProperty("lat", out var latEl) && latEl.ValueKind == JsonValueKind.Number ? latEl.GetDouble() : null,
            OtherAirportLon: otherAirport is { } oa5 && oa5.TryGetProperty("location", out var lonParentEl) && lonParentEl.TryGetProperty("lon", out var lonEl) && lonEl.ValueKind == JsonValueKind.Number ? lonEl.GetDouble() : null,
            OtherAirportTimezone: otherAirport is { } oa6 ? GetString(oa6, "timeZone") : null,
            Direction: direction,
            ScheduledUtc: scheduledUtc,
            RevisedUtc: revisedUtc,
            ActualUtc: actualUtc,
            RawStatus: rawStatus,
            AircraftModel: aircraftModel,
            AircraftRegistration: aircraftReg
        );
    }

    private static DateTime? GetUtcTime(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var timeEl))
        {
            return null;
        }

        // AeroDataBox zaman alanları genelde {"utc": "...", "local": "..."} şeklindedir.
        string? utcString = timeEl.ValueKind == JsonValueKind.Object && timeEl.TryGetProperty("utc", out var utcEl)
            ? utcEl.GetString()
            : timeEl.ValueKind == JsonValueKind.String ? timeEl.GetString() : null;

        if (string.IsNullOrWhiteSpace(utcString))
        {
            return null;
        }

        // Bazı sürümler "2026-08-13 09:00Z" gibi boşluklu bir biçim döndürebilir.
        utcString = utcString.Replace(" ", "T");
        if (!utcString.EndsWith("Z", StringComparison.Ordinal) && !utcString.Contains('+'))
        {
            utcString += "Z";
        }

        return DateTimeOffset.TryParse(utcString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto)
            ? dto.UtcDateTime
            : null;
    }

    private static string? GetString(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
