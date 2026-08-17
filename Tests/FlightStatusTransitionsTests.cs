using ErzurumFlight.Server.Helpers;
using ErzurumFlight.Server.Models;

namespace ErzurumFlight.Tests;

public class FlightStatusTransitionsTests
{
    [Fact]
    public void NextDepartureStatus_HighAltitude_ReturnsAirborne()
    {
        var k = new FlightKinematics(AltitudeFt: 5000, GroundSpeedKt: 250, VerticalRateFpm: 1500, DistanceFromAirportNm: 20);
        var status = FlightStatusTransitions.NextDepartureStatus(FlightStatus.Departed, k);
        Assert.Equal(FlightStatus.Airborne, status);
    }

    [Fact]
    public void NextDepartureStatus_TaxiSpeed_ReturnsTaxiing()
    {
        var k = new FlightKinematics(AltitudeFt: 0, GroundSpeedKt: 15, VerticalRateFpm: 0, DistanceFromAirportNm: 0);
        var status = FlightStatusTransitions.NextDepartureStatus(FlightStatus.Monitoring, k);
        Assert.Equal(FlightStatus.Taxiing, status);
    }

    [Fact]
    public void NextDepartureStatus_HighGroundSpeedOnRunway_ReturnsDeparted()
    {
        var k = new FlightKinematics(AltitudeFt: 0, GroundSpeedKt: 120, VerticalRateFpm: 100, DistanceFromAirportNm: 0);
        var status = FlightStatusTransitions.NextDepartureStatus(FlightStatus.Taxiing, k);
        Assert.Equal(FlightStatus.Departed, status);
    }

    [Fact]
    public void NextDepartureStatus_TerminalStatus_NeverRegresses()
    {
        var k = new FlightKinematics(AltitudeFt: 10000, GroundSpeedKt: 400, VerticalRateFpm: 2000, DistanceFromAirportNm: 50);
        var status = FlightStatusTransitions.NextDepartureStatus(FlightStatus.Cancelled, k);
        Assert.Equal(FlightStatus.Cancelled, status);
    }

    [Fact]
    public void NextArrivalStatus_OnGroundNearAirport_ReturnsLanded()
    {
        var k = new FlightKinematics(AltitudeFt: 0, GroundSpeedKt: 20, VerticalRateFpm: 0, DistanceFromAirportNm: 2);
        var status = FlightStatusTransitions.NextArrivalStatus(FlightStatus.NearAirport, k);
        Assert.Equal(FlightStatus.Landed, status);
    }

    [Fact]
    public void NextArrivalStatus_DescendingFarFromAirport_ReturnsApproaching()
    {
        var k = new FlightKinematics(AltitudeFt: 8000, GroundSpeedKt: 300, VerticalRateFpm: -1200, DistanceFromAirportNm: 30);
        var status = FlightStatusTransitions.NextArrivalStatus(FlightStatus.Airborne, k);
        Assert.Equal(FlightStatus.Approaching, status);
    }

    [Fact]
    public void NextArrivalStatus_CruisingFarAway_StaysAirborne()
    {
        var k = new FlightKinematics(AltitudeFt: 35000, GroundSpeedKt: 480, VerticalRateFpm: 0, DistanceFromAirportNm: 300);
        var status = FlightStatusTransitions.NextArrivalStatus(FlightStatus.Airborne, k);
        Assert.Equal(FlightStatus.Airborne, status);
    }

    [Fact]
    public void NextArrivalStatus_LowAltitudeNearAirportButStillMoving_ReturnsNearAirport()
    {
        var k = new FlightKinematics(AltitudeFt: 800, GroundSpeedKt: 140, VerticalRateFpm: -400, DistanceFromAirportNm: 5);
        var status = FlightStatusTransitions.NextArrivalStatus(FlightStatus.Approaching, k);
        Assert.Equal(FlightStatus.NearAirport, status);
    }
}
