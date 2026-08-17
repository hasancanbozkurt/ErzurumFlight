using ErzurumFlight.Server.Data;
using ErzurumFlight.Server.DTOs;
using ErzurumFlight.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ErzurumFlight.Server.Services;

/// <summary>
/// Veri kaynaklarının (DataSource) sağlık durumunu takip eder. Dış kaynak çalışmazsa
/// uygulama çökmez; son başarılı/başarısız zaman damgaları ve hata mesajı burada tutulur.
/// </summary>
public interface IDataSourceService
{
    Task RecordSuccessAsync(string sourceName, CancellationToken ct = default);
    Task RecordFailureAsync(string sourceName, string error, CancellationToken ct = default);
    Task<IReadOnlyList<DataSourceStatusDto>> GetAllAsync(CancellationToken ct = default);
    Task<DataSource?> GetBySourceNameAsync(string name, CancellationToken ct = default);
}

public class DataSourceService : IDataSourceService
{
    private readonly FlightDbContext _db;
    private readonly ILogger<DataSourceService> _logger;

    public DataSourceService(FlightDbContext db, ILogger<DataSourceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RecordSuccessAsync(string sourceName, CancellationToken ct = default)
    {
        var source = await _db.DataSources.FirstOrDefaultAsync(s => s.Name == sourceName, ct);
        if (source is null) return;

        source.LastSuccessUtc = DateTime.UtcNow;
        source.LastError = null;
        await _db.SaveChangesAsync(ct);
    }

    public async Task RecordFailureAsync(string sourceName, string error, CancellationToken ct = default)
    {
        var source = await _db.DataSources.FirstOrDefaultAsync(s => s.Name == sourceName, ct);
        if (source is null) return;

        source.LastFailureUtc = DateTime.UtcNow;
        source.LastError = error.Length > 2000 ? error[..2000] : error;
        await _db.SaveChangesAsync(ct);

        _logger.LogWarning("Veri kaynağı hatası [{Source}]: {Error}", sourceName, error);
    }

    public async Task<IReadOnlyList<DataSourceStatusDto>> GetAllAsync(CancellationToken ct = default)
    {
        var sources = await _db.DataSources.OrderBy(s => s.Priority).ToListAsync(ct);
        return sources.Select(s => new DataSourceStatusDto(
            s.Id, s.Name, s.Type.ToString(), s.IsEnabled, s.Priority, s.LastSuccessUtc, s.LastFailureUtc, s.LastError
        )).ToList();
    }

    public Task<DataSource?> GetBySourceNameAsync(string name, CancellationToken ct = default) =>
        _db.DataSources.FirstOrDefaultAsync(s => s.Name == name, ct);
}
