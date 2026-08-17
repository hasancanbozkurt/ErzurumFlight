using ErzurumFlight.Server.Data;
using ErzurumFlight.Server.DTOs;
using ErzurumFlight.Server.Helpers;
using ErzurumFlight.Server.Models;
using ErzurumFlight.Server.Providers;
using Microsoft.EntityFrameworkCore;

namespace ErzurumFlight.Server.Services;

public record ScheduleSyncResult(int Fetched, int Created, int Updated, int Failed, IReadOnlyList<FlightStatusChangedEvent> StatusChanges);

/// <summary>
/// Gerçek dış kaynaktan (IFlightScheduleDataProvider) Erzurum'un kalkış/varış listesini çeker
/// ve FlightInstance tablosuna upsert eder. Bu, uygulamanın TEK gerçek tarife kaynağıdır —
/// admin panelinden manuel giriş artık zorunlu değil, isteğe bağlı bir istisna mekanizmasıdır.
///
/// Akış: Provider → AirportFlightRecord (normalize) → Airport/Airline otomatik keşif/oluşturma
/// → FlightInstance upsert (FlightNumber+Tarih+Yön ile eşleştirilir) → durum güncelle
/// (iptal/gecikme dahil) → tahmini saatleri FlightOperation'a yaz.
/// </summary>
public interface IScheduleSyncService
{
    Task<ScheduleSyncResult> SyncWindowAsync(DateTime fromLocalErz, DateTime toLocalErz, CancellationToken ct = default);
}

public class ScheduleSyncService : IScheduleSyncService
{
    private readonly FlightDbContext _db;
    private readonly IFlightScheduleDataProvider _provider;
    private readonly IDataSourceService _dataSourceService;
    private readonly IConfiguration _config;
    private readonly ILogger<ScheduleSyncService> _logger;

    public ScheduleSyncService(
        FlightDbContext db,
        IFlightScheduleDataProvider provider,
        IDataSourceService dataSourceService,
        IConfiguration config,
        ILogger<ScheduleSyncService> logger)
    {
        _db = db;
        _provider = provider;
        _dataSourceService = dataSourceService;
        _config = config;
        _logger = logger;
    }

    public async Task<ScheduleSyncResult> SyncWindowAsync(DateTime fromLocalErz, DateTime toLocalErz, CancellationToken ct = default)
    {
        var erzurumIata = _config["Airport:Iata"] ?? SeedData.ErzurumIata;
        var erzurumIcao = _config["Airport:Icao"] ?? SeedData.ErzurumIcao;

        var erzurum = await _db.Airports.FirstOrDefaultAsync(a => a.IataCode == erzurumIata, ct);
        if (erzurum is null)
        {
            _logger.LogWarning("Erzurum havalimanı veritabanında bulunamadı; senkronizasyon atlanıyor.");
            return new ScheduleSyncResult(0, 0, 0, 0, Array.Empty<FlightStatusChangedEvent>());
        }

        IReadOnlyList<AirportFlightRecord> records;
        try
        {
            records = await _provider.GetAirportFlightsAsync(erzurumIcao, erzurumIata, fromLocalErz, toLocalErz, ct);
            await _dataSourceService.RecordSuccessAsync(_provider.SourceName, ct);
        }
        catch (Exception ex)
        {
            // Dış kaynak çalışmazsa uygulama çökmez; veritabanındaki son bilinen veri korunur.
            await _dataSourceService.RecordFailureAsync(_provider.SourceName, ex.Message, ct);
            _logger.LogWarning(ex, "{Provider} senkronizasyonu başarısız, önceki veri korunuyor.", _provider.SourceName);
            return new ScheduleSyncResult(0, 0, 0, 0, Array.Empty<FlightStatusChangedEvent>());
        }

        var source = await _dataSourceService.GetBySourceNameAsync(_provider.SourceName, ct);
        if (source is null)
        {
            source = new DataSource { Name = _provider.SourceName, Type = DataSourceType.OpenData, IsEnabled = true, Priority = 10 };
            _db.DataSources.Add(source);
            await _db.SaveChangesAsync(ct);
        }

        int created = 0, updated = 0, failed = 0;
        var statusChanges = new List<FlightStatusChangedEvent>();

        foreach (var record in records)
        {
            try
            {
                var (outcome, changeEvent) = await UpsertAsync(record, erzurum, source, ct);
                if (outcome == UpsertOutcome.Created) created++;
                else if (outcome == UpsertOutcome.Updated) updated++;

                if (changeEvent is not null)
                {
                    statusChanges.Add(changeEvent);
                }
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "Uçuş kaydı işlenemedi: {FlightNumber}", record.FlightNumber);
            }
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "{Provider} senkronizasyonu: {Fetched} kayıt çekildi, {Created} yeni, {Updated} güncellendi, {Failed} hata.",
            _provider.SourceName, records.Count, created, updated, failed);

