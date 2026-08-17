using ErzurumFlight.Server.Helpers;
using ErzurumFlight.Server.Models;

namespace ErzurumFlight.Server.Services;

/// <summary>Bir FlightInstance için hesaplanan eşleştirme sonucu.</summary>
public record FlightMatchOutcome(int FlightInstanceId, MatchResult Result);

/// <summary>
/// AircraftMatcher (saf mantık) ile izlenmesi gereken uçuşları canlı ADS-B adaylarıyla eşleştiren servis.
/// Birden fazla aday varsa veya güven düşükse yanlış uçağı göstermek yerine Unknown döner (AircraftMatcher.Match).
/// </summary>
public interface IAircraftMatchingService
{
    IReadOnlyList<FlightMatchOutcome> MatchAll(
        IReadOnlyList<(FlightInstance Flight, string? AirlineCallsignPrefix)> monitoredFlights,
        IReadOnlyList<LiveAircraftCandidate> liveCandidates,
        double airportLat,
        double airportLon);
}

public class AircraftMatchingService : IAircraftMatchingService
{
    public IReadOnlyList<FlightMatchOutcome> MatchAll(
        IReadOnlyList<(FlightInstance Flight, string? AirlineCallsignPrefix)> monitoredFlights,
        IReadOnlyList<LiveAircraftCandidate> liveCandidates,
        double airportLat,
        double airportLon)
    {
        var results = new List<FlightMatchOutcome>();
        var usedHexes = new HashSet<string>();

        // Zaman penceresine göre önce en yakın kalkışlı uçuşları eşleştir; bir uçak yalnızca bir uçuşa atanabilir.
        foreach (var (flight, callsignPrefix) in monitoredFlights.OrderBy(f =>
                     Math.Abs((f.Flight.ScheduledDepartureUtc - DateTime.UtcNow).TotalMinutes)))
        {
            var availableCandidates = liveCandidates.Where(c => !usedHexes.Contains(c.IcaoHex)).ToList();

            var match = AircraftMatcher.Match(
                flight.FlightNumber,
                callsignPrefix,
                flight.ScheduledDepartureUtc,
                airportLat,
                airportLon,
                availableCandidates);

            if (!match.IsUnknown && match.Candidate is not null)
            {
                usedHexes.Add(match.Candidate.IcaoHex);
            }

            results.Add(new FlightMatchOutcome(flight.Id, match));
        }

        return results;
    }
}
