using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Otomar.WebApp.Dtos.Auth;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Refit;

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

    public BearerTokenHandler(IHttpContextAccessor httpContextAccessor, IAuthApi authApi)
    {
        _httpContextAccessor = httpContextAccessor;
        _authApi = authApi;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        var accessToken = context != null
            ? await context.GetTokenAsync(AccessTokenName).ConfigureAwait(false)
            : null;

        if (!string.IsNullOrEmpty(accessToken))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        byte[]? contentBuffer = null;
        System.Net.Http.Headers.MediaTypeHeaderValue? contentType = null;
        if (request.Content != null)
        {
            contentType = request.Content.Headers.ContentType;
            contentBuffer = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var newContent = new ByteArrayContent(contentBuffer);
            if (contentType != null)
                newContent.Headers.ContentType = contentType;
            request.Content = newContent;
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Unauthorized || context == null)
            return response;

        var refreshToken = await context.GetTokenAsync(RefreshTokenName).ConfigureAwait(false);
        if (string.IsNullOrEmpty(refreshToken))
            return response;

        TokenDto? newTokenDto;
        try
        {
            newTokenDto = await _authApi.RefreshTokenAsync(
                new CreateTokenByRefreshTokenDto { RefreshToken = refreshToken },
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return response;
        }

        if (newTokenDto == null || string.IsNullOrEmpty(newTokenDto.Token))
            return response;

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

        var retryRequest = CloneRequest(request, newTokenDto.Token, contentBuffer);
        try
        {
            return await base.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            retryRequest.Dispose();
        }
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage original, string newAccessToken, byte[]? contentBuffer)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        if (contentBuffer != null && contentBuffer.Length > 0)
        {
            var content = new ByteArrayContent(contentBuffer);
            if (original.Content?.Headers.ContentType != null)
                content.Headers.ContentType = original.Content.Headers.ContentType;
            clone.Content = content;
        }

        foreach (var header in original.Headers)
        {
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                continue;
            foreach (var value in header.Value)
                clone.Headers.TryAddWithoutValidation(header.Key, value);
        }

        clone.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken);
        return clone;
    }
}
