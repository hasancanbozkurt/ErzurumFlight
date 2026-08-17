using ErzurumFlight.Server.Helpers;
using ErzurumFlight.Server.Services;

namespace ErzurumFlight.Server.Background;

/// <summary>
/// Periyodik olarak FlightSchedule şablonlarından ileri tarihli FlightInstance kayıtlarını üretir.
/// Bu, uygulamanın UZAK GELECEK (3/7/14/30+ gün) katmanıdır — bkz. README "Gerçek canlı
/// tarife/durum nasıl açılır". SeedData açılışta bir kez çalıştırır; bu worker, gün ilerledikçe
/// 90 günlük pencerenin sürekli dolu kalmasını sağlar (appsettings "Schedule:RefreshHours",
/// varsayılan 24 saat). ScheduleSyncWorker (AeroDataBox) devreye girdiğinde, bu worker'ın
/// ürettiği kayıtların üzerine gerçek/doğrulanmış veriyle yazar — ikisi çakışmaz, tamamlayıcıdır.
/// </summary>
public class ScheduleRefreshWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly ILogger<ScheduleRefreshWorker> _logger;

    public ScheduleRefreshWorker(IServiceProvider services, IConfiguration config, ILogger<ScheduleRefreshWorker> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var refreshHours = _config.GetValue<int?>("Schedule:RefreshHours") ?? 24;
        var futureDays = _config.GetValue<int?>("Schedule:FutureDays") ?? 90;

        // SeedData açılışta zaten bir tur çalıştırdığı için burada hemen değil, ilk periyottan
        // sonra başlar (gereksiz çift üretim/duplicate-check maliyetinden kaçınmak için).
        try
        {
            await Task.Delay(TimeSpan.FromHours(refreshHours), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var scheduleService = scope.ServiceProvider.GetRequiredService<IScheduleService>();

                var today = TimeZoneHelper.TodayInIstanbul();
                var toDate = today.AddDays(futureDays);

                var created = await scheduleService.GenerateInstancesAsync(today, toDate, stoppingToken);
                _logger.LogInformation("ScheduleRefreshWorker: {Created} yeni uçuş instance'ı üretildi ({From} - {To}).",
                    created, today, toDate);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "ScheduleRefreshWorker çalışırken hata oluştu.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(refreshHours), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
