using Microsoft.AspNetCore.Authentication.Cookies;
using Otomar.Contracts.Dtos.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Otomar.WebApp.Extensions;

public static class CookieAuthExtensions
{
    /// <summary>
    /// TokenDto ve JWT'den (varsa) ClaimsPrincipal oluşturur. IdentityService ile uyumlu claim tipleri kullanılır.
    /// </summary>
    public static ClaimsPrincipal BuildPrincipalFromToken(TokenDto tokenDto, string? fallbackEmail = null)
    {
        var claims = new List<Claim>();

        string? userId = null;
        string? email = null;

        if (!string.IsNullOrEmpty(tokenDto.Token))
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(tokenDto.Token);
                userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                email = jwt.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == ClaimTypes.Email || c.Type == JwtRegisteredClaimNames.Email)?.Value;
            }
            catch
            {
                // JWT parse edilemezse fallback kullanılır
            }
        }

        claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        claims.Add(new Claim(ClaimTypes.Email, email ?? fallbackEmail ?? ""));
        claims.Add(new Claim(ClaimTypes.GivenName, tokenDto.FullName ?? ""));
        claims.Add(new Claim(ClaimTypes.MobilePhone, tokenDto.PhoneNumber ?? ""));
        claims.Add(new Claim(ClaimTypes.Role, tokenDto.RoleName ?? ""));
        claims.Add(new Claim("ClientCode", tokenDto.ClientCode ?? null));
        claims.Add(new Claim("IsPaymentExempt", tokenDto.IsPaymentExempt.ToString() ?? "false"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}