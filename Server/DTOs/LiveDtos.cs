namespace ErzurumFlight.Server.DTOs;

/// <summary>Canlı haritada gösterilen bir uçağın anlık durumu.</summary>
public record LiveAircraftDto(
    int? FlightOperationId,
    string? FlightNumber,
    string IcaoHex,
    string? Callsign,
    string? Registration,
    double Latitude,
    double Longitude,
    double? Altitude,
    double? GroundSpeed,
    double? Heading,
    string Status,
    DateTime TimestampUtc
);

/// <summary>SignalR üzerinden gönderilen uçuş durumu değişikliği olayı.</summary>
public record FlightStatusChangedEvent(int FlightInstanceId, string FlightNumber, string Status, DateTime TimestampUtc);

/// <summary>SignalR üzerinden gönderilen konum güncelleme olayı.</summary>
public record FlightPositionUpdatedEvent(int FlightOperationId, string? FlightNumber, double Latitude, double Longitude, double? Heading, DateTime TimestampUtc);
