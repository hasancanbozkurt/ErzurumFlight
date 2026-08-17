# ERZURUM FLIGHT — Hafif Mimari Teknik Şartname

> Visual Studio + OmniRoute/Invait Chat + 4-5 AI ajan ile geliştirilecek hafif, yönetilebilir ve sonradan büyütülebilir Erzurum uçuş web uygulamasının ana teknik şartnamesidir.

## 1. Ana hedef

Uygulama öncelikle **Erzurum Havalimanı (ERZ)** için:

- Bugünkü kalkış uçuşlarını
- Yarınki kalkış uçuşlarını
- Sonraki günlerdeki kalkış uçuşlarını
- Mümkün olan daha ileri tarihleri
- Erzurum'a gelecek uçuşları
- Uçuş saatlerini, numarasını, havayolunu ve rotasını
- Uçuş gerçekleştiğinde mümkünse canlı uçak konumunu

gösterecek.

### En önemli özellik

> **"Yarın / sonraki gün Erzurum'dan hangi uçaklar kalkacak?"**

Bu özellik canlı haritadan daha önemlidir.

---

## 2. Hafif mimari

İlk sürümde mikroservis veya ağır enterprise mimarisi kullanılmayacak.

```text
ErzurumFlight
│
├── Server
│   ├── Models
│   ├── Data
│   ├── Services
│   ├── Controllers
│   ├── Background
│   ├── Providers
│   └── Hubs
│
└── ClientApp
    └── React + TypeScript + Vite
```

### Backend

- ASP.NET Core
- C#
- Entity Framework Core

### Database

**SQLite**

İlk sürümde PostgreSQL kullanılmayacak. İleride ihtiyaç olursa EF Core sayesinde PostgreSQL'e geçilebilecek.

### Frontend

- React
- TypeScript
- Vite

### Canlı veri

- İlk tercih: Airplanes.Live

### Gerçek zamanlı

- SignalR

### Harita

- Leaflet
- OpenStreetMap tabanlı harita

### Background

- ASP.NET Core `BackgroundService`

### Cache

- İlk sürümde `MemoryCache`
- Redis yok

---

## 3. İlk sürümde gereksiz teknolojiler kullanılmayacak

Aşağıdakiler zorunlu olarak eklenmeyecek:

- Mikroservis
- Kubernetes
- Redis
- RabbitMQ
- Kafka
- Docker
- CQRS framework
- MediatR
- Generic Repository
- ayrı Domain/Application/Infrastructure projeleri
- ayrı Worker projesi

İhtiyaç oluşursa ileride eklenebilir.

---

## 4. Proje yapısı

```text
ErzurumFlight/
│
├── ErzurumFlight.sln
├── ERZURUM-FLIGHT.md
├── README.md
│
├── Server/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   │
│   ├── Controllers/
│   │   ├── FlightsController.cs
│   │   ├── CalendarController.cs
│   │   ├── LiveController.cs
│   │   └── AdminController.cs
│   │
│   ├── Models/
│   │   ├── Airport.cs
│   │   ├── Airline.cs
│   │   ├── Aircraft.cs
│   │   ├── FlightSchedule.cs
│   │   ├── FlightInstance.cs
│   │   ├── FlightOperation.cs
│   │   ├── AircraftPosition.cs
│   │   └── DataSource.cs
│   │
│   ├── Data/
│   │   ├── FlightDbContext.cs
│   │   ├── Migrations/
│   │   └── SeedData.cs
│   │
│   ├── Services/
│   │   ├── ScheduleService.cs
│   │   ├── FlightService.cs
│   │   ├── LiveTrackingService.cs
│   │   ├── AircraftMatchingService.cs
│   │   ├── DataSourceService.cs
│   │   └── CacheService.cs
│   │
│   ├── Background/
│   │   ├── ScheduleRefreshWorker.cs
│   │   ├── LiveTrackingWorker.cs
│   │   └── DataHealthWorker.cs
│   │
│   ├── Providers/
│   │   ├── IScheduleProvider.cs
│   │   ├── ILiveTrackingProvider.cs
│   │   └── AirplanesLiveProvider.cs
│   │
│   ├── Hubs/
│   │   └── FlightHub.cs
│   │
│   ├── DTOs/
│   └── Helpers/
│
└── ClientApp/
    ├── package.json
    └── src/
```

---

## 5. Erzurum Havalimanı

Ana havalimanı:

