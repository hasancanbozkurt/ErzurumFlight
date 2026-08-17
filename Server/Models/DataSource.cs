using System.ComponentModel.DataAnnotations;

namespace ErzurumFlight.Server.Models;

/// <summary>
/// Tarife veya canlı veri sağlayan bir kaynağın tanımı (resmi kaynak, açık veri, scraper, admin, vb.).
/// Rate limit ve sağlık durumu bilgilerini de tutar.
/// </summary>
public class DataSource
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DataSourceType Type { get; set; }

    [MaxLength(500)]
    public string? BaseUrl { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>Düşük sayı = yüksek öncelik. Aynı bilgi birden fazla kaynaktan gelirse öncelik sırasına göre seçilir.</summary>
    public int Priority { get; set; } = 100;

    public int? DailyLimit { get; set; }
    public double? RequestsPerSecond { get; set; }

    public DateTime? LastSuccessUtc { get; set; }
    public DateTime? LastFailureUtc { get; set; }

    [MaxLength(2000)]
    public string? LastError { get; set; }

    [MaxLength(500)]
    public string? TermsUrl { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
