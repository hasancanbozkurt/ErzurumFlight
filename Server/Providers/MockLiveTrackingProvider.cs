using ErzurumFlight.Server.Helpers;

namespace ErzurumFlight.Server.Providers;

/// <summary>
/// Geliştirme ve test ortamı için sahte ADS-B verisi üreten sağlayıcı.
/// Üretilen veriler asla production'da gerçek uçuş gibi gösterilmez;
/// yalnızca appsettings.Development.json'da "LiveTracking:UseMockProvider": true iken devrededir.
/// </summary>
public class MockLiveTrackingProvider : ILiveTrackingProvider
{
    public string SourceName => "MockLiveTrackingProvider (TEST)";

    public Task<IReadOnlyList<LiveAircraftCandidate>> GetAircraftNearAsync(
        double latitude, double longitude, double radiusNm, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var random = new Random(now.Minute); // Deterministik olmayan ama tekrarlanabilir demo hareket.

        IReadOnlyList<LiveAircraftCandidate> result = new List<LiveAircraftCandidate>
        {
            new(
                IcaoHex: "TEST01",
                Callsign: "TK2705",
                Registration: "TC-TEST",
                Latitude: latitude + 0.15 + random.NextDouble() * 0.02,
                Longitude: longitude + 0.20 + random.NextDouble() * 0.02,
                Heading: 275,
                ObservedUtc: now
            ),
            new(
                IcaoHex: "TEST02",
                Callsign: "PC1234",
                Registration: "TC-TEST2",
                Latitude: latitude - 0.30,
                Longitude: longitude - 0.10,
                Heading: 95,
                ObservedUtc: now
            )
        };

        return Task.FromResult(result);
    }
}