```text
Name: Erzurum Havalimanı
IATA: ERZ
ICAO: LTCE
City: Erzurum
Country: Türkiye
Timezone: Europe/Istanbul
```

Koordinatlar güvenilir bir kaynaktan doğrulanmalı.

---

## 6. Veritabanı

SQLite + Entity Framework Core.

### Airport

```text
Id
IataCode
IcaoCode
Name
City
Country
Latitude
Longitude
IsActive
```

### Airline

```text
Id
IataCode
IcaoCode
Name
Callsign
IsActive
```

### Aircraft

```text
Id
IcaoHex
Registration
AircraftType
LastSeenUtc
```

### FlightSchedule

İleri tarihli tarifenin temel tablosudur.

```text
Id
AirlineId
FlightNumber
OriginAirportId
DestinationAirportId
DepartureLocalTime
ArrivalLocalTime

Monday
Tuesday
Wednesday
Thursday
Friday
Saturday
Sunday

ValidFrom
ValidTo

SourceId
IsVerified
LastVerifiedUtc
IsActive
Notes
```

### FlightInstance

Belirli tarihteki uçuş.

```text
Id
FlightScheduleId
FlightDate
FlightNumber
OriginAirportId
DestinationAirportId
ScheduledDepartureUtc
ScheduledArrivalUtc
Status
IsVerified
SourceId
CreatedUtc
UpdatedUtc
```

Unique index:

```text
FlightDate + FlightNumber + OriginAirportId + DestinationAirportId
```

### FlightOperation

Uçuş gerçekleşmeye başladığında operasyon bilgileri.

```text
Id
FlightInstanceId
AircraftId
EstimatedDepartureUtc
ActualDepartureUtc
EstimatedArrivalUtc
ActualArrivalUtc
Status
MatchConfidence
LastLiveUpdateUtc
```

### AircraftPosition

```text
Id
FlightOperationId
TimestampUtc
Latitude
Longitude
Altitude
GroundSpeed
Heading
VerticalRate
IcaoHex
Callsign
SourceId
```

### DataSource

```text
Id
Name
Type
BaseUrl
IsEnabled
Priority
DailyLimit
RequestsPerSecond
LastSuccessUtc
LastFailureUtc
LastError
TermsUrl
Notes
```

---

## 7. Veri doğruluğu

Sistem bilmediği uçuşu gerçekmiş gibi göstermeyecek.

Örnek:

```text
30 Ağustos → Doğrulanmış
31 Ağustos → Tarife doğrulanmadı
```

Bir haftalık pattern'den teorik olarak uçuş çıkarılabiliyorsa bile kaynak doğrulaması yoksa kullanıcıya kesin gerçek gibi gösterilmemeli.

UI'da:

```text
✓ Tarife doğrulandı
```

veya:

```text
! Tarife doğrulanmadı
```

gösterilebilir.

---

## 8. İleri tarihli tarife motoru

Akış:

```text
FlightSchedule
      ↓
Tarih aralığı
      ↓
Haftanın günü kontrolü
      ↓
ValidFrom / ValidTo
      ↓
FlightInstance
```

Örneğin:

```text
Pazartesi + Çarşamba + Cuma
ValidFrom: 2026-08-01
ValidTo:   2026-09-30
```

yalnızca uygun günler için instance üretir.

### Kullanıcı seçenekleri

```text
Bugün
Yarın
3 Gün
7 Gün
14 Gün
30 Gün
Tarih seç
```

API:

```http
GET /api/flights?date=2026-08-10&direction=departure
GET /api/flights?date=2026-08-10&direction=arrival
GET /api/flights/upcoming?days=7
GET /api/calendar?from=2026-08-10&to=2026-08-30
```

---

## 9. Ana sayfa

Mobil öncelikli.

```text
ERZURUM FLIGHT
Erzurum Havalimanı
ERZ / LTCE

[Bugün] [Yarın] [3 Gün] [7 Gün]

GİDEN 12
GELEN 10
CANLI 2

GİDEN

09:25
TK2705
ERZ → IST
Planlandı
✓ Tarife doğrulandı
```

---

## 10. Uçuş detay sayfası

Route:

```text
/flights/{id}
```

Göster:

- Uçuş numarası
- Havayolu
- Uçak
- Kalkış
- Varış
- Planlanan kalkış
- Tahmini kalkış
- Gerçek kalkış
- Planlanan varış
- Tahmini varış
- Gerçek varış
- Durum
- Son güncelleme
- Veri kaynağı

Canlı uçuşta:

