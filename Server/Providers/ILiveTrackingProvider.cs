using ErzurumFlight.Server.Helpers;

namespace ErzurumFlight.Server.Providers;

/// <summary>
/// Canlı ADS-B veri kaynağını soyutlayan arayüz. Frontend bu sağlayıcıyı asla doğrudan çağırmaz;
/// yalnızca BackgroundService (LiveTrackingWorker) üzerinden, tek kontrollü akışla kullanılır.
/// </summary>
public interface ILiveTrackingProvider
{
    string SourceName { get; }

    /// <summary>
    /// Verilen merkez koordinat etrafında, belirtilen yarıçap (deniz mili) içindeki uçakları döndürür.
    /// </summary>
    Task<IReadOnlyList<LiveAircraftCandidate>> GetAircraftNearAsync(
        double latitude, double longitude, double radiusNm, CancellationToken ct = default);
}
