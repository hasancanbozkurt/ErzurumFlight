# Erzurum Flight

Erzurum Havalimanı (ERZ / LTCE) için uçuş tarifesi ve canlı takip web uygulaması.
Mimari ve ürün kararları için `ERZURUM-FLIGHT.md` dosyasına bakınız (ana teknik şartname).

**Öncelik sırası:** 1) Doğru tarife → 2) İleri tarih → 3) Uçuş durumu → 4) Canlı uçak → 5) Harita.

---

## ⚠️ Önemli: Bu proje nasıl hazırlandı, nelere dikkat edin

Bu proje, NuGet.org'a ağ erişimi olmayan bir sandbox ortamında elle yazıldı. **.NET 10 SDK bu
ortamda kurulup gerçekten kullanıldı**; `Models/`, `Helpers/`, `DTOs/` ve `Providers/` klasörlerindeki
tüm kod, EF Core gerektirmeyen bir yardımcı projeyle **gerçekten derlenip doğrulandı**, ayrıca
`Tests/` projesindeki testlerin mantığı elle/Python ile çapraz kontrol edildi. Ancak EF Core,
ASP.NET Core Identity ve SignalR paketlerini indirip **tam çözümü uçtan uca derlemek** bu ortamda
mümkün olmadı (NuGet erişimi yok). Bu nedenle:

- **İlk açılışta mutlaka** Visual Studio'nun NuGet paketlerini geri yüklemesine izin verin
  (otomatik olur) ve bir kere `dotnet build` / Visual Studio'da *Build Solution* çalıştırın.
- Küçük bir derleme hatasıyla karşılaşırsanız (örn. bir using ifadesi eksikliği), bu genellikle
  tek satırlık bir düzeltmedir — kodun mimarisi ve mantığı sağlamdır.
- Frontend (`ClientApp/`) tamamen bu ortamda **gerçek `npm install` + `npm run build` ile derlendi
  ve doğrulandı** (TypeScript hataları dahil giderildi); bu kısımda sorun yaşamamanız beklenir.

---

## Gereksinimler

| Araç | Sürüm |
|---|---|
| Visual Studio | 2026 (17.x) — ".NET 10" ve "ASP.NET ve web geliştirme" iş yükü |
| .NET SDK | 10.0.10x (`dotnet --version` ile kontrol edin) |
| Node.js | 20+ (npm dahil) |

## Hızlı başlangıç

### 1) Backend (Server)

```bash
cd Server
dotnet restore
dotnet build
dotnet run
```

Visual Studio'da: `ErzurumFlight.sln` dosyasını açın, **Server** projesini başlangıç projesi
yapın (zaten öyle işaretli), **https** profiliyle çalıştırın (F5). API şu adreste açılır:
`https://localhost:5001` (ve `http://localhost:5000`).

İlk çalıştırmada uygulama otomatik olarak:
- SQLite veritabanını (`erzurumflight.db`) oluşturur,
- Erzurum Havalimanı (ERZ/LTCE) ve gerçek uçuş rotalarındaki 4 havalimanını (İstanbul, Sabiha
  Gökçen, Esenboğa, Adnan Menderes) ekler,
