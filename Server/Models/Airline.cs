using System.ComponentModel.DataAnnotations;

namespace ErzurumFlight.Server.Models;

/// <summary>Bir havayolu şirketini temsil eder.</summary>
public class Airline
{
    public int Id { get; set; }

    [MaxLength(3)]
    public string? IataCode { get; set; }

    [MaxLength(4)]
    public string? IcaoCode { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>ADS-B callsign eşleştirmesi için kullanılan radyo çağrı kodu, örn. "THY".</summary>
    [MaxLength(20)]
    public string? Callsign { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<FlightSchedule> Schedules { get; set; } = new List<FlightSchedule>();
}
