using Microsoft.AspNetCore.Identity;

namespace ErzurumFlight.Server.Models;

/// <summary>
/// ASP.NET Core Identity kullanıcı sınıfı. İlk sürümde yalnızca Admin girişi için kullanılır.
/// Şifre hash'leme, cookie authentication ve authorization Identity üzerinden sağlanır.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