- THY, AJet, Pegasus ve SunExpress için **gerçek Erzurum güzergahlarına dayalı** bir tarife
  yükler (bkz. `Data/SeedData.cs` başındaki kaynak notları — rotalar doğrulanmış, saatler
  temsilidir; bu yüzden UI'da "! Tarife doğrulanmadı" rozetiyle işaretlenir),
- Bu tarifeden **90 gün ileriye kadar** somut uçuşları hemen üretir — **hiçbir admin/manuel
  adım gerekmez**, uygulamayı açar açmaz Ana Sayfa'da Bugün/Yarın/3-7-14-30 Gün için uçuşlar
  hazır gelir,
- `appsettings.Development.json` içindeki `Seed:AdminUserName` / `Seed:AdminPassword`
  tanımlıysa bir admin kullanıcısı oluşturur (varsayılan: **admin** / **ErzurumFlight!2026** —
  **production'a almadan önce mutlaka değiştirin**, tercihen User Secrets ile).

> Development ortamında `FlightTracking:UseMockProvider = true` olduğu için canlı uçak verisi
> **sahte (mock)** veridir — gerçek Airplanes.Live API'sine gereksiz istek atılmaz. Gerçek veriyle
> test etmek için `appsettings.Development.json`'da bu değeri `false` yapın.

Gerçek saatleri öğrendiğinizde (resmi DHMİ/havayolu tarifesinden), admin panelinden ilgili
tarifeyi düzenleyip **"Doğrula"** ile işaretleyebilirsiniz — ama bu artık bir ön koşul değil,
isteğe bağlı bir iyileştirmedir.

### 2) Frontend (ClientApp)

```bash
cd ClientApp
npm install
npm run dev
```
cd source\repos\ErzurumFlight2\ClientApp\npm run dev

Tarayıcıda `http://localhost:5173` açılır. `vite.config.ts` içindeki dev proxy, `/api` ve
`/hubs` isteklerini otomatik olarak `https://localhost:5001`'e yönlendirir — backend'in aynı anda
çalışıyor olması yeterlidir, ek CORS ayarı gerekmez.

> Visual Studio, Vite/npm projelerini doğrudan çalıştırmaz; `ClientApp`'i ayrı bir terminalde
> (veya VS Code'da) `npm run dev` ile başlatın. İsterseniz Visual Studio'da *Solution Explorer*'a
> "Existing Web Site" olarak ekleyebilir veya `npm run build` çıktısını (`ClientApp/dist`)
> `Server/wwwroot`'a kopyalayıp `app.UseStaticFiles()` ile tek bir process'ten servis edebilirsiniz
> (production dağıtımı için önerilir).

### 3) Testler

```bash
cd Tests
dotnet test
```

`ScheduleDateCalculator`, `AircraftMatcher`, `TimeZoneHelper` ve `FlightStatusTransitions` için
saf mantık testleri içerir (dış servis/DB gerektirmez).

---

## Varsayılan uçuş verisi (kutudan çıktığı gibi çalışır)

Bu sürümde tarife verisi **admin panelinden elle girilmez, dış bir kaynaktan da çekilmez** —
`Data/SeedData.cs` içine gerçek Erzurum uçuş rotaları doğrudan gömülüdür ve uygulama ilk
açıldığında otomatik olarak yüklenir:

| Havayolu | Rota | Sıklık |
|---|---|---|
| Türk Hava Yolları (TK) | ERZ ⇄ İstanbul (IST) | Her gün |
| AJet (VF) | ERZ ⇄ Sabiha Gökçen (SAW) | Her gün |
| AJet (VF) | ERZ ⇄ Ankara Esenboğa (ESB) | Pzt/Çrş/Cum/Paz |
| Pegasus (PC) | ERZ ⇄ Sabiha Gökçen (SAW) | Her gün |
| SunExpress (XQ) | ERZ ⇄ İzmir (ADB) | Sal/Perş/Cmt |

**Rotalar ve havayolları gerçektir** (Erzurum Havalimanı'nın bilinen güzergahlarına dayanır).
**Saatler temsilidir** — resmi bir tarife API'sinden anlık çekilmediği için admin tarafından
doğrulanana kadar her uçuş kartında "! Tarife doğrulanmadı" rozeti görünür. Gerçek saatleri
`/api/admin/schedules` üzerinden düzeltmek istediğinizde bu tamamen isteğe bağlıdır — uygulama
bu düzeltme yapılmadan da tam işlevseldir ve Ana Sayfa hiçbir zaman boş görünmez.

Kendi rotanızı/havalimanınızı eklemek isterseniz `Data/SeedData.cs` → `SeedAirportsAirlinesAndSchedules`
metodundaki `AddSchedule(...)` satırlarını çoğaltıp düzenlemeniz yeterlidir; veritabanını silip
(`erzurumflight.db`) uygulamayı yeniden başlattığınızda yeni tarife otomatik yüklenir.

```
ErzurumFlight/
├── ErzurumFlight.sln
├── ERZURUM-FLIGHT.md        ← Ana teknik şartname
├── README.md                 ← Bu dosya
│
├── Server/                   ← ASP.NET Core (.NET 10) — API + SignalR + BackgroundService
│   ├── Program.cs
│   ├── Controllers/           Flights, Calendar, Live, Admin, Auth
│   ├── Models/                Airport, Airline, Aircraft, FlightSchedule, FlightInstance, ...
│   ├── Data/                  FlightDbContext, SeedData, Migrations/
│   ├── Services/               ScheduleService, FlightService, LiveTrackingService, ScheduleSyncService, ...
│   ├── Background/            ScheduleRefreshWorker, LiveTrackingWorker, DataHealthWorker, ScheduleSyncWorker
│   ├── Providers/              ILiveTrackingProvider (AirplanesLiveProvider), IFlightScheduleDataProvider (AeroDataBoxProvider, Mock)
│   ├── Hubs/                   FlightHub (SignalR)
│   ├── Helpers/                TimeZoneHelper, ScheduleDateCalculator, AircraftMatcher, ...
│   └── DTOs/
│
├── Tests/                     xUnit — saf mantık testleri
│
└── ClientApp/                 React + TypeScript + Vite
    └── src/
        ├── api/                client.ts, types.ts
        ├── hooks/              useFlightHub.ts (SignalR)
        ├── components/         Header, DateTabs, FlightCard
        └── pages/               Dashboard, FlightDetail, LiveMap, AdminLogin, AdminSchedules
```

## Gerçek canlı tarife/durum nasıl açılır (ÖNEMLİ)

Bu sürümde iki ayrı tarife katmanı vardır:

1. **Temel iskelet** (`Data/SeedData.cs` → `FlightSchedule` deseni): uygulama açılır açılmaz 90
   gün ileriye kadar uçuşları üretir, admin/manuel adım gerekmez (bkz. yukarıdaki "Varsayılan
   uçuş verisi" bölümü). Ama bu katman **statiktir** — iptal/gecikme bilgisini bilemez, çünkü
   havayolları bu kararı uçuşa günler kala alır.

2. **Canlı katman** (`Services/ScheduleSyncService.cs` + `Background/ScheduleSyncWorker.cs`):
   gerçek bir dış API'den (aşağıda) Erzurum'un **güncel/yakın (±48 saat)** kalkış-varış
   durumunu periyodik çeker ve veritabanını günceller — **iptal, gecikme, gerçek saat dahil**.
   Bir uçuş bu pencereye girdiğinde otomatik olarak doğrulanır (`IsVerified=true`) ve durumu
   canlı kaynaktan gelir; değişiklikler SignalR ile anında tüm açık sekmelere yayılır
   ("uçuş iptal edildi" banner'ı dahil).

**Bu ikinci katman gerçek bir API anahtarı olmadan çalışamaz** — böyle bir veri, kayıt
gerektirmeden ücretsiz sunan hiçbir kaynak yoktur. `FlightData:RapidApiKey` boşsa uygulama
otomatik olarak `MockFlightScheduleDataProvider`'a düşer (sahte veri, yalnızca UI'da iptal
senaryosunu görmek için — bir uçuş örneği kasıtlı "Canceled" gelir); **gerçek canlı veri için**:

### AeroDataBox ücretsiz anahtarı alma (5 dakika, kredi kartı istenmez)

1. https://rapidapi.com/aedbx-aedbx/api/aerodatabox adresine gidin, RapidAPI'ye ücretsiz kaydolun.
2. Sayfadaki **"Pricing"** sekmesinden **Basic (Free)** plana abone olun (aylık ~600 birim kota,
   kredi kartı gerekmez).
3. **"Endpoints"** sekmesinde sağ panelde görünen `X-RapidAPI-Key` değerini kopyalayın.
4. Anahtarı **User Secrets**'a ekleyin (kaynak koduna/appsettings'e asla yazmayın):
   ```bash
   cd Server
   dotnet user-secrets init
   dotnet user-secrets set "FlightData:RapidApiKey" "BURAYA_KOPYALADIĞINIZ_ANAHTAR"
   ```
5. Uygulamayı yeniden başlatın. Konsolda `"Uçuş tarifesi/durumu kaynağı: AeroDataBox (gerçek
   canlı veri aktif)."` satırını görürseniz kurulum başarılıdır. `ScheduleSyncWorker` birkaç
   saniye içinde ilk senkronizasyonu yapar ve loglar `{Fetched} kayıt çekildi, ...` satırını basar.

Anahtar girilmezse uygulama **çökmez**; sadece mock veriyle devam eder ve başlangıçta konsola
açık bir uyarı basar, böylece "neden gerçek veri gelmiyor" sorusu asla sessiz kalmaz.

### Rate limit gerçeği (dürüst uyarı)

Ücretsiz plan aylık ~600 birim kotalıdır; bu uç nokta (FIDS) çağrı başına 2 birim harcar, yani
ayda ~300 çağrı hakkınız var. `ScheduleSyncWorker` varsayılan olarak **3 saatte bir**
(`FlightData:SyncIntervalMinutes`) tek bir istek atar — günde 8, ayda ~240 istek — kotanın
altında kalacak şekilde ayarlanmıştır. Daha sık güncelleme isterseniz `SyncIntervalMinutes`'ı
düşürebilirsiniz ama ücretsiz plan sınırını aşmamaya dikkat edin; aşarsanız `DataSourceService`
bunu `/api/admin/sources` altında hata olarak loglar, uygulama çökmez, sadece o turdaki
güncelleme atlanır ve veritabanındaki son bilinen (bir önceki turdan doğrulanmış) veri korunur.

### Neden 30 gün sonrasının iptal bilgisi hiçbir zaman "canlı" olamaz

Bunu netleştirmek isteriz: bu bir mimari kısıtlama değil, gerçek dünyanın işleyiş biçimidir.
Havayolları bir uçuşu genellikle operasyon gününe 24-72 saat kala iptal eder/gecikmeye alır;
3 hafta sonraki bir uçuşun iptal olup olmayacağını **havayolunun kendisi de henüz bilmez**.
Bu yüzden endüstri standardı (havaalanı ekranları, uçuş takip siteleri) da tam olarak bu projenin
kurduğu iki katmanlı modeli kullanır: uzak gelecek = tarife deseni, yakın gelecek = canlı durum.

```
GET  /api/flights?date=2026-08-10&direction=departure
GET  /api/flights?date=2026-08-10&direction=arrival
GET  /api/flights/upcoming?days=7
GET  /api/flights/counts?date=2026-08-10
GET  /api/flights/{id}
GET  /api/calendar?from=2026-08-10&to=2026-08-30
GET  /api/live/aircraft
POST /api/auth/login   { userName, password }
POST /api/auth/logout
GET  /api/auth/me
GET  /api/admin/schedules        [Authorize]
POST /api/admin/schedules        [Authorize]
POST /api/admin/schedules/{id}/disable  [Authorize]
POST /api/admin/schedules/{id}/verify   [Authorize]
GET  /api/admin/sources          [Authorize]
GET  /api/admin/logs             [Authorize]
GET  /health
WS   /hubs/flights   (SignalR: FlightStatusChanged, FlightPositionUpdated, FlightDeparted, FlightLanded, FlightCancelled, ScheduleSynced)
```

## Kullanılan teknolojiler (ve kullanılmayanlar)

✅ ASP.NET Core (.NET 10), EF Core + SQLite, React + TypeScript + Vite, SignalR, Leaflet +
OpenStreetMap, `BackgroundService`, `MemoryCache`, ASP.NET Core Identity (yalnızca admin girişi).

❌ Mikroservis, Kubernetes, Redis, RabbitMQ, Kafka, Docker, CQRS/MediatR, Generic Repository,
ayrı Domain/Application/Infrastructure projeleri, ayrı Worker projesi — şartname gereği ilk
sürümde kasıtlı olarak kullanılmadı. İhtiyaç oluşursa EF Core provider değişikliğiyle
PostgreSQL'e, `ICacheService` arayüzü üzerinden Redis'e geçiş kolaydır.

## Canlı veri kaynağı

[Airplanes.Live](https://airplanes.live/api-guide/) — ücretsiz, API anahtarı gerektirmeyen ADS-B
REST API'si. Frontend bu API'ye **asla doğrudan** istek atmaz; tüm istekler tek bir
`LiveTrackingWorker` üzerinden, `appsettings.json` → `FlightTracking:PollingSeconds` (varsayılan
30 saniye) periyoduyla ve `RequestsPerSecond`/`DailyRequestLimit` sınırlarına uygun şekilde
yapılır, sonuç `MemoryCache`'te tutulur ve tüm kullanıcılara SignalR ile yayınlanır. **Production'a
almadan önce Airplanes.Live'ın güncel kullanım şartlarını ve rate limit'lerini tekrar kontrol
edin.**

## Güvenlik notları

- Admin parolası kaynak kodda tutulmaz; `appsettings.Development.json`'daki `Seed:*` yalnızca
  geliştirme kolaylığı içindir. Production'da **User Secrets** veya ortam değişkenleri kullanın:
  ```bash
  dotnet user-secrets set "Seed:AdminUserName" "admin"
  dotnet user-secrets set "Seed:AdminPassword" "güçlü-bir-parola"
  ```
- Hiçbir üçüncü taraf API anahtarı frontend'e gönderilmez.
- Admin uçları `[Authorize]` ile korunur; cookie `HttpOnly` ve production'da `Secure`'dur.
