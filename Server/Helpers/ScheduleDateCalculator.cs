using ErzurumFlight.Server.Models;

namespace ErzurumFlight.Server.Helpers;

/// <summary>
/// FlightSchedule şablonundan, belirtilen tarih aralığında hangi tarihlerde uçuşun
/// gerçekleşmesi gerektiğini hesaplayan saf (side-effect'siz) mantık.
/// Akış: FlightSchedule → tarih aralığı → haftanın günü kontrolü → ValidFrom/ValidTo → tarih listesi.
/// </summary>
public static class ScheduleDateCalculator
{
    /// <summary>
    /// [fromDate, toDate] kapalı aralığında, tarifenin RunsOn() ve ValidFrom/ValidTo
    /// kısıtlarına uyan tüm tarihleri döndürür. Aralık ters ise boş liste döner.
    /// </summary>
    public static IReadOnlyList<DateOnly> GenerateDates(FlightSchedule schedule, DateOnly fromDate, DateOnly toDate)
    {
        if (toDate < fromDate)
        {
            return Array.Empty<DateOnly>();
        }

        // Etkin aralığı tarifenin ValidFrom/ValidTo sınırlarıyla kesiştir.
        var effectiveFrom = fromDate < schedule.ValidFrom ? schedule.ValidFrom : fromDate;
        var effectiveTo = schedule.ValidTo.HasValue && toDate > schedule.ValidTo.Value
            ? schedule.ValidTo.Value
            : toDate;

        if (effectiveTo < effectiveFrom)
        {
            return Array.Empty<DateOnly>();
        }

        var result = new List<DateOnly>();
        for (var date = effectiveFrom; date <= effectiveTo; date = date.AddDays(1))
        {
            if (schedule.RunsOn(date.DayOfWeek))
            {
                result.Add(date);
            }
        }

        return result;
    }

    /// <summary>Kısa yol seçenekleri ("Bugün", "Yarın", "3 Gün" ...) için [from,to] tarih aralığı üretir.</summary>
    public static (DateOnly From, DateOnly To) ResolveRangeShortcut(string shortcut, DateOnly today)
    {
        return shortcut.Trim().ToLowerInvariant() switch
        {
            "today" or "bugün" => (today, today),
            "tomorrow" or "yarın" => (today.AddDays(1), today.AddDays(1)),
            "3d" or "3gun" or "3gün" => (today, today.AddDays(2)),
            "7d" or "7gun" or "7gün" => (today, today.AddDays(6)),
            "14d" or "14gun" or "14gün" => (today, today.AddDays(13)),
            "30d" or "30gun" or "30gün" => (today, today.AddDays(29)),
            _ => (today, today)
        };
    }
}
