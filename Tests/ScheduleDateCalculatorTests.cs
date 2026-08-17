using ErzurumFlight.Server.Helpers;
using ErzurumFlight.Server.Models;

namespace ErzurumFlight.Tests;

public class ScheduleDateCalculatorTests
{
    private static FlightSchedule MakeSchedule(
        bool mon = false, bool tue = false, bool wed = false, bool thu = false,
        bool fri = false, bool sat = false, bool sun = false,
        DateOnly? validFrom = null, DateOnly? validTo = null)
    {
        return new FlightSchedule
        {
            FlightNumber = "TK2705",
            Monday = mon, Tuesday = tue, Wednesday = wed, Thursday = thu,
            Friday = fri, Saturday = sat, Sunday = sun,
            ValidFrom = validFrom ?? new DateOnly(2026, 1, 1),
            ValidTo = validTo
        };
    }

    [Fact]
    public void GenerateDates_OnlySelectedWeekdays_AreReturned()
    {
        // Pazartesi + Çarşamba + Cuma, şartname örneğindeki gibi.
        var schedule = MakeSchedule(mon: true, wed: true, fri: true,
            validFrom: new DateOnly(2026, 8, 1), validTo: new DateOnly(2026, 8, 31));

        var dates = ScheduleDateCalculator.GenerateDates(schedule, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 7));

        // 2026-08-01 Cumartesi, 08-03 Pazartesi, 08-05 Çarşamba, 08-07 Cuma.
        Assert.Equal(new[]
        {
            new DateOnly(2026, 8, 3),
            new DateOnly(2026, 8, 5),
            new DateOnly(2026, 8, 7)
        }, dates);
    }

    [Fact]
    public void GenerateDates_RespectsValidToBoundary()
    {
        var schedule = MakeSchedule(mon: true, tue: true, wed: true, thu: true, fri: true, sat: true, sun: true,
            validFrom: new DateOnly(2026, 8, 1), validTo: new DateOnly(2026, 8, 5));

        var dates = ScheduleDateCalculator.GenerateDates(schedule, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        Assert.Equal(5, dates.Count);
        Assert.All(dates, d => Assert.True(d <= new DateOnly(2026, 8, 5)));
    }

    [Fact]
    public void GenerateDates_ValidToNull_MeansIndefinite()
    {
        var schedule = MakeSchedule(mon: true, tue: true, wed: true, thu: true, fri: true, sat: true, sun: true,
            validFrom: new DateOnly(2026, 1, 1), validTo: null);

        var dates = ScheduleDateCalculator.GenerateDates(schedule, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3));

        Assert.Equal(3, dates.Count);
    }

    [Fact]
    public void GenerateDates_RequestedRangeBeforeValidFrom_ReturnsEmpty()
    {
        var schedule = MakeSchedule(mon: true, tue: true, wed: true, thu: true, fri: true, sat: true, sun: true,
            validFrom: new DateOnly(2026, 9, 1));

        var dates = ScheduleDateCalculator.GenerateDates(schedule, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        Assert.Empty(dates);
    }

    [Fact]
    public void GenerateDates_ReversedRange_ReturnsEmpty()
    {
        var schedule = MakeSchedule(mon: true, tue: true, wed: true, thu: true, fri: true, sat: true, sun: true);

        var dates = ScheduleDateCalculator.GenerateDates(schedule, new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 1));

        Assert.Empty(dates);
    }

    [Theory]
    [InlineData("bugün", 0, 0)]
    [InlineData("yarın", 1, 1)]
    [InlineData("7gün", 0, 6)]
    [InlineData("30gün", 0, 29)]
    public void ResolveRangeShortcut_ReturnsExpectedOffsets(string shortcut, int expectedFromOffset, int expectedToOffset)
    {
        var today = new DateOnly(2026, 8, 10);
        var (from, to) = ScheduleDateCalculator.ResolveRangeShortcut(shortcut, today);

        Assert.Equal(today.AddDays(expectedFromOffset), from);
        Assert.Equal(today.AddDays(expectedToOffset), to);
    }
}
