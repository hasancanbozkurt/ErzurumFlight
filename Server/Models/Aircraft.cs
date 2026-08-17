using System.ComponentModel.DataAnnotations;

namespace ErzurumFlight.Server.Models;

/// <summary>Canlı takipte görülmüş bir uçak; ADS-B verisinden gelen fiziksel uçak kaydı.</summary>
public class Aircraft
{
    public int Id { get; set; }

    /// <summary>24-bit ICAO hex adresi (ADS-B'nin tekil kimliği), örn. "4BA9F1".</summary>
    [Required, MaxLength(6)]
    public string IcaoHex { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Registration { get; set; }

    [MaxLength(50)]
    public string? AircraftType { get; set; }

    public DateTime LastSeenUtc { get; set; }

    public ICollection<FlightOperation> Operations { get; set; } = new List<FlightOperation>();
}
