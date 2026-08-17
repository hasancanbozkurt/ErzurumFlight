using System.ComponentModel.DataAnnotations;

namespace ErzurumFlight.Server.Models;

/// <summary>Bir havalimanını temsil eder (örn. Erzurum ERZ/LTCE veya diğer uçuş rotalarındaki havalimanları).</summary>
public class Airport
{
    public int Id { get; set; }

    [Required, MaxLength(3)]
    public string IataCode { get; set; } = string.Empty;

    [Required, MaxLength(4)]
    public string IcaoCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>IANA zaman dilimi kimliği, örn. "Europe/Istanbul".</summary>
    [Required, MaxLength(64)]
    public string Timezone { get; set; } = "Europe/Istanbul";

    public bool IsActive { get; set; } = true;

    public ICollection<FlightSchedule> DepartureSchedules { get; set; } = new List<FlightSchedule>();
    public ICollection<FlightSchedule> ArrivalSchedules { get; set; } = new List<FlightSchedule>();
}
