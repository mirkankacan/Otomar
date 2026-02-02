using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Providers;
using Otomar.Application.Dtos.Auth;
using Otomar.Domain.Entities;
using Otomar.Persistance.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Otomar.Persistance.Authentication
{
    public class JwtProvider(JwtOptions jwtOptions, UserManager<ApplicationUser> userManager) : IJwtProvider
    {
        public async Task<ServiceResult<TokenDto>> CreateTokenAsync(ApplicationUser applicationUser)
        {
            // Kullanıcının rollerini al
            var userRoles = await userManager.GetRolesAsync(applicationUser);
            var user = await userManager.FindByIdAsync(applicationUser.Id);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, NewId.NextGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, applicationUser.Id),
                new Claim(ClaimTypes.NameIdentifier, applicationUser.Id),
                new Claim(ClaimTypes.Email, applicationUser.Email ?? string.Empty),
                new Claim(ClaimTypes.GivenName, $"{applicationUser.Name} {applicationUser.Surname}"),
                new Claim(ClaimTypes.Role, userRoles.FirstOrDefault()!),
                new Claim(ClaimTypes.MobilePhone, applicationUser.PhoneNumber!),
                new Claim("ClientCode", applicationUser.ClientCode ?? string.Empty),
                new Claim("IsPaymentExempt", applicationUser.IsPaymentExempt.ToString())
            };

            DateTime expires = DateTime.UtcNow.AddHours(1);
            var jwtSecurityToken = new JwtSecurityToken(
                issuer: jwtOptions.Issuer,
                audience: jwtOptions.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    SecurityAlgorithms.HmacSha256));

            string token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            applicationUser.RefreshToken = refreshToken;
            applicationUser.RefreshTokenExpires = DateTime.UtcNow.AddDays(7);
            await userManager.UpdateAsync(applicationUser);

            TokenDto response = new()
            {
                Token = token,
                TokenExpires = expires,
                RefreshToken = refreshToken,
                RefreshTokenExpires = applicationUser.RefreshTokenExpires,
                FullName = $"{applicationUser.Name} {applicationUser.Surname}",
                RoleName = userRoles.FirstOrDefault()!,
                ClientCode = applicationUser.ClientCode ?? string.Empty,
                PhoneNumber = applicationUser.PhoneNumber,
                IsPaymentExempt = applicationUser.IsPaymentExempt
            };
            return ServiceResult<TokenDto>.SuccessAsCreated(response, $"/api/user/{applicationUser.Id}");
        }
    }
}