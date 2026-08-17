using System.ComponentModel.DataAnnotations;

namespace ErzurumFlight.Server.Models;

/// <summary>Bir uçağın belirli bir zaman noktasındaki konum ve uçuş verisi (ADS-B örneklemesi).</summary>
public class AircraftPosition
{
    public long Id { get; set; }

    public int FlightOperationId { get; set; }
    public FlightOperation? FlightOperation { get; set; }

    public DateTime TimestampUtc { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Fit (ayak) cinsinden irtifa.</summary>
    public double? Altitude { get; set; }

    /// <summary>Knot cinsinden yer hızı.</summary>
    public double? GroundSpeed { get; set; }

    /// <summary>Derece cinsinden yön (0-360).</summary>
    public double? Heading { get; set; }

    /// <summary>Feet/min cinsinden dikey hız (pozitif: tırmanış, negatif: alçalış).</summary>
    public double? VerticalRate { get; set; }

    [MaxLength(6)]
    public string? IcaoHex { get; set; }

    [MaxLength(20)]
    public string? Callsign { get; set; }

    public int SourceId { get; set; }
    public DataSource? Source { get; set; }
}