```text
CANLI TAKİP ET
```

---

## 11. Canlı takip

Route:

```text
/live
```

Göster:

- Erzurum çevresindeki uçaklar
- ERZ'den kalkmış aktif uçaklar
- ERZ'ye yaklaşan uçaklar
- canlı harita

Filtreler:

```text
Gelen
Giden
Aktif
Tümü
```

---

## 12. Airplanes.Live

İlk canlı ADS-B provider:

**Airplanes.Live**

Frontend doğrudan çağırmayacak.

Akış:

```text
BackgroundService
      ↓
Airplanes.Live
      ↓
LiveTrackingService
      ↓
MemoryCache
      ↓
SignalR
      ↓
React
```

Ücretsiz kullanımın güncel şartları, rate limitleri ve lisans koşulları production öncesinde resmi dokümantasyondan tekrar kontrol edilmelidir.

Resmi dokümantasyon:

`https://airplanes.live/api-guide/`

Ücretsiz kaynağın limitlerini aşmaya çalışma.

---

## 13. Rate limit ve cache

Örnek config:

```json
"LiveTracking": {
  "Enabled": true,
  "PollingSeconds": 30,
  "RequestsPerSecond": 1,
  "DailyRequestLimit": 500
}
```

Bu değerler kaynağın güncel resmi şartlarına göre değiştirilebilir.

Kullanıcı başına API çağrısı yapılmayacak.

Örneğin 100 kullanıcı varsa:

```text
100 kullanıcı
     ↓
tek backend cache
     ↓
tek kontrollü dış API akışı
```

---

## 14. LiveTrackingWorker

BackgroundService:

1. Aktif uçuşları bul.
2. İzlenmesi gereken uçuşları belirle.
3. Canlı ADS-B verisini çek.
4. Uçakları normalize et.
5. Uçuş eşleştirmesi yap.
6. FlightOperation güncelle.
7. AircraftPosition kaydet.
8. Cache güncelle.
9. SignalR event gönder.

---

## 15. Uçuş-uçak eşleştirme

Tarifeli:

```text
TK2705
ERZ → IST
09:25
```

Canlı:

```text
callsign
icao hex
registration
position
heading
```

ile eşleştirilecek.

Öncelik:

1. Flight number / callsign
2. Zaman penceresi
3. Erzurum'a yakınlık
4. Yön
5. Registration
6. Rota mantığı

Birden fazla aday varsa yanlış uçağı göstermek yerine:

```text
Unknown
```

döndür.

---

## 16. Uçuş durumları

```text
Scheduled
Monitoring
AircraftDetected
Taxiing
Departed
Airborne
Approaching
Landed
Delayed
Cancelled
Diverted
Unknown
```

Kalkış:

```text
Scheduled
→ Monitoring
→ AircraftDetected
→ Taxiing
→ Departed
→ Airborne
```

İniş:

```text
Airborne
→ Approaching
→ NearAirport
→ Landed
```

Tek bir altitude değerine göre karar verilmemeli; hız, irtifa, dikey hız ve havaalanına uzaklık birlikte değerlendirilmeli.

---

## 17. SignalR

Hub:

```text
/hubs/flights
```

Events:

```text
FlightStatusChanged
FlightPositionUpdated
FlightDeparted
FlightLanded
```

Kullanıcı sayfayı yenilemeden canlı durum görebilmeli.

---

## 18. Harita

İlk sürüm:

- Leaflet
- OpenStreetMap

Uçak marker:

- latitude
- longitude
- heading

ile gösterilecek.

Track varsa `AircraftPosition[]` üzerinden çizilecek.

Harita ve tile kullanım koşulları production öncesinde kontrol edilecek.

---

## 19. Admin

İlk sürüm basit tutulacak.

```text
/admin
/admin/schedules
/admin/sources
/admin/logs
```

Admin:

- giriş yapabilir
- tarifeleri görebilir
- tarife ekleyebilir
- düzenleyebilir
- devre dışı bırakabilir
- doğrulayabilir
- veri kaynaklarını görebilir
- son güncellemeleri görebilir
- hataları görebilir

Admin dışındaki kullanıcılar bu endpointlere erişemez.

---

## 20. Authentication

ASP.NET Core Identity kullanılabilir.

Minimum:

- Admin login
- Password hashing
- Authorization
- Secure cookie

API key/token frontend'e gönderilmeyecek.

---

## 21. Tarife kaynakları

Öncelik:

