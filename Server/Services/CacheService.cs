using Microsoft.Extensions.Caching.Memory;

namespace ErzurumFlight.Server.Services;

/// <summary>
/// IMemoryCache üzerine ince bir sarmalayıcı. İlk sürümde Redis kullanılmaz;
/// 100 kullanıcı olsa bile dış canlı veri kaynağına tek kontrollü akışla erişilmesini sağlar
/// (kullanıcı başına API çağrısı yapılmaz, tüm istekler bu cache'den beslenir).
/// </summary>
public interface ICacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan expiration);
    void Remove(string key);
}

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public T? Get<T>(string key) => _cache.TryGetValue(key, out T? value) ? value : default;

    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        _cache.Set(key, value, expiration);
    }

    public void Remove(string key) => _cache.Remove(key);
}
