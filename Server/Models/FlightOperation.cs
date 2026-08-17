namespace ErzurumFlight.Server.Models;

/// <summary>
/// Bir uçuş gerçekleşmeye başladığında (izlemeye alındığında) oluşturulan operasyon bilgileri.
/// Tahmini/gerçek saatler ve canlı uçak eşleştirme güveni burada tutulur.
/// </summary>
public class FlightOperation
{
    public int Id { get; set; }

    public int FlightInstanceId { get; set; }
    public FlightInstance? FlightInstance { get; set; }

    public int? AircraftId { get; set; }
    public Aircraft? Aircraft { get; set; }

    public DateTime? EstimatedDepartureUtc { get; set; }
    public DateTime? ActualDepartureUtc { get; set; }
    public DateTime? EstimatedArrivalUtc { get; set; }
    public DateTime? ActualArrivalUtc { get; set; }

    public FlightStatus Status { get; set; } = FlightStatus.Scheduled;

    /// <summary>Uçuş-uçak eşleştirme güven skoru (0-100). Belirsizse Unknown kabul edilir.</summary>
    public int MatchConfidence { get; set; }

    public DateTime? LastLiveUpdateUtc { get; set; }

    public ICollection<AircraftPosition> Positions { get; set; } = new List<AircraftPosition>();
}
