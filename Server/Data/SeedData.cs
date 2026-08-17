using ErzurumFlight.Server.Helpers;
using ErzurumFlight.Server.Models;
using ErzurumFlight.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ErzurumFlight.Server.Data;

public static class SeedData
{
    public const string ErzurumIata = "ERZ";
    public const string ErzurumIcao = "LTCE";

    public static async Task InitializeAsync(
        FlightDbContext db,
        UserManager<ApplicationUser> userManager,
        IScheduleService scheduleService,
        IConfiguration config)
    {
        await db.Database.EnsureCreatedAsync();

        // 1. Ensure DataSources exist (Idempotent)
        var seedSource = await EnsureDataSourceAsync(db, "Varsayılan Tarife (Seed)", DataSourceType.ManualAdmin, 10, "Uygulamayla birlikte gelen, gerçek rotalara dayalı varsayılan tarife. Saatler temsilidir.");
        await EnsureDataSourceAsync(db, "Airplanes.Live", DataSourceType.LiveTracking, 20, "Ücretsiz ADS-B canlı takip kaynağı. Kullanım şartları production öncesi tekrar kontrol edilmeli.", "https://api.airplanes.live", 1, 500, "https://airplanes.live/api-guide/");

        // 2. Check if Airports already seeded
        var alreadySeeded = await db.Airports.AnyAsync(a => a.IataCode == ErzurumIata);
        if (!alreadySeeded)
        {
            SeedAirportsAirlinesAndSchedules(db, seedSource);
            await db.SaveChangesAsync();
        }

        // 3. Generate instances
        var today = TimeZoneHelper.TodayInIstanbul();
        await scheduleService.GenerateInstancesAsync(today, today.AddDays(90));

        // 4. Admin user
        var adminUser = config["Seed:AdminUserName"];
        var adminPassword = config["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(adminUser) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var existing = await userManager.FindByNameAsync(adminUser);
            if (existing is null)
            {
                var user = new ApplicationUser { UserName = adminUser, DisplayName = "Yönetici" };
                await userManager.CreateAsync(user, adminPassword);
            }
        }
    }

    private static async Task<DataSource> EnsureDataSourceAsync(FlightDbContext db, string name, DataSourceType type, int priority, string notes, string baseUrl = null, int rps = 0, int limit = 0, string termsUrl = null)
    {
        var ds = await db.DataSources.FirstOrDefaultAsync(s => s.Name == name);
        if (ds == null)
        {
            ds = new DataSource
            {
                Name = name,
                Type = type,
                IsEnabled = true,
                Priority = priority,
                Notes = notes,
                BaseUrl = baseUrl,
                RequestsPerSecond = rps,
                DailyLimit = limit,
                TermsUrl = termsUrl
            };
            db.DataSources.Add(ds);
            await db.SaveChangesAsync();
        }
        return ds;
    }

