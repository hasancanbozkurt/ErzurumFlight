using ErzurumFlight.Server.Helpers;

namespace ErzurumFlight.Tests;

public class TimeZoneHelperTests
{
    [Fact]
    public void LocalToUtc_ErzurumLocalMorning_ConvertsToCorrectUtc()
    {
        // Türkiye 2016 sonrasında yıl boyu sabit UTC+3 kullanır (DST uygulanmaz).
        var date = new DateOnly(2026, 8, 10);
        var time = new TimeOnly(9, 25);

        var utc = TimeZoneHelper.LocalToUtc(date, time, TimeZoneHelper.Istanbul);

        Assert.Equal(new DateTime(2026, 8, 10, 6, 25, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void UtcToLocal_RoundTrip_MatchesOriginalLocalTime()
    {
        var date = new DateOnly(2026, 12, 25);
        var time = new TimeOnly(23, 45);

        var utc = TimeZoneHelper.LocalToUtc(date, time, TimeZoneHelper.Istanbul);
        var backToLocal = TimeZoneHelper.UtcToLocal(utc, TimeZoneHelper.Istanbul);

        Assert.Equal(date.ToDateTime(time), backToLocal);
    }

    [Fact]
    public void UtcToLocal_ProducesCorrectIstanbulOffset()
    {
        // Şartname 23. bölüm: sabit UTC+3 hesabı kullanılmamalı ama Europe/Istanbul için sonuç +3 saat olmalı.
        var utc = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var local = TimeZoneHelper.UtcToLocal(utc, TimeZoneHelper.Istanbul);

        Assert.Equal(new DateTime(2026, 3, 1, 15, 0, 0), local);
    }

    [Fact]
    public void TodayInIstanbul_LateUtcEvening_IsAlreadyNextDayLocally()
    {
        // UTC 21:30 -> İstanbul'da yerel saat 00:30, yani bir sonraki gün.
        var utcNow = new DateTime(2026, 8, 10, 21, 30, 0, DateTimeKind.Utc);

        var today = TimeZoneHelper.TodayInIstanbul(utcNow);

        Assert.Equal(new DateOnly(2026, 8, 11), today);
    }

    [Fact]
    public void ResolveTimeZone_UnknownId_FallsBackWithoutThrowing()
    {
        var tz = TimeZoneHelper.ResolveTimeZone("Not/A_Real_Zone");
        Assert.NotNull(tz);
    }
}
