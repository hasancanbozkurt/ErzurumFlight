using ErzurumFlight.Server.DTOs;
using ErzurumFlight.Server.Hubs;
using ErzurumFlight.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace ErzurumFlight.Server.Background;

/// <summary>
/// Şartname bölüm 14 akışını uygular:
/// 1. Aktif uçuşları bul  2. İzlenecek uçuşları belirle  3. Canlı ADS-B verisini çek
/// 4. Uçakları normalize et  5. Uçuş eşleştirmesi yap  6. FlightOperation güncelle
/// 7. AircraftPosition kaydet  8. Cache güncelle  9. SignalR event gönder.
/// Adım 3-7 LiveTrackingService.RefreshAsync içinde yapılır; bu worker periyodik tetikleme
/// ve SignalR yayınından sorumludur. Kullanıcı sayısından bağımsız TEK kontrollü dış API akışı sağlar.
/// </summary>
public class LiveTrackingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<FlightHub> _hubContext;
    private readonly IConfiguration _config;
    private readonly ILogger<LiveTrackingWorker> _logger;

    public LiveTrackingWorker(
        IServiceScopeFactory scopeFactory,
        IHubContext<FlightHub> hubContext,
        IConfiguration config,
        ILogger<LiveTrackingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _config.GetValue<bool?>("FlightTracking:Enabled") ?? true;
        if (!enabled)
        {
            _logger.LogInformation("LiveTrackingWorker devre dışı (FlightTracking:Enabled=false).");
            return;
        }

        var pollingSeconds = _config.GetValue<double?>("FlightTracking:PollingSeconds") ?? 30;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var liveService = scope.ServiceProvider.GetRequiredService<ILiveTrackingService>();

                var result = await liveService.RefreshAsync(stoppingToken);

                foreach (var evt in result.StatusChanges)
                {
                    await _hubContext.Clients.Group("erzurum").SendAsync("FlightStatusChanged", evt, stoppingToken);

                    if (evt.Status == nameof(Models.FlightStatus.Departed))
                    {
                        await _hubContext.Clients.Group("erzurum").SendAsync("FlightDeparted", evt, stoppingToken);
                    }
                    else if (evt.Status == nameof(Models.FlightStatus.Landed))
                    {
                        await _hubContext.Clients.Group("erzurum").SendAsync("FlightLanded", evt, stoppingToken);
                    }
                }

                foreach (var evt in result.PositionUpdates)
                {
                    await _hubContext.Clients.Group("erzurum").SendAsync("FlightPositionUpdated", evt, stoppingToken);
                }

                _logger.LogDebug("LiveTrackingWorker: {Count} uçak gözlemlendi, {StatusCount} durum değişikliği.",
                    result.AircraftObserved, result.StatusChanges.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "LiveTrackingWorker çalışırken beklenmeyen hata oluştu.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(pollingSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
