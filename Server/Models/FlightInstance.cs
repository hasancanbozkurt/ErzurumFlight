using System.ComponentModel.DataAnnotations;

namespace ErzurumFlight.Server.Models;

/// <summary>
/// Belirli bir tarihteki somut uçuş. FlightSchedule şablonundan tarih üretim motoru tarafından
/// oluşturulur. Unique index: FlightDate + FlightNumber + OriginAirportId + DestinationAirportId.
/// </summary>
public class FlightInstance
{
    public int Id { get; set; }

    public int? FlightScheduleId { get; set; }
    public FlightSchedule? FlightSchedule { get; set; }

    /// <summary>
    /// Havayolu referansı. FlightSchedule'dan üretilen instance'larda FlightSchedule.Airline
    /// üzerinden dolaylı olarak da bilinir, ama ScheduleSyncService gibi bir FlightSchedule şablonu
    /// olmadan (doğrudan canlı kaynaktan) oluşturulan instance'lar için burada DOĞRUDAN tutulur.
    /// FlightService bu alanı önce, boşsa FlightSchedule.Airline'ı kullanır.
    /// </summary>
    public int? AirlineId { get; set; }
    public Airline? Airline { get; set; }

    public DateOnly FlightDate { get; set; }

    [Required, MaxLength(10)]
    public string FlightNumber { get; set; } = string.Empty;

    public int OriginAirportId { get; set; }
    public Airport? OriginAirport { get; set; }

    public int DestinationAirportId { get; set; }
    public Airport? DestinationAirport { get; set; }

    public DateTime ScheduledDepartureUtc { get; set; }
    public DateTime ScheduledArrivalUtc { get; set; }

    public FlightStatus Status { get; set; } = FlightStatus.Scheduled;

    /// <summary>Bu tarih için tarife resmi/güvenilir bir kaynaktan doğrulanmış mı.</summary>
    public bool IsVerified { get; set; }

    public int SourceId { get; set; }
    public DataSource? Source { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public FlightOperation? Operation { get; set; }
}
