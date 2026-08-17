using ErzurumFlight.Server.Data;
using ErzurumFlight.Server.DTOs;
using ErzurumFlight.Server.Helpers;
using ErzurumFlight.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ErzurumFlight.Server.Services;

/// <summary>
/// Erzurum Havalimanı için uçuş sorgulama servisi. Tüm okuma uçları (dashboard, giden/gelen,
/// tarih aralığı, detay) bu servis üzerinden çalışır.
/// </summary>
public interface IFlightService
{
    Task<IReadOnlyList<FlightSummaryDto>> GetFlightsAsync(DateOnly date, FlightDirection direction, CancellationToken ct = default);
    Task<IReadOnlyList<FlightSummaryDto>> GetUpcomingAsync(int days, CancellationToken ct = default);
    Task<FlightDetailDto?> GetFlightDetailAsync(int flightInstanceId, CancellationToken ct = default);
    Task<DailyFlightCountsDto> GetDailyCountsAsync(DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<CalendarDayDto>> GetCalendarAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}

public class FlightService : IFlightService
{
    private readonly FlightDbContext _db;
    private readonly IConfiguration _config;

    public FlightService(FlightDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    private string ErzurumIata => _config["Airport:Iata"] ?? SeedData.ErzurumIata;

    public async Task<IReadOnlyList<FlightSummaryDto>> GetFlightsAsync(DateOnly date, FlightDirection direction, CancellationToken ct = default)
    {
        var erzurum = await _db.Airports.FirstOrDefaultAsync(a => a.IataCode == ErzurumIata, ct);
        if (erzurum is null)
        {
            return Array.Empty<FlightSummaryDto>();
        }

        var query = _db.FlightInstances
            .Include(i => i.OriginAirport)
            .Include(i => i.DestinationAirport)
            .Include(i => i.FlightSchedule).ThenInclude(s => s!.Airline)
            .Include(i => i.Airline)
            .Include(i => i.Operation)
            .Where(i => i.FlightDate == date);

        query = direction == FlightDirection.Departure
            ? query.Where(i => i.OriginAirportId == erzurum.Id)
            : query.Where(i => i.DestinationAirportId == erzurum.Id);

        var flights = await query.OrderBy(i => i.ScheduledDepartureUtc).ToListAsync(ct);

        return flights.Select(ToSummaryDto).ToList();
    }

    public async Task<IReadOnlyList<FlightSummaryDto>> GetUpcomingAsync(int days, CancellationToken ct = default)
    {
        var erzurum = await _db.Airports.FirstOrDefaultAsync(a => a.IataCode == ErzurumIata, ct);
        if (erzurum is null)
        {
            return Array.Empty<FlightSummaryDto>();
        }

        var today = TimeZoneHelper.TodayInIstanbul();
        var endDate = today.AddDays(Math.Max(days - 1, 0));

        var flights = await _db.FlightInstances
            .Include(i => i.OriginAirport)
            .Include(i => i.DestinationAirport)
            .Include(i => i.FlightSchedule).ThenInclude(s => s!.Airline)
            .Include(i => i.Airline)
            .Include(i => i.Operation)
            .Where(i => i.FlightDate >= today && i.FlightDate <= endDate)
            .Where(i => i.OriginAirportId == erzurum.Id || i.DestinationAirportId == erzurum.Id)
            .OrderBy(i => i.ScheduledDepartureUtc)
            .ToListAsync(ct);

        return flights.Select(ToSummaryDto).ToList();
    }

    public async Task<FlightDetailDto?> GetFlightDetailAsync(int flightInstanceId, CancellationToken ct = default)
    {
        var flight = await _db.FlightInstances
            .Include(i => i.OriginAirport)
            .Include(i => i.DestinationAirport)
            .Include(i => i.FlightSchedule).ThenInclude(s => s!.Airline)
            .Include(i => i.Airline)
            .Include(i => i.Source)
            .Include(i => i.Operation).ThenInclude(o => o!.Aircraft)
            .FirstOrDefaultAsync(i => i.Id == flightInstanceId, ct);

        if (flight is null)
        {
            return null;
        }

        var hasLive = flight.Operation is not null &&
                      flight.Status is FlightStatus.Monitoring or FlightStatus.AircraftDetected or
                          FlightStatus.Taxiing or FlightStatus.Departed or FlightStatus.Airborne or
                          FlightStatus.Approaching or FlightStatus.NearAirport;

        return new FlightDetailDto(
            Id: flight.Id,
            FlightNumber: flight.FlightNumber,
            AirlineName: flight.Airline?.Name ?? flight.FlightSchedule?.Airline?.Name,
            OriginIata: flight.OriginAirport?.IataCode ?? "",
            OriginName: flight.OriginAirport?.Name ?? "",
            DestinationIata: flight.DestinationAirport?.IataCode ?? "",
            DestinationName: flight.DestinationAirport?.Name ?? "",
            AircraftType: flight.Operation?.Aircraft?.AircraftType,
            Registration: flight.Operation?.Aircraft?.Registration,
            ScheduledDepartureUtc: flight.ScheduledDepartureUtc,
            EstimatedDepartureUtc: flight.Operation?.EstimatedDepartureUtc,
            ActualDepartureUtc: flight.Operation?.ActualDepartureUtc,
            ScheduledArrivalUtc: flight.ScheduledArrivalUtc,
            EstimatedArrivalUtc: flight.Operation?.EstimatedArrivalUtc,
            ActualArrivalUtc: flight.Operation?.ActualArrivalUtc,
            Status: flight.Status,
            LastUpdateUtc: flight.Operation?.LastLiveUpdateUtc ?? flight.UpdatedUtc,
            SourceName: flight.Source?.Name,
            IsVerified: flight.IsVerified,
            HasLiveTracking: hasLive
        );
    }

    public async Task<DailyFlightCountsDto> GetDailyCountsAsync(DateOnly date, CancellationToken ct = default)
    {
        var erzurum = await _db.Airports.FirstOrDefaultAsync(a => a.IataCode == ErzurumIata, ct);
        if (erzurum is null)
        {
            return new DailyFlightCountsDto(date, 0, 0, 0);
        }

        var dayFlights = await _db.FlightInstances
            .Where(i => i.FlightDate == date)
            .Where(i => i.OriginAirportId == erzurum.Id || i.DestinationAirportId == erzurum.Id)
            .Select(i => new { i.OriginAirportId, i.Status })
            .ToListAsync(ct);

        var departures = dayFlights.Count(f => f.OriginAirportId == erzurum.Id);
        var arrivals = dayFlights.Count(f => f.OriginAirportId != erzurum.Id);
        var live = dayFlights.Count(f => f.Status is FlightStatus.Airborne or FlightStatus.Approaching
            or FlightStatus.NearAirport or FlightStatus.Taxiing or FlightStatus.Departed);

        return new DailyFlightCountsDto(date, departures, arrivals, live);
    }

    public async Task<IReadOnlyList<CalendarDayDto>> GetCalendarAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var erzurum = await _db.Airports.FirstOrDefaultAsync(a => a.IataCode == ErzurumIata, ct);
        if (erzurum is null || to < from)
        {
            return Array.Empty<CalendarDayDto>();
        }

        var flights = await _db.FlightInstances
            .Where(i => i.FlightDate >= from && i.FlightDate <= to)
            .Where(i => i.OriginAirportId == erzurum.Id || i.DestinationAirportId == erzurum.Id)
            .Select(i => new { i.FlightDate, i.IsVerified })
            .ToListAsync(ct);

        var byDate = flights.GroupBy(f => f.FlightDate)
            .ToDictionary(g => g.Key, g => (Total: g.Count(), Verified: g.Count(x => x.IsVerified)));

        var result = new List<CalendarDayDto>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            byDate.TryGetValue(date, out var stats);
            result.Add(new CalendarDayDto(date, stats.Total, stats.Verified, stats.Total > stats.Verified));
        }

