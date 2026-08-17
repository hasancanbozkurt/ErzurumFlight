using ErzurumFlight.Server.Helpers;

namespace ErzurumFlight.Tests;

public class AircraftMatcherTests
{
    // Erzurum Havalimanı referans koordinatları.
    private const double AirportLat = 39.9565;
    private const double AirportLon = 41.1702;

    [Fact]
    public void Match_ExactCallsignNearAirportAtScheduledTime_ReturnsHighConfidence()
    {
        var scheduledDeparture = new DateTime(2026, 8, 10, 9, 25, 0, DateTimeKind.Utc);

        var candidates = new List<LiveAircraftCandidate>
        {
            new("4BA9F1", "TK2705", "TC-JJA", AirportLat + 0.05, AirportLon + 0.05, 275, scheduledDeparture.AddMinutes(5))
        };

        var result = AircraftMatcher.Match("TK2705", "THY", scheduledDeparture, AirportLat, AirportLon, candidates);

        Assert.False(result.IsUnknown);
        Assert.NotNull(result.Candidate);
        Assert.Equal("4BA9F1", result.Candidate!.IcaoHex);
        Assert.True(result.Confidence >= AircraftMatcher.MinimumConfidence);
    }

    [Fact]
    public void Match_NoCandidates_ReturnsUnknown()
    {
        var result = AircraftMatcher.Match(
            "TK2705", "THY", DateTime.UtcNow, AirportLat, AirportLon,
            Array.Empty<LiveAircraftCandidate>());

        Assert.True(result.IsUnknown);
        Assert.Null(result.Candidate);
    }

    [Fact]
    public void Match_UnrelatedFarAwayCandidate_ReturnsUnknown()
    {
        // Callsign eşleşmiyor, zaman penceresi çok uzak, havaalanından çok uzak -> düşük skor.
        var candidates = new List<LiveAircraftCandidate>
        {
            new("999999", "XX9999", null, AirportLat + 20, AirportLon + 20, 90, DateTime.UtcNow.AddHours(10))
        };

        var result = AircraftMatcher.Match("TK2705", "THY", DateTime.UtcNow, AirportLat, AirportLon, candidates);

        Assert.True(result.IsUnknown);
    }

    [Fact]
    public void Match_TwoEquallyPlausibleCandidates_ReturnsUnknownRatherThanGuessing()
    {
        var scheduledDeparture = new DateTime(2026, 8, 10, 9, 25, 0, DateTimeKind.Utc);

        // Hiçbiri callsign ile eşleşmiyor, ikisi de benzer mesafe/zamanda -> belirsizlik, Unknown dönmeli.
        var candidates = new List<LiveAircraftCandidate>
        {
            new("AAAAAA", "ZZ1111", null, AirportLat + 0.1, AirportLon + 0.1, 100, scheduledDeparture.AddMinutes(10)),
            new("BBBBBB", "ZZ2222", null, AirportLat + 0.12, AirportLon + 0.11, 105, scheduledDeparture.AddMinutes(12))
        };

        var result = AircraftMatcher.Match("TK2705", "THY", scheduledDeparture, AirportLat, AirportLon, candidates);

        Assert.True(result.IsUnknown);
    }

    [Fact]
    public void HaversineNm_SamePoint_ReturnsZero()
    {
        var distance = AircraftMatcher.HaversineNm(AirportLat, AirportLon, AirportLat, AirportLon);
        Assert.Equal(0, distance, precision: 6);
    }

    [Fact]
    public void HaversineNm_KnownDistance_IsReasonablyAccurate()
    {
        // Erzurum (ERZ) - İstanbul (IST) büyük daire mesafesi ~571 deniz mili civarındadır.
        const double istanbulLat = 41.2753;
        const double istanbulLon = 28.7519;

        var distance = AircraftMatcher.HaversineNm(AirportLat, AirportLon, istanbulLat, istanbulLon);

        Assert.InRange(distance, 550, 590);
    }
}
