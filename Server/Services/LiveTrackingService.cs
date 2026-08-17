using ErzurumFlight.Server.Data;
using ErzurumFlight.Server.DTOs;
using ErzurumFlight.Server.Helpers;
using ErzurumFlight.Server.Models;
using ErzurumFlight.Server.Providers;
using Microsoft.EntityFrameworkCore;

namespace ErzurumFlight.Server.Services;

/// <summary>
/// Canlı takip akışının orkestrasyonu: izlenmesi gereken uçuşları bulur, ILiveTrackingProvider'dan
/// veri çeker, AircraftMatchingService ile eşleştirir, FlightOperation/AircraftPosition günceller,
/// MemoryCache'i tazeler ve SignalR'a gönderilecek olayları döndürür.
/// Akış (şartname bölüm 12): BackgroundService → Airplanes.Live → LiveTrackingService → MemoryCache → SignalR → React.
/// </summary>
public interface ILiveTrackingService
{
    Task<LiveTrackingResult> RefreshAsync(CancellationToken ct = default);
    IReadOnlyList<LiveAircraftDto> GetCachedAircraft();
}

public record LiveTrackingResult(
    IReadOnlyList<FlightStatusChangedEvent> StatusChanges,
    IReadOnlyList<FlightPositionUpdatedEvent> PositionUpdates,
    int AircraftObserved
);

public class LiveTrackingService : ILiveTrackingService
{
    private const string CacheKey = "live:aircraft";
    private const double DefaultRadiusNm = 100;

    private readonly FlightDbContext _db;
    private readonly ILiveTrackingProvider _provider;
    private readonly IAircraftMatchingService _matchingService;
    private readonly IDataSourceService _dataSourceService;
    private readonly ICacheService _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<LiveTrackingService> _logger;

