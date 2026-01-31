using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Otomar.Application.Dtos.Cart;
using Otomar.Persistance.Options;
using System.Security.Claims;
using System.Text.Json;

namespace Otomar.Persistance.Helpers
{
    public static class CartHelper
    {
        private const string CartCookieName = "CartSessionId";
        private const string CartSessionHeader = "X-Cart-Session-Id";

        /// <summary>
        /// Kullanıcı için cart key'i döndürür (user veya session bazlı)
        /// Öncelik: 1) User ID, 2) Header, 3) Cookie
        /// </summary>
        public static string GetCartKey(HttpContext context, RedisOptions redisOptions)
        {
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                return $"{redisOptions.InstanceName}cart:user:{userId}";
            }

            var sessionId = GetOrCreateSessionId(context, redisOptions);
            return $"{redisOptions.InstanceName}cart:session:{sessionId}";
        }

        /// <summary>
        /// Session ID'yi header'dan veya cookie'den alır, yoksa yeni oluşturur
        /// Öncelik: 1) Header (WebApp'tan), 2) Cookie (doğrudan API erişimi)
        /// </summary>
        private static string GetOrCreateSessionId(HttpContext context, RedisOptions redisOptions)
        {
            // 1. Önce header'dan bak (WebApp'tan geliyor)
            var sessionId = context.Request.Headers[CartSessionHeader].FirstOrDefault();

            if (!string.IsNullOrEmpty(sessionId))
            {
                return sessionId;
            }

            // 2. Cookie'den bak (doğrudan API erişimi için fallback)
            sessionId = context.Request.Cookies[CartCookieName];

            if (!string.IsNullOrEmpty(sessionId))
            {
                RefreshCookie(context, sessionId, redisOptions.CartExpirationDays);
                return sessionId;
            }

            // 3. Yeni oluştur
            sessionId = Guid.NewGuid().ToString("N");
            RefreshCookie(context, sessionId, redisOptions.CartExpirationDays);
            return sessionId;
        }

        private static void RefreshCookie(HttpContext context, string sessionId, int expirationDays)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(expirationDays),
                Path = "/"
            };

            context.Response.Cookies.Append(CartCookieName, sessionId, cookieOptions);
        }

        /// <summary>
        /// Kullanıcı login olduğunda session sepetini user sepetine taşır/birleştirir
        /// </summary>
        public static async Task MergeCartsOnLoginAsync(
          HttpContext context,
          IDistributedCache cache,
          RedisOptions redisOptions,
          ShippingOptions shippingOptions)
        {
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return;

            // Header'dan veya cookie'den session ID al
            var sessionId = context.Request.Headers[CartSessionHeader].FirstOrDefault()
                         ?? context.Request.Cookies[CartCookieName];

            if (string.IsNullOrEmpty(sessionId))
                return;

            var sessionCartKey = $"{redisOptions.InstanceName}cart:session:{sessionId}";
            var userCartKey = $"{redisOptions.InstanceName}cart:user:{userId}";

            var sessionCartJson = await cache.GetStringAsync(sessionCartKey);
            if (string.IsNullOrEmpty(sessionCartJson))
                return;

            var sessionCart = JsonSerializer.Deserialize<CartDto>(sessionCartJson);
            if (sessionCart == null || !sessionCart.Items.Any())
                return;

            // User sepetini al veya oluştur
            var userCartJson = await cache.GetStringAsync(userCartKey);
            var userCart = string.IsNullOrEmpty(userCartJson)
                ? new CartDto()
                : JsonSerializer.Deserialize<CartDto>(userCartJson) ?? new CartDto();

            // Sepetleri birleştir
            foreach (var sessionItem in sessionCart.Items)
            {
                var existingItem = userCart.Items.FirstOrDefault(x => x.ProductId == sessionItem.ProductId);

                if (existingItem != null)
                {
                    existingItem.Quantity += sessionItem.Quantity;
                }
                else
                {
                    userCart.Items.Add(sessionItem);
                }
            }

            // Kargo hesapla
            var subTotal = userCart.Items.Sum(x => x.UnitPrice * x.Quantity);
            userCart.ShippingCost = subTotal >= shippingOptions.Threshold ? 0 : shippingOptions.Cost;

            // Kaydet
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(redisOptions.CartExpirationDays)
            };

            await cache.SetStringAsync(userCartKey, JsonSerializer.Serialize(userCart), cacheOptions);

            // Session sepetini sil
            await cache.RemoveAsync(sessionCartKey);

            // Cookie'yi temizle (artık user cart kullanılacak)
            context.Response.Cookies.Delete(CartCookieName);
        }

        /// <summary>
        /// Session ID'yi döndürür (header veya cookie'den)
        /// </summary>
        public static string? GetSessionId(HttpContext context)
        {
            return context.Request.Headers[CartSessionHeader].FirstOrDefault()
                ?? context.Request.Cookies[CartCookieName];
        }
    }
}