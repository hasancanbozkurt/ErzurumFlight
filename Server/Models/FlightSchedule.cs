using System.ComponentModel.DataAnnotations;

namespace ErzurumFlight.Server.Models;

/// <summary>
/// İleri tarihli tarifenin temel tablosu. Haftanın hangi günlerinde uçuşun tekrarlandığını,
/// geçerlilik aralığını ve doğrulama durumunu tutar. FlightInstance kayıtları bu şablondan üretilir.
/// </summary>
public class FlightSchedule
{
    public int Id { get; set; }

    public int AirlineId { get; set; }
    public Airline? Airline { get; set; }

    [Required, MaxLength(10)]
    public string FlightNumber { get; set; } = string.Empty;

    public int OriginAirportId { get; set; }
    public Airport? OriginAirport { get; set; }

    public int DestinationAirportId { get; set; }
    public Airport? DestinationAirport { get; set; }

    /// <summary>Kalkış havalimanının yerel saatine göre planlanan kalkış saati (TimeOnly).</summary>
    public TimeOnly DepartureLocalTime { get; set; }

    /// <summary>Varış havalimanının yerel saatine göre planlanan varış saati (TimeOnly).</summary>
    public TimeOnly ArrivalLocalTime { get; set; }

    public bool Monday { get; set; }
    public bool Tuesday { get; set; }
    public bool Wednesday { get; set; }
    public bool Thursday { get; set; }
    public bool Friday { get; set; }
    public bool Saturday { get; set; }
    public bool Sunday { get; set; }

    /// <summary>Tarifenin geçerli olduğu ilk tarih (dahil).</summary>
    public DateOnly ValidFrom { get; set; }

    /// <summary>Tarifenin geçerli olduğu son tarih (dahil). Null ise süresiz.</summary>
    public DateOnly? ValidTo { get; set; }

    public int SourceId { get; set; }
    public DataSource? Source { get; set; }

    public bool IsVerified { get; set; }
    public DateTime? LastVerifiedUtc { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public ICollection<FlightInstance> Instances { get; set; } = new List<FlightInstance>();

    /// <summary>Belirtilen haftanın gününde bu tarifenin çalışıp çalışmadığını döndürür.</summary>
    public bool RunsOn(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Monday,
        DayOfWeek.Tuesday => Tuesday,
        DayOfWeek.Wednesday => Wednesday,
        DayOfWeek.Thursday => Thursday,
        DayOfWeek.Friday => Friday,
        DayOfWeek.Saturday => Saturday,
        DayOfWeek.Sunday => Sunday,
        _ => false
    };
}
