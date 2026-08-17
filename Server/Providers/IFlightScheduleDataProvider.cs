using ErzurumFlight.Server.Models;

namespace ErzurumFlight.Server.Providers;

/// <summary>
/// Dış API'den (AeroDataBox) gelen, normalize edilmiş tek bir uçuş kaydı.
/// "Diğer havalimanı" (OtherAirport*) alanları, sorgulanan havalimanına göre değişir:
/// Direction=Departure ise varış havalimanıdır, Direction=Arrival ise kalkış havalimanıdır.
/// </summary>
public record AirportFlightRecord(
    string FlightNumber,
    string? AirlineName,
    string? AirlineIata,
    string? AirlineIcao,
    string? OtherAirportIata,
    string? OtherAirportIcao,
    string? OtherAirportName,
    double? OtherAirportLat,
    double? OtherAirportLon,
    string? OtherAirportTimezone,
    FlightDirection Direction,
    DateTime ScheduledUtc,
    DateTime? RevisedUtc,
    DateTime? ActualUtc,
    string RawStatus,
    string? AircraftModel,
    string? AircraftRegistration
);

/// <summary>
/// Gerçek, dış bir kaynaktan (AeroDataBox — resmi/yarı-resmi uçuş verisi agregatörü) belirli bir
/// havalimanının kalkış/varış listesini (FIDS) çeken sağlayıcıları soyutlar. Bu arayüzü uygulayan
/// sınıflar GERÇEK bir API anahtarı gerektirir — anahtarsız, sahte olmayan, resmi bir ücretsiz
/// kaynak yoktur. Frontend bu sağlayıcıları asla doğrudan çağırmaz; yalnızca ScheduleSyncWorker
/// üzerinden, tek kontrollü ve rate-limit'e uygun bir akışla kullanılır.
/// </summary>
public interface IFlightScheduleDataProvider
{
    string SourceName { get; }

    /// <summary>
    /// Belirtilen havalimanının, [fromLocal, toLocal] yerel zaman aralığındaki (havalimanının
    /// kendi saat diliminde) kalkış ve varışlarını döndürür. Sağlayıcının tek istekte
    /// destekleyebileceği maksimum aralık planına göre değişir (bkz. appsettings "FlightData:WindowHours").
    /// </summary>
    Task<IReadOnlyList<AirportFlightRecord>> GetAirportFlightsAsync(
        string airportIcao, string airportIata, DateTime fromLocal, DateTime toLocal, CancellationToken ct = default);
}