    private static void SeedAirportsAirlinesAndSchedules(FlightDbContext db, DataSource seedSource)
    {
        // ---------- Havalimanları ----------
        var erz = new Airport { IataCode = "ERZ", IcaoCode = "LTCE", Name = "Erzurum Havalimanı", City = "Erzurum", Country = "Türkiye", Latitude = 39.9565, Longitude = 41.1702, Timezone = "Europe/Istanbul" };
        var ist = new Airport { IataCode = "IST", IcaoCode = "LTFM", Name = "İstanbul Havalimanı", City = "İstanbul", Country = "Türkiye", Latitude = 41.2753, Longitude = 28.7519, Timezone = "Europe/Istanbul" };
        var saw = new Airport { IataCode = "SAW", IcaoCode = "LTFJ", Name = "Sabiha Gökçen Havalimanı", City = "İstanbul", Country = "Türkiye", Latitude = 40.8986, Longitude = 29.3092, Timezone = "Europe/Istanbul" };
        var esb = new Airport { IataCode = "ESB", IcaoCode = "LTAC", Name = "Esenboğa Havalimanı", City = "Ankara", Country = "Türkiye", Latitude = 40.1281, Longitude = 32.9951, Timezone = "Europe/Istanbul" };
        var adb = new Airport { IataCode = "ADB", IcaoCode = "LTBJ", Name = "Adnan Menderes Havalimanı", City = "İzmir", Country = "Türkiye", Latitude = 38.2924, Longitude = 27.1570, Timezone = "Europe/Istanbul" };
        db.Airports.AddRange(erz, ist, saw, esb, adb);

        // ---------- Havayolları ----------
        var thy = new Airline { IataCode = "TK", IcaoCode = "THY", Name = "Türk Hava Yolları", Callsign = "THY" };
        var ajet = new Airline { IataCode = "VF", IcaoCode = "TKJ", Name = "AJet", Callsign = "AJET" };
        var pegasus = new Airline { IataCode = "PC", IcaoCode = "PGT", Name = "Pegasus Hava Yolları", Callsign = "PEGASUS" };
        var sunexpress = new Airline { IataCode = "XQ", IcaoCode = "SXS", Name = "SunExpress", Callsign = "SUNEXPRESS" };
        db.Airlines.AddRange(thy, ajet, pegasus, sunexpress);

        var today = TimeZoneHelper.TodayInIstanbul();
        var validFrom = today;
        var validTo = today.AddYears(1);

        const bool M = true, T = true, W = true, Th = true, F = true, Sa = true, Su = true;
        const bool _ = false;

        AddSchedule(db, thy, "TK2802", erz, ist, new(7, 0), new(8, 40), M, T, W, Th, F, Sa, Su, validFrom, validTo, seedSource);
        AddSchedule(db, thy, "TK2801", ist, erz, new(17, 40), new(19, 20), M, T, W, Th, F, Sa, Su, validFrom, validTo, seedSource);

        AddSchedule(db, ajet, "VF1476", erz, saw, new(9, 15), new(11, 5), M, T, W, Th, F, Sa, Su, validFrom, validTo, seedSource);
        AddSchedule(db, ajet, "VF1475", saw, erz, new(20, 30), new(22, 20), M, T, W, Th, F, Sa, Su, validFrom, validTo, seedSource);

        AddSchedule(db, ajet, "VF2384", erz, esb, new(13, 20), new(14, 40), M, _, W, _, F, _, Su, validFrom, validTo, seedSource);
        AddSchedule(db, ajet, "VF2383", esb, erz, new(11, 20), new(12, 40), M, _, W, _, F, _, Su, validFrom, validTo, seedSource);

        AddSchedule(db, pegasus, "PC2705", erz, saw, new(10, 40), new(12, 30), M, T, W, Th, F, Sa, Su, validFrom, validTo, seedSource);
        AddSchedule(db, pegasus, "PC2706", saw, erz, new(8, 0), new(9, 50), M, T, W, Th, F, Sa, Su, validFrom, validTo, seedSource);

        AddSchedule(db, sunexpress, "XQ3512", erz, adb, new(12, 0), new(13, 55), _, T, _, Th, _, Sa, _, validFrom, validTo, seedSource);
        AddSchedule(db, sunexpress, "XQ3511", adb, erz, new(9, 30), new(11, 25), _, T, _, Th, _, Sa, _, validFrom, validTo, seedSource);
    }

    private static void AddSchedule(
        FlightDbContext db, Airline airline, string flightNumber, Airport origin, Airport destination,
        TimeOnly departure, TimeOnly arrival,
        bool mon, bool tue, bool wed, bool thu, bool fri, bool sat, bool sun,
        DateOnly validFrom, DateOnly? validTo, DataSource source)
    {
        db.FlightSchedules.Add(new FlightSchedule
        {
            Airline = airline,
            FlightNumber = flightNumber,
            OriginAirport = origin,
            DestinationAirport = destination,
            DepartureLocalTime = departure,
            ArrivalLocalTime = arrival,
            Monday = mon, Tuesday = tue, Wednesday = wed, Thursday = thu, Friday = fri, Saturday = sat, Sunday = sun,
            ValidFrom = validFrom,
            ValidTo = validTo,
            Source = source,
            IsVerified = false,
            IsActive = true,
            Notes = "Varsayılan örnek tarife — saatler temsilidir, resmi kaynaktan doğrulanmadı. Admin panelinden düzeltip doğrulayabilirsiniz."
        });
    }
}
