using ErzurumFlight.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErzurumFlight.Server.Controllers;

/// <summary>
/// GET /api/calendar?from=2026-08-10&amp;to=2026-08-30
/// Gün bazlı doğrulama durumunu (tarife doğrulandı / doğrulanmadı) döndürür.
/// </summary>
[ApiController]
[Route("api/calendar")]
public class CalendarController : ControllerBase
{
    private readonly IFlightService _flightService;

    public CalendarController(IFlightService flightService)
    {
        _flightService = flightService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCalendar([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (to < from)
        {
            return BadRequest(new { error = "'to' tarihi 'from' tarihinden önce olamaz." });
        }

        if ((to.ToDateTime(TimeOnly.MinValue) - from.ToDateTime(TimeOnly.MinValue)).TotalDays > 120)
        {
            return BadRequest(new { error = "Tarih aralığı en fazla 120 gün olabilir." });
        }

        var days = await _flightService.GetCalendarAsync(from, to, ct);
        return Ok(days);
    }
}
