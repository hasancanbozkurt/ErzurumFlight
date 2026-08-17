using ErzurumFlight.Server.DTOs;
using ErzurumFlight.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ErzurumFlight.Server.Controllers;

/// <summary>
/// Admin girişi için cookie tabanlı authentication. Şartname bölüm 20: ASP.NET Core Identity,
/// password hashing, secure cookie. API key/token frontend'e asla gönderilmez.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _signInManager.PasswordSignInAsync(
            request.UserName, request.Password, isPersistent: true, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized(new { error = "Kullanıcı adı veya parola hatalı." });
        }

        return Ok(new { message = "Giriş başarılı." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Çıkış yapıldı." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new { userName = user.UserName, displayName = user.DisplayName });
    }
}
