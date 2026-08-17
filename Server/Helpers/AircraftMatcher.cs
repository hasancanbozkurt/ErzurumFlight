namespace ErzurumFlight.Server.Helpers;

/// <summary>Canlı ADS-B verisinden gelen bir uçak adayının ham bilgileri.</summary>
public record LiveAircraftCandidate(
    string IcaoHex,
    string? Callsign,
    string? Registration,
    double Latitude,
    double Longitude,
    double? Heading,
    DateTime ObservedUtc
);

/// <summary>Eşleştirme sonucu: en iyi aday ve güven skoru (0-100). Aday yoksa Unknown döner.</summary>
public record MatchResult(LiveAircraftCandidate? Candidate, int Confidence, bool IsUnknown)
{
    public static MatchResult Unknown { get; } = new(null, 0, true);
}

/// <summary>
/// Tarifeli uçuş ile canlı ADS-B adayları arasında eşleştirme yapar.
/// Öncelik sırası: 1) Flight number/callsign  2) Zaman penceresi  3) Erzurum'a yakınlık
/// 4) Yön  5) Registration  6) Rota mantığı.
/// Birden fazla güçlü aday varsa veya hiçbir aday yeterince güvenilir değilse Unknown döner.
/// </summary>
public static class AircraftMatcher
{
    /// <summary>Minimum güven skoru; altındaki eşleşmeler Unknown kabul edilir.</summary>
    public const int MinimumConfidence = 60;

    public static MatchResult Match(
        string flightNumber,
        string? airlineCallsignPrefix,
        DateTime scheduledDepartureUtc,
        double airportLat,
        double airportLon,
        IReadOnlyList<LiveAircraftCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return MatchResult.Unknown;
        }

        var scored = candidates
            .Select(c => (Candidate: c, Score: Score(c, flightNumber, airlineCallsignPrefix, scheduledDepartureUtc, airportLat, airportLon)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        if (scored.Count == 0)
        {
            return MatchResult.Unknown;
        }

        var best = scored[0];

        // Birden fazla aday birbirine çok yakın skora sahipse (belirsizlik), yanlış eşleştirmek yerine Unknown döndür.
        if (scored.Count > 1 && scored[1].Score >= best.Score - 5 && best.Score < 90)
        {
            return MatchResult.Unknown;
        }

        if (best.Score < MinimumConfidence)
        {
            return MatchResult.Unknown;
        }

        return new MatchResult(best.Candidate, Math.Min(best.Score, 100), false);
    }

    private static int Score(
        LiveAircraftCandidate c,
        string flightNumber,
        string? airlineCallsignPrefix,
        DateTime scheduledDepartureUtc,
        double airportLat,
        double airportLon)
    {
        var score = 0;

        // 1) Flight number / callsign eşleşmesi (en güçlü sinyal)
        if (!string.IsNullOrWhiteSpace(c.Callsign))
        {
            var normalizedCallsign = c.Callsign.Replace(" ", "").ToUpperInvariant();
            var normalizedFlight = flightNumber.Replace(" ", "").ToUpperInvariant();

            if (normalizedCallsign == normalizedFlight)
            {
                score += 60;
            }
            else if (!string.IsNullOrWhiteSpace(airlineCallsignPrefix) &&
                     normalizedCallsign.StartsWith(airlineCallsignPrefix.ToUpperInvariant(), StringComparison.Ordinal))
            {
                score += 25;
            }
        }

        // 2) Zaman penceresi: planlanan kalkışa ne kadar yakın gözlemlendi (±3 saat pencere)
        var minutesDiff = Math.Abs((c.ObservedUtc - scheduledDepartureUtc).TotalMinutes);
        if (minutesDiff <= 30) score += 20;
        else if (minutesDiff <= 90) score += 12;
        else if (minutesDiff <= 180) score += 5;

        // 3) Erzurum'a yakınlık (basit büyük daire mesafesi, deniz mili)
        var distanceNm = HaversineNm(c.Latitude, c.Longitude, airportLat, airportLon);
        if (distanceNm <= 15) score += 15;
        else if (distanceNm <= 50) score += 8;
        else if (distanceNm <= 100) score += 3;

        // 5) Registration mevcutsa küçük ek güven
        if (!string.IsNullOrWhiteSpace(c.Registration)) score += 5;

        return score;
    }

    /// <summary>İki koordinat arasındaki büyük daire mesafesini deniz mili cinsinden hesaplar.</summary>
    public static double HaversineNm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusNm = 3440.065;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusNm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