    public LiveTrackingService(
        FlightDbContext db,
        ILiveTrackingProvider provider,
        IAircraftMatchingService matchingService,
        IDataSourceService dataSourceService,
        ICacheService cache,
        IConfiguration config,
        ILogger<LiveTrackingService> logger)
    {
        _db = db;
        _provider = provider;
        _matchingService = matchingService;
        _dataSourceService = dataSourceService;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    public async Task<LiveTrackingResult> RefreshAsync(CancellationToken ct = default)
    {
        var erzurumIata = _config["Airport:Iata"] ?? SeedData.ErzurumIata;
        var erzurum = await _db.Airports.FirstOrDefaultAsync(a => a.IataCode == erzurumIata, ct);
        if (erzurum is null)
        {
            return new LiveTrackingResult(Array.Empty<FlightStatusChangedEvent>(), Array.Empty<FlightPositionUpdatedEvent>(), 0);
        }

        var radiusNm = _config.GetValue<double?>("FlightTracking:RadiusNm") ?? DefaultRadiusNm;

        IReadOnlyList<LiveAircraftCandidate> liveCandidates;
        try
        {
            liveCandidates = await _provider.GetAircraftNearAsync(erzurum.Latitude, erzurum.Longitude, radiusNm, ct);
            await _dataSourceService.RecordSuccessAsync(_provider.SourceName, ct);
        }
        catch (Exception ex)
        {
            // Dış kaynak çalışmazsa uygulama çökmez; son doğrulanmış cache verisi korunur.
            await _dataSourceService.RecordFailureAsync(_provider.SourceName, ex.Message, ct);
            _logger.LogWarning(ex, "Canlı takip verisi alınamadı, önceki cache korunuyor.");
            return new LiveTrackingResult(Array.Empty<FlightStatusChangedEvent>(), Array.Empty<FlightPositionUpdatedEvent>(), 0);
        }

        // 1. İzlenmesi gereken uçuşları belirle: bugün ±6 saat penceresindeki, henüz tamamlanmamış uçuşlar.
        var now = DateTime.UtcNow;
        var window = TimeSpan.FromHours(6);
        var today = TimeZoneHelper.TodayInIstanbul(now);

        // Not: SQLite'a çevrilebilir kalması için zaman penceresi filtresi bellek tarafında (C# LINQ) uygulanır;
        // EF.Functions.DateDiffMinute gibi sağlayıcıya özgü SQL fonksiyonları kullanılmaz.
        var candidateFlights = await _db.FlightInstances
            .Include(i => i.OriginAirport)
            .Include(i => i.DestinationAirport)
            .Include(i => i.FlightSchedule).ThenInclude(s => s!.Airline)
            .Include(i => i.Airline)
            .Include(i => i.Operation)
            .Where(i => i.FlightDate == today || i.FlightDate == today.AddDays(-1))
            .Where(i => i.OriginAirportId == erzurum.Id || i.DestinationAirportId == erzurum.Id)
            .Where(i => i.Status != FlightStatus.Landed && i.Status != FlightStatus.Cancelled)
            .ToListAsync(ct);

        var monitored = candidateFlights.Where(i =>
            Math.Abs((i.ScheduledDepartureUtc - now).TotalMinutes) < window.TotalMinutes ||
            Math.Abs((i.ScheduledArrivalUtc - now).TotalMinutes) < window.TotalMinutes ||
            i.Status != FlightStatus.Scheduled
        ).ToList();

        var matchInput = monitored
            .Select(f => (Flight: f, AirlineCallsignPrefix: f.Airline?.Callsign ?? f.FlightSchedule?.Airline?.Callsign))
            .ToList();

        var matches = _matchingService.MatchAll(matchInput, liveCandidates, erzurum.Latitude, erzurum.Longitude);

        var statusEvents = new List<FlightStatusChangedEvent>();
        var positionEvents = new List<FlightPositionUpdatedEvent>();
        var operationsToRecordPosition = new List<(FlightOperation Operation, LiveAircraftCandidate Candidate, string FlightNumber)>();

        foreach (var outcome in matches)
        {
            var flight = monitored.First(f => f.Id == outcome.FlightInstanceId);
            var isDeparture = flight.OriginAirportId == erzurum.Id;

            if (outcome.Result.IsUnknown || outcome.Result.Candidate is null)
            {
                continue; // Belirsiz eşleşme: mevcut durum korunur, yanlış uçak gösterilmez.
            }

            var candidate = outcome.Result.Candidate;

            var operation = flight.Operation ?? new FlightOperation
            {
                FlightInstanceId = flight.Id,
                Status = flight.Status
            };

            if (flight.Operation is null)
            {
                _db.FlightOperations.Add(operation);
            }

            var aircraft = await _db.Aircrafts.FirstOrDefaultAsync(a => a.IcaoHex == candidate.IcaoHex, ct);
            if (aircraft is null)
            {
                aircraft = new Aircraft { IcaoHex = candidate.IcaoHex, Registration = candidate.Registration, LastSeenUtc = now };
                _db.Aircrafts.Add(aircraft);
            }
            else
            {
                aircraft.LastSeenUtc = now;
                aircraft.Registration ??= candidate.Registration;
            }

            operation.Aircraft = aircraft;
            operation.MatchConfidence = outcome.Result.Confidence;
            operation.LastLiveUpdateUtc = now;

            var distanceNm = AircraftMatcher.HaversineNm(candidate.Latitude, candidate.Longitude, erzurum.Latitude, erzurum.Longitude);
            var kinematics = new FlightKinematics(null, null, null, distanceNm);

            var previousStatus = flight.Status;
            var newStatus = isDeparture
                ? FlightStatusTransitions.NextDepartureStatus(flight.Status, kinematics)
                : FlightStatusTransitions.NextArrivalStatus(flight.Status, kinematics);

            flight.Status = newStatus;
            operation.Status = newStatus;
            flight.UpdatedUtc = now;

            if (newStatus == FlightStatus.Departed && operation.ActualDepartureUtc is null)
            {
                operation.ActualDepartureUtc = now;
            }
            if (newStatus == FlightStatus.Landed && operation.ActualArrivalUtc is null)
            {
                operation.ActualArrivalUtc = now;
            }

            if (newStatus != previousStatus)
            {
                statusEvents.Add(new FlightStatusChangedEvent(flight.Id, flight.FlightNumber, newStatus.ToString(), now));
            }

            operationsToRecordPosition.Add((operation, candidate, flight.FlightNumber));
        }

        // İlk SaveChanges: yeni FlightOperation kayıtları Id alır, ardından AircraftPosition'lar bu Id'lerle eklenebilir.
        await _db.SaveChangesAsync(ct);

        var liveSource = await _dataSourceService.GetBySourceNameAsync(_provider.SourceName, ct);

        foreach (var (operation, candidate, flightNumber) in operationsToRecordPosition)
        {
            _db.AircraftPositions.Add(new AircraftPosition
            {
                FlightOperationId = operation.Id,
                TimestampUtc = now,
                Latitude = candidate.Latitude,
                Longitude = candidate.Longitude,
                Heading = candidate.Heading,
                IcaoHex = candidate.IcaoHex,
                Callsign = candidate.Callsign,
                SourceId = liveSource?.Id ?? 0
            });

            positionEvents.Add(new FlightPositionUpdatedEvent(
                operation.Id, flightNumber, candidate.Latitude, candidate.Longitude, candidate.Heading, now));
        }

        await _db.SaveChangesAsync(ct);

        var liveDtos = liveCandidates.Select(c => new LiveAircraftDto(
            FlightOperationId: null,
            FlightNumber: matches.FirstOrDefault(m => !m.Result.IsUnknown && m.Result.Candidate?.IcaoHex == c.IcaoHex) is { } m
                ? monitored.First(f => f.Id == m.FlightInstanceId).FlightNumber
                : null,
            IcaoHex: c.IcaoHex,
            Callsign: c.Callsign,
            Registration: c.Registration,
            Latitude: c.Latitude,
            Longitude: c.Longitude,
            Altitude: null,
            GroundSpeed: null,
            Heading: c.Heading,
            Status: "Live",
            TimestampUtc: c.ObservedUtc
        )).ToList();

        _cache.Set(CacheKey, liveDtos, TimeSpan.FromSeconds(45));

        return new LiveTrackingResult(statusEvents, positionEvents, liveCandidates.Count);
    }

    public IReadOnlyList<LiveAircraftDto> GetCachedAircraft() =>
        _cache.Get<List<LiveAircraftDto>>(CacheKey) ?? new List<LiveAircraftDto>();
}
