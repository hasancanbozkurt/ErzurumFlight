using ErzurumFlight.Server.Models;

namespace ErzurumFlight.Server.Helpers;

/// <summary>Bir uçağın anlık uçuş verisi; durum geçişi hesaplamak için kullanılır.</summary>
public record FlightKinematics(double? AltitudeFt, double? GroundSpeedKt, double? VerticalRateFpm, double DistanceFromAirportNm);

/// <summary>
/// Kalkış: Scheduled → Monitoring → AircraftDetected → Taxiing → Departed → Airborne
/// İniş:   Airborne → Approaching → NearAirport → Landed
/// Tek bir irtifa değerine göre karar verilmez; hız, irtifa, dikey hız ve havaalanına
/// uzaklık birlikte değerlendirilir.
/// </summary>
public static class FlightStatusTransitions
{
    private const double TaxiSpeedKt = 40;
    private const double AirborneAltitudeFt = 500;
    private const double NearAirportNm = 8;
    private const double ApproachAltitudeFt = 3000;

    /// <summary>Kalkış yönündeki bir uçuş için mevcut kinematik veriye göre yeni durumu hesaplar.</summary>
    public static FlightStatus NextDepartureStatus(FlightStatus current, FlightKinematics k)
    {
        if (current is FlightStatus.Cancelled or FlightStatus.Diverted or FlightStatus.Landed)
        {
            return current; // Terminal durumlar geri alınmaz.
        }

        var isMoving = (k.GroundSpeedKt ?? 0) > 5;
        var isTaxiSpeed = (k.GroundSpeedKt ?? 0) is > 5 and <= TaxiSpeedKt;
        var isAirborne = (k.AltitudeFt ?? 0) > AirborneAltitudeFt || (k.VerticalRateFpm ?? 0) > 300;

        if (isAirborne)
        {
            return FlightStatus.Airborne;
        }

        if (isMoving && !isTaxiSpeed)
        {
            return FlightStatus.Departed; // Pistte hızlanıyor, kalkış anı.
        }

        if (isTaxiSpeed)
        {
            return FlightStatus.Taxiing;
        }

        return current == FlightStatus.Scheduled ? FlightStatus.AircraftDetected : current;
    }

    /// <summary>İniş yönündeki bir uçuş için mevcut kinematik veriye göre yeni durumu hesaplar.</summary>
    public static FlightStatus NextArrivalStatus(FlightStatus current, FlightKinematics k)
    {
        if (current is FlightStatus.Cancelled or FlightStatus.Diverted or FlightStatus.Landed)
        {
            return current;
        }

        var isOnGround = (k.GroundSpeedKt ?? 999) < TaxiSpeedKt && (k.AltitudeFt ?? 999) < 50;
        if (isOnGround && k.DistanceFromAirportNm <= NearAirportNm)
        {
            return FlightStatus.Landed;
        }

        if (k.DistanceFromAirportNm <= NearAirportNm && (k.AltitudeFt ?? 0) < ApproachAltitudeFt)
        {
            return FlightStatus.NearAirport;
        }

        var isDescending = (k.VerticalRateFpm ?? 0) < -200;
        if (isDescending && (k.AltitudeFt ?? double.MaxValue) < ApproachAltitudeFt * 3)
        {
            return FlightStatus.Approaching;
        }

        return FlightStatus.Airborne;
    }
}