1. Resmi havalimanı/operasyon kaynağı
2. Havayolunun resmi kaynağı
3. Kullanım şartları uygun güvenilir açık veri
4. Hukuken uygun kontrollü scraper
5. Manuel admin doğrulaması

Hiçbir kaynak ileri tarihleri kesin sağlıyor varsayımıyla kodlanmayacak.

---

## 22. Scraper kuralları

Scraper gerekirse:

- CAPTCHA bypass etme
- Bot korumasını aşma
- Kullanım koşullarını ihlal etme
- Düşük istek sıklığı kullan
- Hata durumunda eski doğrulanmış veriyi koru
- Yeni veriyi doğrulamadan yayına alma

---

## 23. Tarih ve saat

Database:

```text
UTC
```

UI:

```text
Europe/Istanbul
```

`UTC+3` sabit hesabı kullanılmayacak.

.NET timezone dönüşümü kullanılacak.

---

## 24. Hata yönetimi

Dış kaynak çalışmazsa:

- Uygulama çökmeyecek
- Eski doğrulanmış veri silinmeyecek
- Son başarılı güncelleme gösterilecek
- Log tutulacak

Örnek:

```text
Veri kaynağı şu anda güncellenemiyor.
Son başarılı güncelleme: 10 Ağustos 2026 08:32
```

---

## 25. Logging

İlk sürümde ASP.NET Core logging yeterlidir. Gerekirse Serilog sonradan eklenebilir.

Kategoriler:

```text
Schedule
LiveTracking
AircraftMatching
DataSource
Database
Authentication
```

Secret/token loglanmayacak.

---

## 26. Health

```http
GET /health
```

Kontrol:

- uygulama
- SQLite
- veri kaynağının son durumu

---

## 27. Test

Öncelikli unit testler:

- Schedule date generation
- Haftanın günü
- ValidFrom/ValidTo
- Duplicate prevention
- Aircraft matching
- Status transitions
- Timezone conversion

Integration:

- API
- SQLite
- Mock live provider

Gerçek Airplanes.Live unit testlerde kullanılmayacak.

---

## 28. Mock provider

```text
MockScheduleProvider
MockLiveTrackingProvider
```

Geliştirme/test için kullanılabilir.

Mock veriler production gerçek uçuşları gibi gösterilmeyecek.

---

## 29. Configuration

