using ErzurumFlight.Server.Models;

namespace ErzurumFlight.Server.Helpers;

/// <summary>
/// AeroDataBox'ın serbest metin durum alanını (örn. "Expected", "Canceled", "EnRoute", "Landed")
/// uygulamanın kendi FlightStatus enum'una eşler. Sağlayıcı sürümleri arası küçük yazım
/// farklılıklarına dayanıklı olması için büyük/küçük harf duyarsız alt-dize eşleşmesi kullanılır;
/// tam enum eşleşmesine güvenilmez.
/// </summary>
public static class FlightStatusMapper
{
    public static FlightStatus Map(string? rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            return FlightStatus.Unknown;
        }

        var s = rawStatus.Trim().ToLowerInvariant();

        if (s.Contains("cancel")) return FlightStatus.Cancelled;
        if (s.Contains("divert")) return FlightStatus.Diverted;
        if (s.Contains("land") || s.Contains("arrived")) return FlightStatus.Landed;
        if (s.Contains("depart") || s.Contains("airborne") || s.Contains("enroute") || s.Contains("en-route") || s.Contains("en route")) return FlightStatus.Departed;
        if (s.Contains("delay")) return FlightStatus.Delayed;
        if (s.Contains("board") || s.Contains("checkin") || s.Contains("check-in") || s.Contains("gate")) return FlightStatus.Monitoring;
        if (s.Contains("expect") || s.Contains("schedul")) return FlightStatus.Scheduled;

        return FlightStatus.Unknown;
    }
}