        return new ScheduleSyncResult(records.Count, created, updated, failed, statusChanges);
    }

    private enum UpsertOutcome { Created, Updated, Unchanged }

    private async Task<(UpsertOutcome Outcome, FlightStatusChangedEvent? ChangeEvent)> UpsertAsync(
        AirportFlightRecord record, Airport erzurum, DataSource source, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(record.OtherAirportIata) && string.IsNullOrWhiteSpace(record.OtherAirportIcao))
        {
            throw new InvalidOperationException("Kayıtta karşı havalimanı bilgisi yok, atlanıyor.");
        }

        var otherAirport = await ResolveOrCreateAirportAsync(record, ct);
        var airline = await ResolveOrCreateAirlineAsync(record, ct);

        var originAirport = record.Direction == FlightDirection.Departure ? erzurum : otherAirport;
        var destinationAirport = record.Direction == FlightDirection.Departure ? otherAirport : erzurum;

        var scheduledDepartureUtc = record.Direction == FlightDirection.Departure
            ? record.ScheduledUtc
            : InferOtherLegUtc(record, isDeparture: true);
        var scheduledArrivalUtc = record.Direction == FlightDirection.Arrival
            ? record.ScheduledUtc
            : InferOtherLegUtc(record, isDeparture: false);

        var flightDateLocal = DateOnly.FromDateTime(TimeZoneHelper.UtcToLocal(scheduledDepartureUtc, TimeZoneHelper.Istanbul));

        var existing = await _db.FlightInstances
            .Include(i => i.Operation)
            .FirstOrDefaultAsync(i =>
                i.FlightDate == flightDateLocal &&
                i.FlightNumber == record.FlightNumber &&
                i.OriginAirportId == originAirport.Id &&
                i.DestinationAirportId == destinationAirport.Id, ct);

        var mappedStatus = FlightStatusMapper.Map(record.RawStatus);
        var now = DateTime.UtcNow;

        if (existing is null)
        {
            existing = new FlightInstance
            {
                FlightNumber = record.FlightNumber,
                FlightDate = flightDateLocal,
                OriginAirport = originAirport,
                DestinationAirport = destinationAirport,
                Airline = airline,
                ScheduledDepartureUtc = scheduledDepartureUtc,
                ScheduledArrivalUtc = scheduledArrivalUtc,
                Status = mappedStatus,
                IsVerified = true, // Gerçek dış kaynaktan geldiği için doğrulanmış kabul edilir.
                Source = source,
                CreatedUtc = now,
                UpdatedUtc = now
            };
            _db.FlightInstances.Add(existing);
            ApplyOperationDetails(existing, record, now);
            // Yeni eklenen bir uçuş için "durum değişti" olayı yayınlanmaz (kullanıcı zaten görmüyordu);
            // yalnızca ScheduleSyncWorker'ın genel "ScheduleSynced" bildirimiyle listeler tazelenir.
            return (UpsertOutcome.Created, null);
        }

        var changed = existing.Status != mappedStatus;
        existing.Status = mappedStatus;
        existing.IsVerified = true;
        existing.Source = source;
        existing.Airline ??= airline;
        existing.UpdatedUtc = now;
        ApplyOperationDetails(existing, record, now);

        var changeEvent = changed
            ? new FlightStatusChangedEvent(existing.Id, existing.FlightNumber, mappedStatus.ToString(), now)
            : null;

        return (changed ? UpsertOutcome.Updated : UpsertOutcome.Unchanged, changeEvent);
    }

    /// <summary>
    /// Sağlayıcı yalnızca sorgulanan yönün (kalkış ya da varış) saatini verdiyse, karşı bacağın
    /// saatini bilmiyoruz demektir; bu durumda planlanan saatten makul bir varsayılan süre
    /// (1.5 saat) eklenerek/çıkarılarak kaba bir tahmin üretilir. Sağlayıcı her iki bacağı da
    /// döndürdüğünde (AeroDataBox genelde döndürür) gerçek süre otomatik olarak kullanılır.
    /// </summary>
    private static DateTime InferOtherLegUtc(AirportFlightRecord record, bool isDeparture)
    {
        var fallbackDuration = TimeSpan.FromMinutes(90);
        return isDeparture ? record.ScheduledUtc.Subtract(fallbackDuration) : record.ScheduledUtc.Add(fallbackDuration);
    }

    private void ApplyOperationDetails(FlightInstance instance, AirportFlightRecord record, DateTime now)
    {
        var operation = instance.Operation;
        if (operation is null)
        {
            operation = new FlightOperation { FlightInstance = instance, Status = instance.Status };
            _db.FlightOperations.Add(operation);
            instance.Operation = operation;
        }

        operation.Status = instance.Status;
        operation.LastLiveUpdateUtc = now;

        if (record.Direction == FlightDirection.Departure)
        {
            operation.EstimatedDepartureUtc = record.RevisedUtc ?? operation.EstimatedDepartureUtc;
            operation.ActualDepartureUtc = record.ActualUtc ?? operation.ActualDepartureUtc;
        }
        else
        {
            operation.EstimatedArrivalUtc = record.RevisedUtc ?? operation.EstimatedArrivalUtc;
            operation.ActualArrivalUtc = record.ActualUtc ?? operation.ActualArrivalUtc;
        }
    }

    private async Task<Airport> ResolveOrCreateAirportAsync(AirportFlightRecord record, CancellationToken ct)
    {
        Airport? airport = null;

        if (!string.IsNullOrWhiteSpace(record.OtherAirportIata))
        {
            airport = await _db.Airports.FirstOrDefaultAsync(a => a.IataCode == record.OtherAirportIata, ct);
        }
        if (airport is null && !string.IsNullOrWhiteSpace(record.OtherAirportIcao))
        {
            airport = await _db.Airports.FirstOrDefaultAsync(a => a.IcaoCode == record.OtherAirportIcao, ct);
        }

        if (airport is not null)
        {
            return airport;
        }

        // Sağlayıcı, veritabanında henüz bulunmayan bir havalimanı bildirdiyse otomatik oluştur
        // (dinamik referans verisi — elle havalimanı eklemeye gerek kalmaz).
        var fallbackCode = record.OtherAirportIcao is { Length: > 0 }
            ? record.OtherAirportIcao[..Math.Min(3, record.OtherAirportIcao.Length)]
            : "XXX";

        airport = new Airport
        {
            IataCode = record.OtherAirportIata ?? fallbackCode,
            IcaoCode = record.OtherAirportIcao ?? record.OtherAirportIata ?? "????",
            Name = record.OtherAirportName ?? record.OtherAirportIata ?? "Bilinmeyen Havalimanı",
            City = record.OtherAirportName ?? "-",
            Country = "-",
            Latitude = record.OtherAirportLat ?? 0,
            Longitude = record.OtherAirportLon ?? 0,
            Timezone = string.IsNullOrWhiteSpace(record.OtherAirportTimezone) ? "Europe/Istanbul" : record.OtherAirportTimezone,
            IsActive = true
        };
        _db.Airports.Add(airport);
        await _db.SaveChangesAsync(ct); // Id hemen gerektiği için erken kaydedilir.
        return airport;
    }

    private async Task<Airline?> ResolveOrCreateAirlineAsync(AirportFlightRecord record, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(record.AirlineName) && string.IsNullOrWhiteSpace(record.AirlineIata))
        {
            return null;
        }

        Airline? airline = null;
        if (!string.IsNullOrWhiteSpace(record.AirlineIata))
        {
            airline = await _db.Airlines.FirstOrDefaultAsync(a => a.IataCode == record.AirlineIata, ct);
        }

        if (airline is not null)
        {
            return airline;
        }

        airline = new Airline
        {
            IataCode = record.AirlineIata,
            IcaoCode = record.AirlineIcao,
            Name = record.AirlineName ?? record.AirlineIata ?? "Bilinmeyen Havayolu",
            Callsign = record.AirlineIcao
        };
        _db.Airlines.Add(airline);
        await _db.SaveChangesAsync(ct);
        return airline;
    }
}
