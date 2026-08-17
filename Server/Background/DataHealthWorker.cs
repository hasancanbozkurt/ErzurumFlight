using ErzurumFlight.Server.Data;
using ErzurumFlight.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ErzurumFlight.Server.Background;

/// <summary>
/// Periyodik olarak veri kaynaklarının sağlığını kontrol eder: uzun süredir başarılı güncelleme
/// almayan AKTİF OLARAK ÇEKİLEN kaynakları loglar. /health endpoint'inin arkasındaki veriyi
/// güncel tutar. "ManualAdmin" tipi kaynaklar (örn. "Varsayılan Tarife (Seed)") kasıtlı olarak
/// hariç tutulur — bunlar hiçbir zaman otomatik çekilmez, bu yüzden LastSuccessUtc her zaman
/// null'dır ve bu normaldir; dahil edilselerdi her turda yanlış pozitif bir uyarı basılırdı.
/// </summary>
public class DataHealthWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataHealthWorker> _logger;

    public DataHealthWorker(IServiceScopeFactory scopeFactory, ILogger<DataHealthWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FlightDbContext>();

                var staleThreshold = DateTime.UtcNow.AddHours(-2);
                var staleSources = await db.DataSources
                    .Where(s => s.IsEnabled && s.Type != DataSourceType.ManualAdmin)
                    .Where(s => s.LastSuccessUtc == null || s.LastSuccessUtc < staleThreshold)
                    .Select(s => s.Name)
                    .ToListAsync(stoppingToken);

                if (staleSources.Count > 0)
                {
                    _logger.LogWarning("Şu veri kaynakları 2 saatten uzun süredir başarılı güncelleme almadı: {Sources}",
                        string.Join(", ", staleSources));
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "DataHealthWorker çalışırken hata oluştu.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
