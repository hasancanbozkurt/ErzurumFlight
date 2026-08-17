using ErzurumFlight.Server.Models;

namespace ErzurumFlight.Server.DTOs;

/// <summary>Uçuş listesinde (dashboard, giden/gelen) gösterilen özet kart bilgisi.</summary>
public record FlightSummaryDto(
    int Id,
    string FlightNumber,
    string? AirlineName,
    string OriginIata,
    string DestinationIata,
    DateTime ScheduledDepartureUtc,
    DateTime ScheduledArrivalUtc,
    string ScheduledDepartureLocal,
    string ScheduledArrivalLocal,
    FlightStatus Status,
    bool IsVerified,
    bool HasLiveTracking
);

/// <summary>Uçuş detay sayfasında gösterilen tam bilgi.</summary>
public record FlightDetailDto(
    int Id,
    string FlightNumber,
    string? AirlineName,
    string OriginIata,
    string OriginName,
    string DestinationIata,
    string DestinationName,
    string? AircraftType,
    string? Registration,
    DateTime ScheduledDepartureUtc,
    DateTime? EstimatedDepartureUtc,
    DateTime? ActualDepartureUtc,
    DateTime ScheduledArrivalUtc,
    DateTime? EstimatedArrivalUtc,
    DateTime? ActualArrivalUtc,
    FlightStatus Status,
    DateTime? LastUpdateUtc,
    string? SourceName,
    bool IsVerified,
    bool HasLiveTracking
);

/// <summary>Bir günün uçuş sayıları (dashboard üst özet: giden/gelen/canlı).</summary>
public record DailyFlightCountsDto(DateOnly Date, int Departures, int Arrivals, int Live);

/// <summary>Takvim görünümü için gün bazlı doğrulama özeti.</summary>
public record CalendarDayDto(DateOnly Date, int TotalFlights, int VerifiedFlights, bool AnyUnverified);
