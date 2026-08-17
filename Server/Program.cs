using ErzurumFlight.Server.Background;
using ErzurumFlight.Server.Data;
using ErzurumFlight.Server.Helpers;
using ErzurumFlight.Server.Hubs;
using ErzurumFlight.Server.Models;
using ErzurumFlight.Server.Providers;
using ErzurumFlight.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Veritabanı (SQLite + EF Core) ----------
builder.Services.AddDbContext<FlightDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Identity (yalnızca Admin girişi için) ----------
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<FlightDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

    // API tabanlı auth: yönlendirme yerine 401/403 JSON durum kodları döndür.
    options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});

// ---------- MemoryCache (ilk sürümde Redis yok) ----------
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, CacheService>();

// ---------- Servisler ----------
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IDataSourceService, DataSourceService>();
builder.Services.AddScoped<ILiveTrackingService, LiveTrackingService>();
builder.Services.AddScoped<IScheduleSyncService, ScheduleSyncService>();
builder.Services.AddSingleton<IAircraftMatchingService, AircraftMatchingService>();

// ---------- Sağlayıcılar (Provider) ----------
// Development'ta appsettings: "FlightTracking:UseMockProvider": true iken sahte veri kullanılır.
var useMockProvider = builder.Configuration.GetValue<bool?>("FlightTracking:UseMockProvider") ?? false;
if (useMockProvider)
{
    builder.Services.AddSingleton<ILiveTrackingProvider, MockLiveTrackingProvider>();
}
else
{
    builder.Services.AddHttpClient<ILiveTrackingProvider, AirplanesLiveProvider>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ErzurumFlight/1.0 (+https://github.com/)");
    });
}

// ---------- Canlı tarife/durum kaynağı (GERÇEK ZAMANLI: tarife + iptal/gecikme durumu) ----------
// AeroDataBox — appsettings/User Secrets'ta "FlightData:RapidApiKey" ayarlıysa kullanılır.
// Anahtar yoksa uygulama ÇÖKMEZ; otomatik olarak MockFlightScheduleDataProvider'a düşer, böylece
// ilk klonlamada bile tam çalışır durumda kalır. Gerçek canlı veri (iptal/gecikme dahil) için
// README "Gerçek uçuş verisi kurulumu" bölümündeki adımlarla ücretsiz bir anahtar alıp ekleyin.
var rapidApiKey = builder.Configuration["FlightData:RapidApiKey"];
if (!string.IsNullOrWhiteSpace(rapidApiKey))
{
    builder.Services.AddHttpClient<IFlightScheduleDataProvider, AeroDataBoxProvider>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Add("X-RapidAPI-Key", rapidApiKey);
        client.DefaultRequestHeaders.Add("X-RapidAPI-Host", "aerodatabox.p.rapidapi.com");
    });
}
else
{
    builder.Services.AddSingleton<IFlightScheduleDataProvider, MockFlightScheduleDataProvider>();
}

// ---------- Background Services ----------
builder.Services.AddHostedService<ScheduleRefreshWorker>();
builder.Services.AddHostedService<LiveTrackingWorker>();
builder.Services.AddHostedService<DataHealthWorker>();
builder.Services.AddHostedService<ScheduleSyncWorker>();

// ---------- Controllers + SignalR ----------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Enum değerlerini sayı yerine string olarak gönder (Frontend'deki toLowerCase hatasını çözer)
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
}).AddJsonProtocol(options =>
{
    // SignalR üzerinden giden verilerde de enum'ları string yap
    options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// ---------- CORS (React/Vite dev server için) ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Identity cookie'si için gerekli.
    });
    // Development için ekstra esneklik (SignalR negotiation sorunlarını önler)
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("DevPolicy", policy =>
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    }
});

// ---------- OpenAPI (yalnızca Development) ----------
builder.Services.AddOpenApi();

// ---------- Health checks ----------
builder.Services.AddHealthChecks()
    .AddDbContextCheck<FlightDbContext>("sqlite");

var app = builder.Build();

// ---------- Aktif canlı tarife kaynağını açıkça logla (sessiz Mock moduna düşülmesin) ----------
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
if (string.IsNullOrWhiteSpace(rapidApiKey))
{
    startupLogger.LogWarning(
        "FlightData:RapidApiKey ayarlanmamış — uçuş tarifesi/durumu için MOCK (sahte) veri kullanılıyor. " +
        "Gerçek canlı veri (iptal/gecikme dahil) için README 'Gerçek uçuş verisi kurulumu' bölümüne bakın.");
}
else
{
    startupLogger.LogInformation("Uçuş tarifesi/durumu kaynağı: AeroDataBox (gerçek canlı veri aktif).");
}

// ---------- İlk çalıştırmada veritabanını oluştur + seed et ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var scheduleService = scope.ServiceProvider.GetRequiredService<IScheduleService>();
    await SeedData.InitializeAsync(db, userManager, scheduleService, app.Configuration);

    // İLK SENKRONİZASYON: Gerçek uçuş verilerini AeroDataBox'tan çek
    // Not: Rate limit (429) önlemek için 10 saniye bekleyip öyle dene
    if (!string.IsNullOrWhiteSpace(rapidApiKey))
    {
        var syncService = scope.ServiceProvider.GetRequiredService<IScheduleSyncService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("İlk senkronizasyon planlanıyor: 10 saniye bekleniyor (rate limit önleme)...");

        await Task.Delay(10000); // 10 saniye bekle

        try
        {
            var nowLocal = TimeZoneHelper.UtcToLocal(DateTime.UtcNow, TimeZoneHelper.Istanbul);
            // AeroDataBox max 12 saat pencere izin veriyor: şimdi -> şimdi+12 saat
            var fromLocal = nowLocal;
            var toLocal = nowLocal.AddHours(12);

            logger.LogInformation("İlk senkronizasyon başlatılıyor: AeroDataBox'tan gerçek uçuş verileri çekiliyor (pencere: {From} - {To})...", fromLocal, toLocal);
            var result = await syncService.SyncWindowAsync(fromLocal, toLocal);
            logger.LogInformation(
                "İlk senkronizasyon tamamlandı: {Fetched} kayıt çekildi, {Created} yeni, {Updated} güncellendi, {Failed} hata.",
                result.Fetched, result.Created, result.Updated, result.Failed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "İlk senkronizasyon başarısız oldu. Hata: {Message}", ex.Message);
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Development'ta daha esnek CORS, Production'da katı policy
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevPolicy");
}
else
{
    app.UseCors("ClientApp");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<FlightHub>("/hubs/flights");
app.MapHealthChecks("/health");

app.Run();
