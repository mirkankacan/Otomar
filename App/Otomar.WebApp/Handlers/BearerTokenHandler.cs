using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Otomar.WebApp.Dtos.Auth;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Refit;
using System.Net;

namespace Otomar.WebApp.Handlers;

/// <summary>
/// Refit / HttpClient için: İsteklere otomatik Bearer token ekler; 401 alırsa refresh token ile yeniler ve isteği tekrar dener.
/// </summary>
public class BearerTokenHandler : DelegatingHandler
{
    private const string AccessTokenName = "access_token";
    private const string RefreshTokenName = "refresh_token";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthApi _authApi;
    private readonly ILogger<BearerTokenHandler> _logger;

    public BearerTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        IAuthApi authApi,
        ILogger<BearerTokenHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _authApi = authApi;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;

        // Token'ı al ve ekle
        var accessToken = context != null
            ? await context.GetTokenAsync(AccessTokenName).ConfigureAwait(false)
            : null;

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // 401 değilse veya context yoksa direkt dön
        if (response.StatusCode != HttpStatusCode.Unauthorized || context == null)
            return response;

        _logger.LogInformation("401 Unauthorized alındı, token yenileniyor");

        // Refresh token'ı al
        var refreshToken = await context.GetTokenAsync(RefreshTokenName).ConfigureAwait(false);
        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("Refresh token bulunamadı");
            return response;
        }

        // Token'ı yenile
        TokenDto? newTokenDto;
        try
        {
            newTokenDto = await _authApi.RefreshTokenAsync(
                new CreateTokenByRefreshTokenDto { RefreshToken = refreshToken },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token yenileme işlemi başarısız");
            return response;
        }

        if (newTokenDto == null || string.IsNullOrEmpty(newTokenDto.Token))
        {
            _logger.LogWarning("Token yenileme işlemi boş veya geçersiz token döndü");
            return response;
        }

        // Yeni token'ı cookie'ye kaydet
        var principal = CookieAuthExtensions.BuildPrincipalFromToken(newTokenDto);
        var props = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
        };
        props.StoreTokens(new[]
        {
            new AuthenticationToken { Name = AccessTokenName, Value = newTokenDto.Token },
            new AuthenticationToken { Name = RefreshTokenName, Value = newTokenDto.RefreshToken ?? string.Empty }
        });

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            props).ConfigureAwait(false);

        _logger.LogInformation("Token başarıyla yenilendi, istek tekrar gönderiliyor");

        // Retry request'i oluştur
        HttpRequestMessage? retryRequest = null;
        try
        {
            retryRequest = await CloneRequestAsync(request, newTokenDto.Token, cancellationToken)
                .ConfigureAwait(false);

            return await base.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token yenilendikten sonra istek klonlama veya tekrar gönderme başarısız");

            // Clone başarısız olursa orijinal 401 response'u dön
            return response;
        }
        finally
        {
            retryRequest?.Dispose();
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage original,
        string newAccessToken,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version
        };

        // Content'i clone et (stream tabanlı içerik tekrar okunamaz - 401 retry'da body gönderilmez)
        if (original.Content != null)
        {
            // Stream veya multipart içerik zaten ilk istekte tüketildiği için tekrar okunamaz
            if (original.Content is StreamContent or MultipartFormDataContent)
            {
                throw new InvalidOperationException(
                    "Stream veya multipart/form-data içerik 401 sonrası tekrar gönderilemez. " +
                    "İsteği sayfadan yenileyip tekrar deneyin.");
            }

            // ByteArrayContent, StringContent vb. için güvenli clone
            var contentBytes = await original.Content.ReadAsByteArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Request headers'ı kopyala (Authorization hariç - yenisini ekleyeceğiz)
        foreach (var header in original.Headers)
        {
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                continue;

            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Yeni Bearer token'ı ekle
        clone.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken);

        // Options'ları kopyala (.NET 5+)
        foreach (var prop in original.Options)
        {
            clone.Options.TryAdd(prop.Key, prop.Value);
        }

        return clone;
    }
}