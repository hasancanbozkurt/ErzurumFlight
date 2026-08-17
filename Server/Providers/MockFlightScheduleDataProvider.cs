using ErzurumFlight.Server.Models;

namespace ErzurumFlight.Server.Providers;

/// <summary>
/// Gerçek bir API anahtarı yokken kullanılan, AÇIKÇA SAHTE geliştirme verisi sağlayıcısı.
/// appsettings "FlightData:Provider": "Mock" iken devrededir. Üretilen uçuşlar veritabanında
/// IsVerified=false ve kaynak adı "(TEST)" ibaresiyle işaretlenir; production'da asla gerçek
/// veri gibi gösterilmez. İptal edilmiş bir uçuş örneği de içerir (UI'daki iptal davranışını
/// gerçek API'ye bağlanmadan test edebilmeniz için).
/// </summary>
public class MockFlightScheduleDataProvider : IFlightScheduleDataProvider
{
    public string SourceName => "Mock Tarife Kaynağı (TEST)";

    public Task<IReadOnlyList<AirportFlightRecord>> GetAirportFlightsAsync(
        string airportIcao, string airportIata, DateTime fromLocal, DateTime toLocal, CancellationToken ct = default)
    {
        var baseDate = fromLocal.Date;

        IReadOnlyList<AirportFlightRecord> records = new List<AirportFlightRecord>
        {
            new("TK2802", "Türk Hava Yolları (TEST)", "TK", "THY", "IST", "LTFM", "İstanbul Havalimanı (TEST)", 41.2753, 28.7519, "Europe/Istanbul",
                FlightDirection.Departure, baseDate.AddHours(7), null, null, "Expected", "A321neo", "TC-TEST"),

            new("TK2801", "Türk Hava Yolları (TEST)", "TK", "THY", "IST", "LTFM", "İstanbul Havalimanı (TEST)", 41.2753, 28.7519, "Europe/Istanbul",
                FlightDirection.Arrival, baseDate.AddHours(19).AddMinutes(20), null, null, "Expected", "A321neo", "TC-TEST"),

            // İptal senaryosu demo amaçlıdır.
            new("VF1476", "AJet (TEST)", "VF", "TKJ", "SAW", "LTFJ", "Sabiha Gökçen Havalimanı (TEST)", 40.8986, 29.3092, "Europe/Istanbul",
                FlightDirection.Departure, baseDate.AddHours(9).AddMinutes(15), null, null, "Canceled", "B737-800", null),
        };

        return Task.FromResult(records);
    }
}
