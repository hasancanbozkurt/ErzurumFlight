using ErzurumFlight.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErzurumFlight.Server.Controllers;

/// <summary>
/// Canlı uçak verisini yalnızca MemoryCache üzerinden sunar; dış API'ye (Airplanes.Live)
/// asla doğrudan istek atmaz (şartname bölüm 12: "Frontend doğrudan çağırmayacak").
/// Gerçek zamanlı akış SignalR /hubs/flights üzerinden yapılır; bu uç ilk yükleme/polling geri düşüşü içindir.
/// </summary>
[ApiController]
[Route("api/live")]
public class LiveController : ControllerBase
{
    private readonly ILiveTrackingService _liveTrackingService;

    public LiveController(ILiveTrackingService liveTrackingService)
    {
        _liveTrackingService = liveTrackingService;
    }

    [HttpGet("aircraft")]
    public IActionResult GetAircraft()
    {
        var aircraft = _liveTrackingService.GetCachedAircraft();
        return Ok(aircraft);
    }
}
