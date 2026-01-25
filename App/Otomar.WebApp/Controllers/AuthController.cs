using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Dtos.Auth;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [Route("giris")]
    public class AuthController(IAuthApi authApi) : Controller
    {
        [HttpGet("")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet("kayit")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("giris-yap")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken = default)
        {
            return await authApi.LoginAsync(dto, cancellationToken).ToActionResultAsync();
        }

        [HttpPost("kayit-ol")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken = default)
        {
            return await authApi.RegisterAsync(dto, cancellationToken).ToActionResultAsync();
        }

        [HttpPost("cikis")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
        {
            return await authApi.LogoutAsync(cancellationToken).ToActionResultAsync();
        }

        [HttpPost("token-yenile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshToken([FromBody] CreateTokenByRefreshTokenDto dto, CancellationToken cancellationToken = default)
        {
            return await authApi.RefreshTokenAsync(dto, cancellationToken).ToActionResultAsync();
        }

        [HttpPost("sifre-sifirla")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken cancellationToken = default)
        {
            return await authApi.ResetPasswordAsync(dto, cancellationToken).ToActionResultAsync();
        }
    }
}
