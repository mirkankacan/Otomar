using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Otomar.Shared.Common;
using Otomar.Application.Interfaces.Services;
using Otomar.Shared.Dtos.Auth;
using Otomar.Domain.Entities;
using Otomar.Application.Interfaces.Repositories;
using System.Net;
using System.Text;

namespace Otomar.Application.Services
{
    public class AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtProvider jwtProvider,
        IHttpContextAccessor httpContextAccessor,
        IUserRepository panelUserRepository,
        IEmailService emailService,
        ILogger<AuthService> logger) : IAuthService
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

                // Panel kullanicisi kontrolu: Identity'de yoksa panel tablosundan otomatik provision
                if (user == null)
                {
                    var provisionResult = await TryProvisionFromPanelAsync(loginDto.Email);
                    if (!provisionResult.IsSuccess)
                    {
                        return ServiceResult<TokenDto>.Error(
                            "Giriş Başarısız",
                            "E-posta veya şifre hatalı",
                            HttpStatusCode.BadRequest);
                    }
                    user = provisionResult.User;
                }

                if (!user.IsActive)
                {
                    return ServiceResult<TokenDto>.Error(
                        "Hesap Pasif",
                        "Hesabınız pasif durumda. Lütfen yöneticinizle iletişime geçin.",
                        HttpStatusCode.Forbidden);
                }

                // Sifre kontrolu: Identity sifre eslesmezse, panel sifresiyle senkronize et
                var signInResult = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: false);
                if (!signInResult.Succeeded)
                {
                    var syncResult = await TrySyncPanelPasswordAsync(user, loginDto.Password);
                    if (!syncResult)
                    {
                        return ServiceResult<TokenDto>.Error(
                            "Giriş Başarısız",
                            "E-posta veya şifre hatalı",
                            HttpStatusCode.BadRequest);
                    }
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

        /// <summary>
        /// Panel tablosunda kullanici varsa Identity'ye otomatik kayit olusturur.
        /// </summary>
        private async Task<(bool IsSuccess, ApplicationUser? User)> TryProvisionFromPanelAsync(string email)
        {
            try
            {
                var panelUser = await panelUserRepository.GetPanelUserByUsernameAsync(email);

                if (panelUser == null)
                    return (false, null);

                var newUser = new ApplicationUser
                {
                    UserName = panelUser.KullaniciAdi,
                    Email = panelUser.KullaniciAdi,
                    Name = panelUser.CariIsim ?? panelUser.KullaniciAdi,
                    EmailConfirmed = true,
                    ClientCode = panelUser.CariKod,
                    CommerceMail = true,
                    ClarificationAgreement = true,
                    MembershipAgreement = true,
                    NoHashPassword = panelUser.Sifre,
                    IsPaymentExempt = false,
                    IsActive = true
                };

                var createResult = await userManager.CreateAsync(newUser, panelUser.Sifre);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    logger.LogWarning("Panel kullanicisi Identity'ye kaydedilemedi: {Email}, Hatalar: {Errors}", email, errors);
                    return (false, null);
                }

                await userManager.AddToRoleAsync(newUser, "User");
                logger.LogInformation("Panel kullanicisi Identity'ye otomatik kaydedildi: {Email}, CariKod: {CariKod}", email, panelUser.CariKod);

                return (true, newUser);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Panel kullanici sorgusu basarisiz: {Email}", email);
                return (false, null);
            }
        }

        /// <summary>
        /// Identity sifresi eslesmediginde panel tablosundaki sifre ile senkronize eder.
        /// Panel sifresi girilen sifre ile eslesiyorsa Identity sifresini gunceller.
        /// </summary>
        private async Task<bool> TrySyncPanelPasswordAsync(ApplicationUser user, string attemptedPassword)
        {
            try
            {
                var panelUser = await panelUserRepository.GetPanelUserByUsernameAsync(user.Email!);

                if (panelUser == null || panelUser.Sifre != attemptedPassword)
                    return false;

                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await userManager.ResetPasswordAsync(user, resetToken, panelUser.Sifre);
                if (!resetResult.Succeeded)
                {
                    var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                    logger.LogWarning("Panel kullanicisi sifre senkronizasyonu basarisiz: {Email}, Hatalar: {Errors}", user.Email, errors);
                    return false;
                }

                user.NoHashPassword = panelUser.Sifre;
                await userManager.UpdateAsync(user);

                logger.LogInformation("Panel kullanicisi sifresi senkronize edildi: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Panel sifre senkronizasyonu sorgusu basarisiz: {Email}", user.Email);
                return false;
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

                var user = userManager.Users
                    .FirstOrDefault(u => u.RefreshToken == createTokenByRefreshTokenDto.RefreshToken);

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

        public async Task<ServiceResult<bool>> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (forgotPasswordDto == null || string.IsNullOrWhiteSpace(forgotPasswordDto.Email))
                {
                    return ServiceResult<bool>.Error(
                        "Geçersiz İstek",
                        "E-posta adresi boş geçilemez",
                        HttpStatusCode.BadRequest);
                }

                // Kullanıcı bulunamasa bile başarılı döndür — e-posta numaralandırma saldırısını önler
                var user = await userManager.FindByEmailAsync(forgotPasswordDto.Email);
                if (user == null || !user.IsActive)
                {
                    logger.LogInformation("Şifre sıfırlama isteği yapıldı ancak kullanıcı bulunamadı veya pasif: {Email}", forgotPasswordDto.Email);
                    return ServiceResult<bool>.SuccessAsOk(true);
                }

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var resetUrl = $"{forgotPasswordDto.BaseUrl}/sifre-sifirla?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";

                await emailService.SendPasswordResetEmailAsync(user.Email!, user.Name ?? user.Email!, resetUrl, cancellationToken);

                logger.LogInformation("Şifre sıfırlama e-postası gönderildi: {Email}", user.Email);
                return ServiceResult<bool>.SuccessAsOk(true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Şifre sıfırlama e-postası gönderilirken hata oluştu: {Email}", forgotPasswordDto?.Email);
                return ServiceResult<bool>.Error(
                    "Şifre Sıfırlama Hatası",
                    $"İşlem sırasında bir hata oluştu: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (resetPasswordDto == null ||
                    string.IsNullOrWhiteSpace(resetPasswordDto.Email) ||
                    string.IsNullOrWhiteSpace(resetPasswordDto.Token) ||
                    string.IsNullOrWhiteSpace(resetPasswordDto.Password))
                {
                    return ServiceResult<bool>.Error(
                        "Geçersiz İstek",
                        "E-posta, token ve şifre alanları zorunludur",
                        HttpStatusCode.BadRequest);
                }

                if (resetPasswordDto.Password != resetPasswordDto.ConfirmPassword)
                {
                    return ServiceResult<bool>.Error(
                        "Şifre Uyuşmazlığı",
                        "Şifre ve şifre tekrarı eşleşmiyor",
                        HttpStatusCode.BadRequest);
                }

                var user = await userManager.FindByEmailAsync(resetPasswordDto.Email);
                if (user == null || !user.IsActive)
                {
                    return ServiceResult<bool>.Error(
                        "Geçersiz İstek",
                        "Şifre sıfırlama işlemi gerçekleştirilemedi",
                        HttpStatusCode.BadRequest);
                }

                var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetPasswordDto.Token));
                var result = await userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDto.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger.LogWarning("Şifre sıfırlama başarısız: {Email}, Hatalar: {Errors}", user.Email, errors);
                    return ServiceResult<bool>.Error(
                        "Şifre Sıfırlama Başarısız",
                        "Geçersiz veya süresi dolmuş bağlantı. Lütfen tekrar deneyin.",
                        HttpStatusCode.BadRequest);
                }

                user.NoHashPassword = null;
                await userManager.UpdateAsync(user);
                await userManager.UpdateSecurityStampAsync(user);

                logger.LogInformation("Şifre başarıyla sıfırlandı: {Email}", user.Email);
                return ServiceResult<bool>.SuccessAsOk(true);
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
