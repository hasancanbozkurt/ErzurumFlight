using ErzurumFlight.Server.Data;
using ErzurumFlight.Server.DTOs;
using ErzurumFlight.Server.Models;
using ErzurumFlight.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErzurumFlight.Server.Controllers;

/// <summary>
/// Admin dışındaki kullanıcılar bu endpoint'lere erişemez ([Authorize] tüm controller'ı korur).
/// Şartname bölüm 19: tarifeleri görme/ekleme/düzenleme/devre dışı bırakma/doğrulama,
/// veri kaynaklarını görme, son güncellemeleri ve hataları görme.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly FlightDbContext _db;
    private readonly IDataSourceService _dataSourceService;

    public AdminController(FlightDbContext db, IDataSourceService dataSourceService)
    {
        _db = db;
        _dataSourceService = dataSourceService;
    }

    [HttpGet("schedules")]
    public async Task<IActionResult> GetSchedules(CancellationToken ct)
    {
        var schedules = await _db.FlightSchedules
            .Include(s => s.Airline)
            .Include(s => s.OriginAirport)
            .Include(s => s.DestinationAirport)
            .Include(s => s.Source)
            .OrderBy(s => s.FlightNumber)
            .ToListAsync(ct);

        return Ok(schedules.Select(s => new
        {
            s.Id,
            s.FlightNumber,
            Airline = s.Airline?.Name,
            Origin = s.OriginAirport?.IataCode,
            Destination = s.DestinationAirport?.IataCode,
            s.DepartureLocalTime,
            s.ArrivalLocalTime,
            Days = new { s.Monday, s.Tuesday, s.Wednesday, s.Thursday, s.Friday, s.Saturday, s.Sunday },
            s.ValidFrom,
            s.ValidTo,
            Source = s.Source?.Name,
            s.IsVerified,
            s.IsActive,
            s.Notes
        }));
    }

    [HttpPost("schedules")]
    public async Task<IActionResult> UpsertSchedule([FromBody] UpsertScheduleRequest request, CancellationToken ct)
    {
        FlightSchedule schedule;

        if (request.Id.HasValue)
        {
            var existing = await _db.FlightSchedules.FindAsync(new object?[] { request.Id.Value }, ct);
            if (existing is null)
            {
                return NotFound();
            }
            schedule = existing;
        }
        else
        {
            schedule = new FlightSchedule();
            _db.FlightSchedules.Add(schedule);
        }

        schedule.AirlineId = request.AirlineId;
        schedule.FlightNumber = request.FlightNumber;
        schedule.OriginAirportId = request.OriginAirportId;
        schedule.DestinationAirportId = request.DestinationAirportId;
        schedule.DepartureLocalTime = request.DepartureLocalTime;
        schedule.ArrivalLocalTime = request.ArrivalLocalTime;
        schedule.Monday = request.Monday;
        schedule.Tuesday = request.Tuesday;
        schedule.Wednesday = request.Wednesday;
        schedule.Thursday = request.Thursday;
        schedule.Friday = request.Friday;
        schedule.Saturday = request.Saturday;
        schedule.Sunday = request.Sunday;
        schedule.ValidFrom = request.ValidFrom;
        schedule.ValidTo = request.ValidTo;
        schedule.SourceId = request.SourceId;
        schedule.IsVerified = request.IsVerified;
        schedule.LastVerifiedUtc = request.IsVerified ? DateTime.UtcNow : schedule.LastVerifiedUtc;
        schedule.Notes = request.Notes;

        await _db.SaveChangesAsync(ct);
        return Ok(new { schedule.Id });
    }

    [HttpPost("schedules/{id:int}/disable")]
    public async Task<IActionResult> DisableSchedule(int id, CancellationToken ct)
    {
        var schedule = await _db.FlightSchedules.FindAsync(new object?[] { id }, ct);
        if (schedule is null) return NotFound();

        schedule.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("schedules/{id:int}/verify")]
    public async Task<IActionResult> VerifySchedule(int id, CancellationToken ct)
    {
        var schedule = await _db.FlightSchedules.FindAsync(new object?[] { id }, ct);
        if (schedule is null) return NotFound();

        schedule.IsVerified = true;
        schedule.LastVerifiedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpGet("sources")]
    public async Task<IActionResult> GetSources(CancellationToken ct)
    {
        var sources = await _dataSourceService.GetAllAsync(ct);
        return Ok(sources);
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetRecentLogSummary(CancellationToken ct)
    {
        // İlk sürümde basit tutulur: veri kaynaklarının son hata/başarı özetini "log" olarak sunar.
        // Gerekirse Serilog eklenip dosya/veritabanı tabanlı gerçek log sorgusu ile değiştirilebilir.
        var sources = await _dataSourceService.GetAllAsync(ct);
        return Ok(sources.Where(s => s.LastError != null).Select(s => new
        {
            s.Name,
            s.LastFailureUtc,
            s.LastError
        }));
    }
}
