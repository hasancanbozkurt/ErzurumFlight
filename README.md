<h2> Kullanılan Teknolojiler</h2>
Bu proje; gerçek zamanlı uçuş verilerini işleyen, uçuş tarifelerini senkronize eden ve istemcilere anlık güncellemeler ileten modern bir <strong>.NET tabanlı web uygulaması</strong> olarak geliştirilmiştir.
<h3>Backend</h3>
<ul>
 	<li><strong>ASP.NET Core</strong> — REST API, uygulama yaşam döngüsü ve HTTP isteklerinin yönetimi</li>
 	<li><strong>C#</strong> — Uygulamanın ana programlama dili</li>
 	<li><strong>.NET Dependency Injection</strong> — Servislerin yönetimi ve gevşek bağlı (loosely coupled) mimari</li>
 	<li><strong>ASP.NET Core Controllers</strong> — RESTful API endpoint'lerinin oluşturulması</li>
 	<li><strong>SignalR</strong> — Uçuş bilgilerinin istemcilere gerçek zamanlı olarak iletilmesi</li>
 	<li><strong>Background Services / Hosted Services</strong> — Tarife yenileme, canlı takip, veri sağlığı ve senkronizasyon işlemlerinin arka planda otomatik yürütülmesi</li>
</ul>
<h3>Veritabanı ve ORM</h3>
<ul>
 	<li><strong>SQLite</strong> — Hafif ve taşınabilir ilişkisel veritabanı</li>
 	<li><strong>Entity Framework Core</strong> — Veritabanı işlemleri ve ORM katmanı</li>
 	<li><strong>EF Core Health Check</strong> — Veritabanı bağlantısının ve uygulama sağlığının kontrol edilmesi</li>
</ul>
<h3>Kimlik Doğrulama ve Yetkilendirme</h3>
<ul>
 	<li><strong>ASP.NET Core Identity</strong> — Kullanıcı ve yönetici hesaplarının yönetimi</li>
 	<li><strong>IdentityRole</strong> — Rol tabanlı yetkilendirme altyapısı</li>
 	<li><strong>Cookie Authentication</strong> — Yönetici oturumlarının güvenli şekilde yönetilmesi</li>
 	<li><strong>HTTPOnly / SameSite Cookie</strong> — Kimlik doğrulama çerezlerinin güvenliğinin artırılması</li>
 	<li><strong>401 / 403 API Responses</strong> — API tabanlı kimlik doğrulama ve yetkilendirme yaklaşımı</li>
</ul>
<h3>Gerçek Zamanlı Veri</h3>
Proje farklı veri kaynaklarıyla çalışabilecek şekilde <strong>Provider Pattern</strong> yaklaşımı kullanılarak tasarlanmıştır.
<ul>
 	<li><strong>AeroDataBox API</strong> — Gerçek uçuş tarife ve uçuş durumlarının alınması</li>
 	<li><strong>Airplanes.Live</strong> — Canlı uçak takip verilerinin alınması</li>
 	<li><strong>RapidAPI</strong> — AeroDataBox API erişiminin sağlanması</li>
 	<li><strong>Mock Providers</strong> — API anahtarı veya gerçek veri kaynağı bulunmadığında geliştirme/test ortamında sahte verilerle çalışabilme</li>
</ul>
Bu yapı sayesinde gerçek API'lere bağımlı kalmadan proje geliştirilebilir ve test edilebilir.
<h3>Önbellekleme</h3>
<ul>
 	<li><strong>ASP.NET Core MemoryCache</strong> — Sık kullanılan verilerin bellekte önbelleğe alınması</li>
 	<li><strong>ICacheService / CacheService</strong> — Önbellek işlemlerinin uygulama servislerinden soyutlanması</li>
</ul>
Mimari, ileride MemoryCache yerine <strong>Redis gibi dağıtık cache sistemlerine</strong> geçiş yapılabilecek şekilde hazırlanmıştır.
<h3>HTTP ve API</h3>
<ul>
 	<li><strong>HttpClient / IHttpClientFactory</strong> — Harici uçuş API'leriyle güvenli ve yönetilebilir HTTP iletişimi</li>
 	<li><strong>JSON Serialization</strong> — API verilerinin JSON formatında işlenmesi</li>
 	<li><strong>CamelCase JSON</strong> — Frontend ile uyumlu JSON property isimlendirmesi</li>
 	<li><strong>JsonStringEnumConverter</strong> — Enum değerlerinin frontend'e sayısal değerler yerine string olarak gönderilmesi</li>
 	<li><strong>CORS</strong> — React/Vite gibi ayrı bir frontend uygulamasının API'ye güvenli şekilde erişebilmesi</li>