        return result;
    }

    private static FlightSummaryDto ToSummaryDto(FlightInstance flight)
    {
        var originTz = TimeZoneHelper.ResolveTimeZone(flight.OriginAirport?.Timezone ?? "Europe/Istanbul");
        var destTz = TimeZoneHelper.ResolveTimeZone(flight.DestinationAirport?.Timezone ?? "Europe/Istanbul");

        var hasLive = flight.Operation is not null &&
                      flight.Status is FlightStatus.Monitoring or FlightStatus.AircraftDetected or
                          FlightStatus.Taxiing or FlightStatus.Departed or FlightStatus.Airborne or
                          FlightStatus.Approaching or FlightStatus.NearAirport;

        return new FlightSummaryDto(
            Id: flight.Id,
            FlightNumber: flight.FlightNumber,
            AirlineName: flight.Airline?.Name ?? flight.FlightSchedule?.Airline?.Name,
            OriginIata: flight.OriginAirport?.IataCode ?? "",
            DestinationIata: flight.DestinationAirport?.IataCode ?? "",
            ScheduledDepartureUtc: flight.ScheduledDepartureUtc,
            ScheduledArrivalUtc: flight.ScheduledArrivalUtc,
            ScheduledDepartureLocal: TimeZoneHelper.UtcToLocal(flight.ScheduledDepartureUtc, originTz).ToString("HH:mm"),
            ScheduledArrivalLocal: TimeZoneHelper.UtcToLocal(flight.ScheduledArrivalUtc, destTz).ToString("HH:mm"),
            Status: flight.Status,
            IsVerified: flight.IsVerified,
            HasLiveTracking: hasLive
        );
    }
}
