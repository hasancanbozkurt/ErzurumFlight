using ErzurumFlight.Server.Helpers;
using ErzurumFlight.Server.Hubs;
using ErzurumFlight.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace ErzurumFlight.Server.Background;

/// <summary>
/// Gerçek dış kaynaktan (AeroDataBox, appsettings "FlightData:RapidApiKey") periyodik olarak
/// Erzurum'un GÜNCEL/YAKIN kalkış-varış listesini çeker ve FlightInstance tablosunu günceller.
/// Bu, uygulamanın sahip olduğu TEK canlı tarife/durum kaynağıdır — iptal, gecikme ve gerçek
/// saat bilgisi buradan gelir; değişiklikler SignalR ile anında tüm istemcilere yayılır.
///
/// Pencere: appsettings "FlightData:WindowHours" (varsayılan 48) — yani "şu an - 6 saat" ile
/// "şu an + 48 saat" arası sorgulanır. Bu sınırlama BİLİNÇLİDİR: hiçbir kaynak (ücretli dahil)
/// 30 gün sonrasının iptal/gecikme bilgisini veremez, çünkü havayolları bu kararı uçuş gününe
/// yakın alır. Uzak gelecek tarihler için ScheduleService'in ürettiği FlightSchedule tabanlı
/// tarife (Data/SeedData.cs) kullanılmaya devam eder; bir uçuş bu pencereye girdiğinde
/// otomatik olarak bu worker tarafından doğrulanıp güncellenir (IsVerified=true olur).
/// </summary>
public class ScheduleSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<FlightHub> _hubContext;
    private readonly IConfiguration _config;
    private readonly ILogger<ScheduleSyncWorker> _logger;

    public ScheduleSyncWorker(
        IServiceScopeFactory scopeFactory,
        IHubContext<FlightHub> hubContext,
        IConfiguration config,
        ILogger<ScheduleSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _config.GetValue<int?>("FlightData:SyncIntervalMinutes") ?? 180;
        // AeroDataBox max 12 saat pencereye izin veriyor
        var windowHours = 12;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IScheduleSyncService>();

                var nowLocal = TimeZoneHelper.UtcToLocal(DateTime.UtcNow, TimeZoneHelper.Istanbul);
                var fromLocal = nowLocal;
                var toLocal = nowLocal.AddHours(windowHours);

                var result = await syncService.SyncWindowAsync(fromLocal, toLocal, stoppingToken);

                _logger.LogInformation(
                    "ScheduleSyncWorker: {Fetched} kayıt çekildi, {Created} yeni, {Updated} güncellendi, {Failed} hata.",
                    result.Fetched, result.Created, result.Updated, result.Failed);

                // Her durum değişikliği için ayrı SignalR olayı: Dashboard/Detay sayfası anında güncellenir.
                // İptal olan uçuşlar için ayrıca "FlightCancelled" olayı da yayınlanır — kullanıcı bunun
                // için özel bir bildirim/uyarı göstermek isteyebilir (bkz. ClientApp useFlightHub hook'u).
                foreach (var evt in result.StatusChanges)
                {
                    await _hubContext.Clients.Group("erzurum").SendAsync("FlightStatusChanged", evt, stoppingToken);

                    if (evt.Status == nameof(Models.FlightStatus.Cancelled))
                    {
                        await _hubContext.Clients.Group("erzurum").SendAsync("FlightCancelled", evt, stoppingToken);
                    }
                }

                if (result.Created > 0)
                {
                    // Yeni uçuş(lar) eklendi (daha önce yerel tarifede olmayan bir sefer canlı kaynakta
                    // bulundu); istemciler genel bir "tazele" sinyaliyle mevcut listeyi yeniden çeker.
                    await _hubContext.Clients.Group("erzurum").SendAsync("ScheduleSynced", new
                    {
                        created = result.Created,
                        updated = result.Updated,
                        timestampUtc = DateTime.UtcNow
                    }, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "ScheduleSyncWorker çalışırken beklenmeyen hata oluştu.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
