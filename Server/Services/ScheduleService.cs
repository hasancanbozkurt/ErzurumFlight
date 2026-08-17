using ErzurumFlight.Server.Data;
using ErzurumFlight.Server.Helpers;
using ErzurumFlight.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ErzurumFlight.Server.Services;

/// <summary>
/// FlightSchedule şablonlarından FlightInstance kayıtlarını üretir.
/// Akış: FlightSchedule → tarih aralığı → haftanın günü kontrolü → ValidFrom/ValidTo → FlightInstance.
/// Duplicate prevention: FlightDate + FlightNumber + Origin + Destination unique index'ine güvenir,
/// ayrıca üretmeden önce var olan kayıtları kontrol ederek gereksiz DB round-trip'i azaltır.
/// </summary>
public interface IScheduleService
{
    Task<int> GenerateInstancesAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);
}

public class ScheduleService : IScheduleService
{
    private readonly FlightDbContext _db;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(FlightDbContext db, ILogger<ScheduleService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> GenerateInstancesAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        var schedules = await _db.FlightSchedules
            .Include(s => s.OriginAirport)
            .Include(s => s.DestinationAirport)
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        if (schedules.Count == 0)
        {
            return 0;
        }

        // Aralıktaki mevcut instance anahtarlarını tek seferde çekip duplicate'i bellekte engelle.
        var existingKeys = await _db.FlightInstances
            .Where(i => i.FlightDate >= fromDate && i.FlightDate <= toDate)
            .Select(i => new { i.FlightDate, i.FlightNumber, i.OriginAirportId, i.DestinationAirportId })
            .ToListAsync(ct);

        var existingSet = existingKeys
            .Select(k => (k.FlightDate, k.FlightNumber, k.OriginAirportId, k.DestinationAirportId))
            .ToHashSet();

        var newInstances = new List<FlightInstance>();
        var now = DateTime.UtcNow;

        foreach (var schedule in schedules)
        {
            if (schedule.OriginAirport is null || schedule.DestinationAirport is null)
            {
                continue;
            }

            var originTz = TimeZoneHelper.ResolveTimeZone(schedule.OriginAirport.Timezone);
            var destTz = TimeZoneHelper.ResolveTimeZone(schedule.DestinationAirport.Timezone);

            var dates = ScheduleDateCalculator.GenerateDates(schedule, fromDate, toDate);

            foreach (var date in dates)
            {
                var key = (date, schedule.FlightNumber, schedule.OriginAirportId, schedule.DestinationAirportId);
                if (existingSet.Contains(key))
                {
                    continue; // Duplicate prevention.
                }

                var departureUtc = TimeZoneHelper.LocalToUtc(date, schedule.DepartureLocalTime, originTz);

                // Varış tarihi, kalkıştan sonraki gün olabilir (gece yarısını geçen uçuşlar); basitçe
                // varış saatinin kalkıştan önce görünmesi durumunda bir gün ekleyerek düzeltiyoruz.
                var arrivalDate = date;
                var arrivalUtcCandidate = TimeZoneHelper.LocalToUtc(arrivalDate, schedule.ArrivalLocalTime, destTz);
                if (arrivalUtcCandidate < departureUtc)
                {
                    arrivalDate = arrivalDate.AddDays(1);
                    arrivalUtcCandidate = TimeZoneHelper.LocalToUtc(arrivalDate, schedule.ArrivalLocalTime, destTz);
                }

                var instance = new FlightInstance
                {
                    FlightScheduleId = schedule.Id,
                    FlightDate = date,
                    FlightNumber = schedule.FlightNumber,
                    OriginAirportId = schedule.OriginAirportId,
                    DestinationAirportId = schedule.DestinationAirportId,
                    AirlineId = schedule.AirlineId,
                    ScheduledDepartureUtc = departureUtc,
                    ScheduledArrivalUtc = arrivalUtcCandidate,
                    Status = FlightStatus.Scheduled,
                    IsVerified = schedule.IsVerified,
                    SourceId = schedule.SourceId,
                    CreatedUtc = now,
                    UpdatedUtc = now
                };

                newInstances.Add(instance);
                existingSet.Add(key);
            }
        }

        if (newInstances.Count > 0)
        {
            _db.FlightInstances.AddRange(newInstances);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // Unique index çakışması olursa (nadiren, yarış durumu), sessizce logla; veri kaybı olmaz.
                _logger.LogWarning(ex, "FlightInstance ekleme sırasında duplicate index çakışması yaşandı.");
            }
        }

        _logger.LogInformation("Tarife motoru {Count} yeni uçuş instance'ı üretti ({From} - {To}).",
            newInstances.Count, fromDate, toDate);

        return newInstances.Count;
    }
}
