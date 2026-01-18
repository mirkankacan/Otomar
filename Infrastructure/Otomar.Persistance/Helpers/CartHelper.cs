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

        /// <summary>
        /// Kullanıcı için cart key'i döndürür (user veya session bazlı)
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
        /// Session ID'yi cookie'den alır veya yeni oluşturur
        /// </summary>
        private static string GetOrCreateSessionId(HttpContext context, RedisOptions redisOptions)
        {
            var sessionId = context.Request.Cookies[CartCookieName];

            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Strict,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(redisOptions.CartExpirationDays),
                    Path = "/"
                };

                context.Response.Cookies.Append(CartCookieName, sessionId, cookieOptions);
            }

            return sessionId;
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

            var sessionId = context.Request.Cookies[CartCookieName];

            if (string.IsNullOrEmpty(sessionId))
                return;

            var sessionCartKey = $"{redisOptions.InstanceName}cart:session:{sessionId}";
            var userCartKey = $"{redisOptions.InstanceName}cart:user:{userId}";

            // Session sepetini al
            var sessionCartJson = await cache.GetStringAsync(sessionCartKey);

            if (string.IsNullOrEmpty(sessionCartJson))
                return;

            var sessionCart = JsonSerializer.Deserialize<CartDto>(sessionCartJson);

            if (sessionCart == null || !sessionCart.Items.Any())
                return;

            // User sepetini al
            var userCartJson = await cache.GetStringAsync(userCartKey);

            if (string.IsNullOrEmpty(userCartJson))
            {
                // User sepeti yoksa, session sepetini direkt taşı
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(redisOptions.CartExpirationDays)
                };

                await cache.SetStringAsync(userCartKey, sessionCartJson, cacheOptions);
            }
            else
            {
                // İki sepeti birleştir
                var userCart = JsonSerializer.Deserialize<CartDto>(userCartJson);

                if (userCart == null)
                    return;

                foreach (var sessionItem in sessionCart.Items)
                {
                    var existingItem = userCart.Items
                        .FirstOrDefault(x => x.ProductId == sessionItem.ProductId);

                    if (existingItem != null)
                    {
                        // Aynı ürün varsa miktarları topla
                        existingItem.Quantity += sessionItem.Quantity;
                    }
                    else
                    {
                        // Yeni ürünse ekle
                        userCart.Items.Add(sessionItem);
                    }
                }

                // Kargo ücretini hesapla
                var subTotal = userCart.Items.Sum(x => x.UnitPrice * x.Quantity);
                userCart.ShippingCost = subTotal >= shippingOptions.Threshold
                    ? 0
                    : shippingOptions.Cost;

                // Birleştirilmiş sepeti kaydet
                var updatedCartJson = JsonSerializer.Serialize(userCart);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(redisOptions.CartExpirationDays)
                };

                await cache.SetStringAsync(userCartKey, updatedCartJson, cacheOptions);
            }

            // Session sepetini sil
            await cache.RemoveAsync(sessionCartKey);

            // Session cookie'yi temizle
            context.Response.Cookies.Delete(CartCookieName);
        }

        /// <summary>
        /// Session ID'yi döndürür (varsa)
        /// </summary>
        public static string? GetSessionId(HttpContext context)
        {
            return context.Request.Cookies[CartCookieName];
        }
    }
}