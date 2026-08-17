# EF Core Migrations

Bu klasör kasıtlı olarak boş bırakıldı.

İlk sürümde `Program.cs`, açılışta `Database.EnsureCreatedAsync()` çağırarak SQLite şemasını
doğrudan model tanımlarından oluşturur (bkz. `Data/SeedData.cs`). Bu, "hafif mimari" ilkesine
uygun olarak en hızlı başlangıç deneyimini sağlar ve ilk sürümde ayrı bir migration yönetimi
gerektirmez.

## İleride gerçek migration'lara geçmek isterseniz

1. `Data/SeedData.cs` içindeki `EnsureCreatedAsync()` çağrısını kaldırın.
2. Package Manager Console'da (Server projesi seçiliyken):
   ```powershell
   Add-Migration InitialCreate
   Update-Database
   ```
   veya terminalden:
   ```bash
   dotnet ef migrations add InitialCreate --project Server --startup-project Server
   dotnet ef database update --project Server --startup-project Server
   ```
3. `Program.cs` içine `await db.Database.MigrateAsync();` ekleyin.

Not: `EnsureCreatedAsync()` ile `Migrate()` aynı anda kullanılmaz; ikisinden yalnızca biri seçilmelidir.
