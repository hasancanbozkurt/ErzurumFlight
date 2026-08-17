namespace ErzurumFlight.Server.DTOs;

public record LoginRequest(string UserName, string Password);

public record UpsertScheduleRequest(
    int? Id,
    int AirlineId,
    string FlightNumber,
    int OriginAirportId,
    int DestinationAirportId,
    TimeOnly DepartureLocalTime,
    TimeOnly ArrivalLocalTime,
    bool Monday, bool Tuesday, bool Wednesday, bool Thursday, bool Friday, bool Saturday, bool Sunday,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    int SourceId,
    bool IsVerified,
    string? Notes
);

public record DataSourceStatusDto(
    int Id,
    string Name,
    string Type,
    bool IsEnabled,
    int Priority,
    DateTime? LastSuccessUtc,
    DateTime? LastFailureUtc,
    string? LastError
);