</ul>
<h3>API Dokümantasyonu</h3>
<ul>
 	<li><strong>OpenAPI</strong> — Development ortamında API endpoint'lerinin otomatik olarak dokümante edilmesi ve keşfedilmesi</li>
</ul>
<h3>Uygulama Sağlığı ve İzleme</h3>
Proje içerisinde temel bir <strong>Health Check</strong> altyapısı bulunmaktadır.

<code>/health</code> endpoint'i üzerinden uygulamanın ve SQLite veritabanı bağlantısının çalışır durumda olup olmadığı kontrol edilebilir.

Ayrıca uygulama başlangıcında gerçek veri sağlayıcısının veya Mock Provider'ın aktif olup olmadığı loglanmaktadır.
<h3>Zamanlanmış ve Arka Plan İşlemleri</h3>
Uçuş verilerinin sürekli güncel tutulması için birden fazla arka plan servisi kullanılmaktadır:
<ul>
 	<li><code>ScheduleRefreshWorker</code> — Uçuş tarifelerinin yenilenmesi</li>
 	<li><code>LiveTrackingWorker</code> — Canlı uçuş takibinin yürütülmesi</li>
 	<li><code>DataHealthWorker</code> — Veri kaynağının ve veri bütünlüğünün kontrol edilmesi</li>
 	<li><code>ScheduleSyncWorker</code> — Harici kaynak ile yerel veritabanının senkronizasyonu</li>
</ul>
Bu yaklaşım sayesinde kullanıcı isteğine bağlı olmayan işlemler API request lifecycle'ından ayrılarak arka planda yürütülmektedir.
<h3>Mimari Yaklaşım</h3>
Proje, sorumlulukların birbirinden ayrıldığı <strong>katmanlı ve servis tabanlı bir mimari</strong> kullanmaktadır.
<pre><code class="language-text">Frontend
   │
   ▼
ASP.NET Core API
   │
   ├── Controllers
   │
   ├── Services
   │
   ├── Providers
   │      ├── AeroDataBox
   │      ├── Airplanes.Live
   │      └── Mock Providers
   │
   ├── Entity Framework Core
   │
   ├── SQLite
   │
   ├── SignalR
   │
   └── Background Workers
</code></pre>
Bu yapı sayesinde veri sağlayıcısı, iş mantığı, veritabanı ve API katmanları birbirinden bağımsız tutulmuştur.
<h3>Öne Çıkan Teknik Özellikler</h3>
<ul>
 	<li> Gerçek zamanlı veri aktarımı</li>
 	<li> Otomatik uçuş verisi senkronizasyonu</li>
 	<li> Harici API entegrasyonu</li>
 	<li> Provider tabanlı veri kaynağı mimarisi</li>
 	<li> SQLite + Entity Framework Core</li>
 	<li> ASP.NET Core Identity</li>
 	<li> SignalR ile WebSocket tabanlı gerçek zamanlı iletişim</li>
 	<li> Dependency Injection</li>
 	<li> Memory Cache</li>
 	<li> Mock veri sağlayıcıları ile API'siz geliştirme</li>
 	<li> Health Check altyapısı</li>
 	<li> OpenAPI desteği</li>
 	<li> Background Worker mimarisi</li>
 	<li> CORS ve güvenli cookie yapılandırması</li>
</ul>
<h3>Neden Bu Teknolojiler?</h3>
Projenin temel amacı yalnızca uçuş verilerini göstermek değil, <strong>harici veri kaynaklarından gelen bilgileri sürekli işleyebilen, senkronize edebilen ve değişiklikleri kullanıcıya gerçek zamanlı aktarabilen ölçeklenebilir bir altyapı</strong> oluşturmaktır.

Bu nedenle ASP.NET Core'un Dependency Injection, Hosted Services, SignalR, Entity Framework Core ve Identity gibi yerleşik bileşenlerinden yararlanılmış; harici veri kaynakları ise Provider Pattern ile uygulamanın ana iş mantığından ayrılmıştır.

Böylece mevcut sistem, ileride farklı uçuş veri sağlayıcılarının eklenmesi, Redis gibi dağıtık cache sistemlerine geçilmesi veya farklı frontend teknolojilerinin kullanılması için uygun bir altyapıya sahiptir.