Örnek:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=erzurumflight.db"
  },

  "FlightTracking": {
    "Enabled": true,
    "PollingSeconds": 30,
    "RadiusNm": 100
  },

  "Schedule": {
    "FutureDays": 90,
    "RefreshHours": 24
  },

  "Airport": {
    "Iata": "ERZ",
    "Icao": "LTCE",
    "Timezone": "Europe/Istanbul"
  }
}
```

Secret gerekiyorsa:

- User Secrets
- Environment Variables

kullan.

---

## 30. Geliştirme sırası

### Phase 1 — Foundation

- Visual Studio solution
- ASP.NET Core
- React/Vite
- SQLite
- EF Core

### Phase 2 — Database

- Models
- DbContext
- Migration
- Indexes
- ERZ seed

### Phase 3 — Schedule Engine

- FlightSchedule
- FlightInstance
- tarih üretimi
- doğrulama
- ileri tarih sorguları

### Phase 4 — API

- flights
- calendar
- details

### Phase 5 — Frontend

- dashboard
- giden
- gelen
- takvim
- detay

### Phase 6 — Admin

- login
- schedule yönetimi
- source yönetimi

### Phase 7 — Live Tracking

- Airplanes.Live
- rate limit
- MemoryCache
- BackgroundService
- aircraft matching

### Phase 8 — SignalR

- position
- status events

### Phase 9 — Map

- Leaflet
- aircraft markers
- route
- track

### Phase 10 — Test/Cleanup

- test
- performans
- güvenlik
- responsive
- hata yönetimi

---

## 31. OmniRoute / 4-5 ajan görevleri

### Agent 1 — Backend

- ASP.NET Core
- EF Core
- Models
- DbContext
- API
- Services

### Agent 2 — Flight Data

- ScheduleService
- schedule providers
- Airplanes.Live
- live tracking
- aircraft matching
- background workers

### Agent 3 — Frontend

- React
- TypeScript
- dashboard
- flight list
- calendar
- detail
- live map

### Agent 4 — QA

- unit tests
- integration tests
- mocks
- hata bulma
- doğrulama

### Agent 5 — Reviewer

- code review
- security
- performance
- deployment
- documentation

**Aynı dosyalara aynı anda yazmaları yasaktır.**

---

## 32. Git

Önerilen:

```text
main
develop
agent/backend
agent/data
agent/frontend
agent/tests
agent/review
```

Commit örnekleri:

```text
feat:
fix:
test:
refactor:
docs:
chore:
```

Ajanlar küçük commitler yapmalı.

---

## 33. MVP tamamlanma şartları

### Uçuş

- ERZ kalkış
- ERZ varış
- bugün
- yarın
- sonraki günler
- tarih seçimi
- uçuş detay

### Tarife

- haftalık pattern
- ValidFrom
- ValidTo
- doğrulama durumu
- kaynak

### Canlı

- ADS-B provider
- uçak bulma
- eşleştirme
- canlı harita
- status

### Teknik

- SQLite
- EF Core
- ASP.NET Core
- React
- SignalR
- BackgroundService
- admin
- temel testler

---

## 34. İlk sürümde yapılmayacaklar

- WhatsApp
- Telegram
- e-posta bildirimleri
- kullanıcı favorileri
- gelişmiş istatistikler
- çoklu havalimanı
- kendi ADS-B receiver
- PostgreSQL
- Redis
- Docker

Bunlar ihtiyaç oluşursa sonraki fazlarda eklenebilir.

---

## 35. Kritik ürün kuralı

Projenin başarısı şu soruyla ölçülecek:

> **"Yarın Erzurum'dan hangi uçaklar kalkacak?"**

Sistem bu soruya mümkün olan en güvenilir ve anlaşılır cevabı vermelidir.

İkinci soru:

> **"Bu uçak şu anda nerede?"**

Canlı takip bu ikinci ihtiyacı karşılar.

Öncelik:

```text
1. DOĞRU TARİFE
2. İLERİ TARİH
3. UÇUŞ DURUMU
4. CANLI UÇAK
5. HARİTA
```

---

## 36. AI ajanlarına ilk komut

İlk aşamada bütün sistemi yazdırma.

### İlk komut

> `ERZURUM-FLIGHT.md` dosyasını ana teknik şartname kabul et. Mevcut Visual Studio solution'ı analiz et. Gereksiz ağır mimari kurma. Önce mevcut proje yapısının şartnameye uygunluğunu kontrol et. Henüz büyük çaplı kod değişikliği yapma. Eksikleri, çakışmaları ve önerilerini raporla.

### Sonraki komut

> Phase 1 — Project Foundation görevini uygula. Sadece gerekli hafif proje yapısını oluştur. ASP.NET Core + React/Vite + SQLite + EF Core temelini kur. Mikroservis, Redis, Docker, ayrı Worker projesi veya gereksiz abstraction ekleme.

---

## 37. Kesin yasaklar

AI ajanları:

- Sahte uçuşları production verisi olarak göstermeyecek.
- Ücretli API eklemeyecek.
- Ücretsiz API limitlerini aşmaya çalışmayacak.
- API anahtarlarını koda yazmayacak.
- Frontend'den üçüncü taraf gizli API çağrısı yapmayacak.
- Kullanım koşullarını veya bot korumalarını aşmaya çalışmayacak.
- Bilinmeyen canlı uçağı yanlış uçuşla eşleştirmeyecek.
- Çalışan kodu gereksiz yere yeniden yazmayacak.
- Gereksiz ağır mimari eklemeyecek.

---

# 38. Nihai mimari

```text
                  INTERNET
                     │
                     ▼
          ┌────────────────────┐
          │  React + Vite      │
          │  Mobile Web UI     │
          └─────────┬──────────┘
                    │
                    ▼
          ┌────────────────────┐
          │    ASP.NET Core    │
          │                    │
          │ Flight API         │
          │ Schedule Service   │
          │ Live Service       │
          │ Admin              │
          │ SignalR            │
          │ BackgroundService  │
          └────────┬─────┬─────┘
                   │     │
             ┌─────┘     └───────────┐
             ▼                       ▼
       ┌───────────┐          ┌──────────────┐
       │   SQLite  │          │ Airplanes.Live│
       │ Database  │          │    ADS-B      │
       └───────────┘          └──────────────┘
```

Bu mimari küçük bir sunucuda çalışabilecek kadar hafiftir; ihtiyaç büyürse PostgreSQL, Redis, ayrı worker veya Docker sonradan eklenebilir.

**Ana ilke: Önce doğru Erzurum uçuş tarifesi, sonra canlı uçak takibi.**
