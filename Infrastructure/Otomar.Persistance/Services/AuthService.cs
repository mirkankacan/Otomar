using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Otomar.Contracts.Common;
using Otomar.Application.Contracts.Providers;
using Otomar.Application.Contracts.Services;
using Otomar.Contracts.Dtos.Auth;
using Otomar.Domain.Entities;
using System.Net;

namespace Otomar.Persistance.Services
{
    public class AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtProvider jwtProvider,
        IHttpContextAccessor httpContextAccessor) : IAuthService
    {
        public async Task<ServiceResult<TokenDto>> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    return ServiceResult<TokenDto>.Error(
                        "Geçersiz Giriş Bilgileri",
                        "Email ve şifre boş geçilemez",
                        HttpStatusCode.BadRequest);
                }

                var user = await userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    return ServiceResult<TokenDto>.Error(
                        "Kullanıcı Bulunamadı",
                        "Email veya şifre hatalı",
                        HttpStatusCode.Unauthorized);
                }

                if (!user.IsActive)
                {
                    return ServiceResult<TokenDto>.Error(
                        "Hesap Pasif",
                        "Hesabınız pasif durumda. Lütfen yöneticinizle iletişime geçin.",
                        HttpStatusCode.Forbidden);
                }

                var signInResult = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: false);
                if (!signInResult.Succeeded)
                {
                    return ServiceResult<TokenDto>.Error(
                        "Giriş Başarısız",
                        "Email veya şifre hatalı",
                        HttpStatusCode.Unauthorized);
                }

                var tokenResult = await jwtProvider.CreateTokenAsync(user);
                return tokenResult;
            }
            catch (Exception ex)
            {
                return ServiceResult<TokenDto>.Error(
                    "Giriş Hatası",
                    $"Giriş işlemi sırasında bir hata oluştu: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<TokenDto>> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (registerDto == null)
                {
                    return ServiceResult<TokenDto>.Error(
                        "Geçersiz Kayıt Bilgileri",
                        "Kayıt bilgileri boş geçilemez",
                        HttpStatusCode.BadRequest);
                }

                if (string.IsNullOrWhiteSpace(registerDto.Email) ||
                    string.IsNullOrWhiteSpace(registerDto.Password) ||
                    string.IsNullOrWhiteSpace(registerDto.Name) ||
                    string.IsNullOrWhiteSpace(registerDto.Surname))
                {
                    return ServiceResult<TokenDto>.Error(
                        "Eksik Bilgi",
                        "Ad, soyad, email ve şifre alanları zorunludur",
                        HttpStatusCode.BadRequest);
                }

                if (registerDto.Password != registerDto.ConfirmPassword)
                {
                    return ServiceResult<TokenDto>.Error(
                        "Şifre Uyuşmazlığı",
                        "Şifre ve şifre tekrarı eşleşmiyor",
                        HttpStatusCode.BadRequest);
                }

                var existingUser = await userManager.FindByEmailAsync(registerDto.Email);
                if (existingUser != null)
                {
                    return ServiceResult<TokenDto>.Error(
                        "Kullanıcı Mevcut",
                        "Bu email adresi ile zaten bir kullanıcı kayıtlı",
                        HttpStatusCode.Conflict);
                }

                var user = new ApplicationUser
                {
                    UserName = registerDto.Email,
                    Email = registerDto.Email,
                    Name = registerDto.Name,
                    Surname = registerDto.Surname,
                    PhoneNumber = registerDto.PhoneNumber,
                    CommerceMail = registerDto.IsCommerceMailAccepted,
                    ClarificationAgreement = registerDto.IsClarificationAgreementAccepted,
                    MembershipAgreement = registerDto.IsMembershipAgreementAccepted,
                    IsActive = true
                };

                var createResult = await userManager.CreateAsync(user, registerDto.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return ServiceResult<TokenDto>.Error(
                        "Kayıt Başarısız",
                        $"Kullanıcı oluşturulamadı: {errors}",
                        HttpStatusCode.BadRequest);
                }

                // Default role atama (gerekirse)
                // await _userManager.AddToRoleAsync(user, "User");

                var tokenResult = await jwtProvider.CreateTokenAsync(user);
                return tokenResult;
            }
            catch (Exception ex)
            {
                return ServiceResult<TokenDto>.Error(
                    "Kayıt Hatası",
                    $"Kayıt işlemi sırasında bir hata oluştu: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<bool>> LogoutAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = httpContextAccessor.HttpContext?.User?.Claims
                    .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResult<bool>.Error(
                        "Kullanıcı Bulunamadı",
                        "Oturum bilgisi bulunamadı",
                        HttpStatusCode.Unauthorized);
                }

                var user = await userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    // Refresh token'ı temizle
                    user.RefreshToken = null;
                    user.RefreshTokenExpires = null;
                    await userManager.UpdateAsync(user);
                }

                await signInManager.SignOutAsync();

                return ServiceResult<bool>.SuccessAsOk(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Error(
                    "Çıkış Hatası",
                    $"Çıkış işlemi sırasında bir hata oluştu: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<TokenDto>> RefreshTokenAsync(CreateTokenByRefreshTokenDto createTokenByRefreshTokenDto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (createTokenByRefreshTokenDto == null || string.IsNullOrWhiteSpace(createTokenByRefreshTokenDto.RefreshToken))
                {
                    return ServiceResult<TokenDto>.Error(
                        "Geçersiz Refresh Token",
                        "Refresh token boş geçilemez",
                        HttpStatusCode.BadRequest);
                }

                var user = await userManager.Users
                    .FirstOrDefaultAsync(u => u.RefreshToken == createTokenByRefreshTokenDto.RefreshToken, cancellationToken);

                if (user == null)
                {
                    return ServiceResult<TokenDto>.Error(
                        "Geçersiz Refresh Token",
                        "Refresh token bulunamadı",
                        HttpStatusCode.Unauthorized);
                }

                if (!user.IsActive)
                {
                    return ServiceResult<TokenDto>.Error(
                        "Hesap Pasif",
                        "Hesabınız pasif durumda",
                        HttpStatusCode.Forbidden);
                }

                if (user.RefreshTokenExpires == null || user.RefreshTokenExpires < DateTime.UtcNow)
                {
                    return ServiceResult<TokenDto>.Error(
                        "Refresh Token Süresi Dolmuş",
                        "Refresh token'ın süresi dolmuş. Lütfen tekrar giriş yapın.",
                        HttpStatusCode.Unauthorized);
                }
                // ESKİ REFRESH TOKEN'I GEÇERSİZ KIL (Rotating Refresh Token Pattern)
                user.RefreshToken = null; // Geçici olarak null yap
                user.RefreshTokenExpires = null; // Geçici olarak null yap
                await userManager.UpdateAsync(user);
                var tokenResult = await jwtProvider.CreateTokenAsync(user);
                return tokenResult;
            }
            catch (Exception ex)
            {
                return ServiceResult<TokenDto>.Error(
                    "Token Yenileme Hatası",
                    $"Token yenileme işlemi sırasında bir hata oluştu: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto, CancellationToken cancellationToken = default)
        {
            try
            {
                // ResetPasswordDto henüz implement edilmemiş, placeholder olarak bırakıldı
                // İleride email ile şifre sıfırlama token'ı gönderilmesi ve yeni şifre belirlenmesi için kullanılacak

                return ServiceResult<bool>.Error(
                    "Henüz Implement Edilmedi",
                    "Şifre sıfırlama özelliği henüz implement edilmemiştir",
                    HttpStatusCode.NotImplemented);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Error(
                    "Şifre Sıfırlama Hatası",
                    $"Şifre sıfırlama işlemi sırasında bir hata oluştu: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }
    }
}