using ErzurumFlight.Server.Helpers;
using ErzurumFlight.Server.Models;

namespace ErzurumFlight.Tests;

public class FlightStatusMapperTests
{
    [Theory]
    [InlineData("Canceled", FlightStatus.Cancelled)]
    [InlineData("Cancelled", FlightStatus.Cancelled)]
    [InlineData("CanceledUncertain", FlightStatus.Cancelled)]
    [InlineData("canceled", FlightStatus.Cancelled)]
    public void Map_AnyCancelledVariant_MapsToCancelled(string raw, FlightStatus expected)
    {
        // Kritik: bir iptal metni ASLA sessizce başka bir duruma düşmemeli.
        Assert.Equal(expected, FlightStatusMapper.Map(raw));
    }

    [Fact]
    public void Map_Diverted_MapsToDiverted()
    {
        Assert.Equal(FlightStatus.Diverted, FlightStatusMapper.Map("Diverted"));
    }

    [Theory]
    [InlineData("Arrived")]
    [InlineData("Landed")]
    public void Map_ArrivedOrLanded_MapsToLanded(string raw)
    {
        Assert.Equal(FlightStatus.Landed, FlightStatusMapper.Map(raw));
    }

    [Fact]
    public void Map_Delayed_MapsToDelayed()
    {
        Assert.Equal(FlightStatus.Delayed, FlightStatusMapper.Map("Delayed"));
    }

    [Theory]
    [InlineData("Expected")]
    [InlineData("Scheduled")]
    public void Map_ExpectedOrScheduled_MapsToScheduled(string raw)
    {
        Assert.Equal(FlightStatus.Scheduled, FlightStatusMapper.Map(raw));
    }

    [Fact]
    public void Map_NullOrEmpty_MapsToUnknown()
    {
        Assert.Equal(FlightStatus.Unknown, FlightStatusMapper.Map(null));
        Assert.Equal(FlightStatus.Unknown, FlightStatusMapper.Map(""));
        Assert.Equal(FlightStatus.Unknown, FlightStatusMapper.Map("   "));
    }

    [Fact]
    public void Map_CompletelyUnrecognizedText_MapsToUnknown_NeverToCancelled()
    {
        // Bilinmeyen bir metin asla yanlışlıkla "İptal" gibi kritik bir duruma düşmemeli.
        var result = FlightStatusMapper.Map("SomeBrandNewStatusTextFromFutureApiVersion");
        Assert.Equal(FlightStatus.Unknown, result);
        Assert.NotEqual(FlightStatus.Cancelled, result);
    }
}
