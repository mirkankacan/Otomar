using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Otomar.Application.Interfaces.Services;
using Otomar.Application.Options;
using Otomar.Shared.Dtos.Cart;
using System.Security.Claims;
using System.Text.Json;

namespace Otomar.WebApi.Services
{
    /// <summary>
    /// HTTP isteğinden sepet oturumunu yöneten servis.
    /// Cart key, session ID ve sepet birleştirme işlemlerini HttpContext üzerinden sağlar.
    /// </summary>
    public class CartSessionService : ICartSessionService
    {
        private const string CartCookieName = "CartSessionId";
        private const string CartSessionHeader = "X-Cart-Session-Id";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDistributedCache _cache;
        private readonly RedisOptions _redisOptions;
        private readonly ShippingOptions _shippingOptions;
        private readonly ILogger<CartSessionService> _logger;

        public CartSessionService(
            IHttpContextAccessor httpContextAccessor,
            IDistributedCache cache,
            RedisOptions redisOptions,
            ShippingOptions shippingOptions,
            ILogger<CartSessionService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
            _redisOptions = redisOptions;
            _shippingOptions = shippingOptions;
            _logger = logger;
        }

        /// <inheritdoc />
        public string GetCartKey()
        {
            var context = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("HttpContext mevcut değil.");

            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
                return $"{_redisOptions.InstanceName}cart:user:{userId}";

            var sessionId = GetOrCreateSessionId(context);
            return $"{_redisOptions.InstanceName}cart:session:{sessionId}";
        }

        /// <inheritdoc />
        public string? GetSessionId()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return null;

            return context.Request.Headers[CartSessionHeader].FirstOrDefault()
                ?? context.Request.Cookies[CartCookieName];
        }

        /// <inheritdoc />
        public async Task MergeCartsOnLoginAsync(CancellationToken cancellationToken = default)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return;

            var sessionId = context.Request.Headers[CartSessionHeader].FirstOrDefault()
                         ?? context.Request.Cookies[CartCookieName];

            if (string.IsNullOrEmpty(sessionId))
                return;

            var sessionCartKey = $"{_redisOptions.InstanceName}cart:session:{sessionId}";
            var userCartKey = $"{_redisOptions.InstanceName}cart:user:{userId}";

            var sessionCartJson = await _cache.GetStringAsync(sessionCartKey, cancellationToken);
            if (string.IsNullOrEmpty(sessionCartJson))
                return;

            var sessionCart = JsonSerializer.Deserialize<CartDto>(sessionCartJson);
            if (sessionCart == null || sessionCart.Items.Count == 0)
                return;

            var userCartJson = await _cache.GetStringAsync(userCartKey, cancellationToken);
            var userCart = string.IsNullOrEmpty(userCartJson)
                ? new CartDto()
                : JsonSerializer.Deserialize<CartDto>(userCartJson) ?? new CartDto();

            foreach (var sessionItem in sessionCart.Items)
            {
                var existingItem = userCart.Items.FirstOrDefault(x => x.ProductId == sessionItem.ProductId);

                if (existingItem != null)
                    existingItem.Quantity += sessionItem.Quantity;
                else
                    userCart.Items.Add(sessionItem);
            }

            var subTotal = userCart.Items.Sum(x => x.UnitPrice * x.Quantity);
            userCart.ShippingCost = subTotal >= _shippingOptions.Threshold ? 0 : _shippingOptions.Cost;

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(_redisOptions.CartExpirationDays)
            };

            await _cache.SetStringAsync(userCartKey, JsonSerializer.Serialize(userCart), cacheOptions, cancellationToken);
            await _cache.RemoveAsync(sessionCartKey, cancellationToken);

            context.Response.Cookies.Delete(CartCookieName);

            _logger.LogInformation("Session sepeti user sepetine birleştirildi. SessionId: {SessionId}, UserId: {UserId}", sessionId, userId);
        }

        private string GetOrCreateSessionId(HttpContext context)
        {
            var sessionId = context.Request.Headers[CartSessionHeader].FirstOrDefault();
            if (!string.IsNullOrEmpty(sessionId))
                return sessionId;

            sessionId = context.Request.Cookies[CartCookieName];
            if (!string.IsNullOrEmpty(sessionId))
            {
                RefreshCookie(context, sessionId);
                return sessionId;
            }

            sessionId = Guid.NewGuid().ToString("N");
            RefreshCookie(context, sessionId);
            return sessionId;
        }

        private void RefreshCookie(HttpContext context, string sessionId)
        {
            if (context.Response.HasStarted)
                return;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(_redisOptions.CartExpirationDays),
                Path = "/"
            };

            context.Response.Cookies.Append(CartCookieName, sessionId, cookieOptions);
        }
    }
}
