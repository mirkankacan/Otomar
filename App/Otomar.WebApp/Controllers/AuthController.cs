using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Otomar.Contracts.Dtos.Auth;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Filters;
using Otomar.WebApp.Services.Refit;
using Refit;

namespace Otomar.WebApp.Controllers
{
    [AllowAnonymous]
    [Route("")]
    public class AuthController(IAuthApi authApi) : Controller
    {
        [HttpGet("giris-yap")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet("kayit-ol")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("giris-yap")]
        [ValidateAntiForgeryToken]
        [ValidateRecaptcha("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var tokenDto = await authApi.LoginAsync(dto, cancellationToken);
                var principal = CookieAuthExtensions.BuildPrincipalFromToken(tokenDto, fallbackEmail: dto.Email);
                var props = new AuthenticationProperties
                {
                    IsPersistent = dto.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                };
                props.StoreTokens(new[]
                {
                    new AuthenticationToken { Name = "access_token", Value = tokenDto.Token ?? string.Empty },
                    new AuthenticationToken { Name = "refresh_token", Value = tokenDto.RefreshToken ?? string.Empty }
                });
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
                return Ok(tokenDto);
            }
            catch (ApiException ex)
            {
                return new ObjectResult(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = ex.ReasonPhrase ?? "Giriş başarısız",
                    status = ex.StatusCode,
                    detail = ex.Content ?? ex.Message
                })
                {
                    StatusCode = (int)ex.StatusCode
                };
            }
        }

        [HttpPost("kayit-ol")]
        [ValidateAntiForgeryToken]
        [ValidateRecaptcha("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var tokenDto = await authApi.RegisterAsync(dto, cancellationToken);
                var principal = CookieAuthExtensions.BuildPrincipalFromToken(tokenDto, fallbackEmail: dto.Email);
                var props = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                };
                props.StoreTokens(new[]
                {
                    new AuthenticationToken { Name = "access_token", Value = tokenDto.Token ?? string.Empty },
                    new AuthenticationToken { Name = "refresh_token", Value = tokenDto.RefreshToken ?? string.Empty }
                });
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
                return Ok(tokenDto);
            }
            catch (ApiException ex)
            {
                return new ObjectResult(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = ex.ReasonPhrase ?? "Kayıt başarısız",
                    status = ex.StatusCode,
                    detail = ex.Content ?? ex.Message
                })
                {
                    StatusCode = (int)ex.StatusCode
                };
            }
        }
        [HttpPost("cikis-yap")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            try
            {
                await authApi.LogoutAsync(cancellationToken);
            }
            catch
            {
                return Ok();
            }
            return Ok();
        }

        [HttpPost("token-yenile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshToken([FromBody] CreateTokenByRefreshTokenDto dto, CancellationToken cancellationToken = default)
        {
            return await authApi.RefreshTokenAsync(dto, cancellationToken).ToActionResultAsync();
        }

        [HttpPost("sifre-sifirla")]
        [ValidateAntiForgeryToken]
        [ValidateRecaptcha("password_reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken cancellationToken = default)
        {
            return await authApi.ResetPasswordAsync(dto, cancellationToken).ToActionResultAsync();
        }
    }
}