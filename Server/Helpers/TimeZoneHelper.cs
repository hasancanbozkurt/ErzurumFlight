namespace ErzurumFlight.Server.Helpers;

/// <summary>
/// Veritabanı her zaman UTC tutar; UI Europe/Istanbul yerel saatini gösterir.
/// Sabit "UTC+3" hesabı KULLANILMAZ — .NET'in TimeZoneInfo dönüşümü DST/tarihsel kurallara
/// (Türkiye 2016 sonrası sabit UTC+3 kullanıyor olsa da) doğru şekilde uyar ve ileride
/// farklı havalimanları eklenirse her zaman diliminde doğru çalışır.
/// </summary>
public static class TimeZoneHelper
{
    private static readonly Lazy<TimeZoneInfo> IstanbulLazy = new(() => ResolveTimeZone("Europe/Istanbul"));

    public static TimeZoneInfo Istanbul => IstanbulLazy.Value;

    /// <summary>
    /// Linux/macOS'ta IANA kimliğiyle, Windows'ta ise gerekirse eşleşen Windows kimliğiyle
    /// zaman dilimini bulur. .NET 6+ TimeZoneInfo.FindSystemTimeZoneById IANA kimliklerini
    /// Windows'ta da otomatik olarak ICU üzerinden çözebilir.
    /// </summary>
    public static TimeZoneInfo ResolveTimeZone(string ianaId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ianaId);
        }
        catch (TimeZoneNotFoundException)
        {
            // Geri düşüş: bilinen sabit ofsetli özel zaman dilimi (yalnızca acil durum).
            return TimeZoneInfo.CreateCustomTimeZone(ianaId, TimeSpan.FromHours(3), ianaId, ianaId);
        }
    }

    /// <summary>Verilen yerel tarih+saati, belirtilen zaman diliminden UTC'ye çevirir.</summary>
    public static DateTime LocalToUtc(DateOnly date, TimeOnly time, TimeZoneInfo timeZone)
    {
        var unspecified = date.ToDateTime(time, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone);
    }

    /// <summary>Verilen UTC zamanı, belirtilen zaman dilimindeki yerel zamana çevirir.</summary>
    public static DateTime UtcToLocal(DateTime utc, TimeZoneInfo timeZone)
    {
        var kindUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(kindUtc, timeZone);
    }

    /// <summary>Erzurum yerel saatine göre "bugün"ün tarihini döndürür.</summary>
    public static DateOnly TodayInIstanbul(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var local = UtcToLocal(now, Istanbul);
        return DateOnly.FromDateTime(local);
    }
}
