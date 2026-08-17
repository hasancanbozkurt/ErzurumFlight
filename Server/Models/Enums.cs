namespace ErzurumFlight.Server.Models;

/// <summary>
/// Bir uçuşun yaşam döngüsü boyunca alabileceği durumlar.
/// Kalkış akışı: Scheduled → Monitoring → AircraftDetected → Taxiing → Departed → Airborne
/// İniş akışı:   Airborne → Approaching → NearAirport → Landed
/// </summary>
public enum FlightStatus
{
    Scheduled = 0,
    Monitoring = 1,
    AircraftDetected = 2,
    Taxiing = 3,
    Departed = 4,
    Airborne = 5,
    Approaching = 6,
    NearAirport = 7,
    Landed = 8,
    Delayed = 9,
    Cancelled = 10,
    Diverted = 11,
    Unknown = 12
}

/// <summary>Uçuş yönü: Erzurum'dan kalkış mı, Erzurum'a varış mı.</summary>
public enum FlightDirection
{
    Departure = 0,
    Arrival = 1
}

/// <summary>Bir veri kaynağının türü.</summary>
public enum DataSourceType
{
    OfficialAirport = 0,
    OfficialAirline = 1,
    OpenData = 2,
    Scraper = 3,
    ManualAdmin = 4,
    LiveTracking = 5
}
