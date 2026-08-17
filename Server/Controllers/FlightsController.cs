using ErzurumFlight.Server.Models;
using ErzurumFlight.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErzurumFlight.Server.Controllers;

/// <summary>
/// Şartname bölüm 8 API'lerini uygular:
/// GET /api/flights?date=...&amp;direction=departure|arrival
/// GET /api/flights/upcoming?days=7
/// GET /api/flights/{id}
/// </summary>
[ApiController]
[Route("api/flights")]
public class FlightsController : ControllerBase
{
    private readonly IFlightService _flightService;

    public FlightsController(IFlightService flightService)
    {
        _flightService = flightService;
    }

    // RESTful: GET /api/flights/2026-08-16/departure
    [HttpGet("{date}/{direction}")]
    public async Task<IActionResult> GetFlightsByDate(DateOnly date, string direction, CancellationToken ct)
    {
        if (!Enum.TryParse<FlightDirection>(direction, ignoreCase: true, out var parsedDirection))
        {
            return BadRequest(new { error = "direction parametresi 'departure' veya 'arrival' olmalı." });
        }

        var flights = await _flightService.GetFlightsAsync(date, parsedDirection, ct);
        return Ok(flights);
    }

    // Query string: GET /api/flights?date=2026-08-16&direction=departure (eski uyumluluk)
    [HttpGet]
    public async Task<IActionResult> GetFlights([FromQuery] DateOnly? date, [FromQuery] string direction, CancellationToken ct)
    {
        if (!Enum.TryParse<FlightDirection>(direction, ignoreCase: true, out var parsedDirection))
        {
            return BadRequest(new { error = "direction parametresi 'departure' veya 'arrival' olmalı." });
        }

        var targetDate = date ?? Helpers.TimeZoneHelper.TodayInIstanbul();
        var flights = await _flightService.GetFlightsAsync(targetDate, parsedDirection, ct);
        return Ok(flights);
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] int days = 7, CancellationToken ct = default)
    {
        if (days < 1 || days > 30)
        {
            return BadRequest(new { error = "days 1 ile 30 arasında olmalı." });
        }

        var flights = await _flightService.GetUpcomingAsync(days, ct);
        return Ok(flights);
    }

    [HttpGet("counts")]
    public async Task<IActionResult> GetDailyCounts([FromQuery] DateOnly? date, CancellationToken ct = default)
    {
        var targetDate = date ?? Helpers.TimeZoneHelper.TodayInIstanbul();
        var counts = await _flightService.GetDailyCountsAsync(targetDate, ct);
        return Ok(counts);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
    {
        var detail = await _flightService.GetFlightDetailAsync(id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }
}
