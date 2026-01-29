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

            Console.WriteLine($"========== CART DEBUG ==========");
            Console.WriteLine($"Incoming SessionId: {sessionId ?? "NULL"}");
            Console.WriteLine($"Response.HasStarted: {context.Response.HasStarted}");
            Console.WriteLine($"ExpirationDays: {redisOptions.CartExpirationDays}");

            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString("N");
                Console.WriteLine($"Created new: {sessionId}");
            }

            RefreshCookie(context, sessionId, redisOptions.CartExpirationDays);
            return sessionId;
        }

        private static void RefreshCookie(HttpContext context, string sessionId, int expirationDays)
        {
            if (context.Response.HasStarted)
            {
                Console.WriteLine("[CartHelper] ⚠️ Response already started, cookie NOT written!");
                return;
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps, // HTTP'de (localhost) cookie kaydedilsin; yoksa tarayıcı kaydetmez, her istekte yeni CartSessionId = boş sepet
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

            var sessionId = context.Request.Cookies[CartCookieName];
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
        /// Session ID'yi döndürür (varsa)
        /// </summary>
        public static string? GetSessionId(HttpContext context)
        {
            return context.Request.Cookies[CartCookieName];
        }
    }
}