// Api/Otomar.WebApi/Endpoints/CartEndpoints.cs
using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Cart;
using Otomar.Persistance.Options;
using Otomar.WebApi.Extensions;

namespace Otomar.WebApi.Endpoints
{
    public class CartEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/cart")
                .WithTags("Cart");

            // Sepeti getir
            group.MapGet("/", async (HttpContext context, [FromServices] ICartService cartService, [FromServices] RedisOptions redisOptions, CancellationToken cancellationToken) =>
            {
                var cartKey = GetCartKey(context, redisOptions);
                var result = await cartService.GetCartAsync(cartKey, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("GetCart");

            // Sepete ürün ekle
            group.MapPost("/items", async (HttpContext context, [FromBody] AddToCartDto dto, [FromServices] ICartService cartService, [FromServices] RedisOptions redisOptions, CancellationToken cancellationToken) =>
            {
                var cartKey = GetCartKey(context, redisOptions);
                var result = await cartService.AddToCartAsync(cartKey, dto, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("AddToCart");

            // Sepetteki ürün miktarını güncelle
            group.MapPut("/items", async (HttpContext context, [FromBody] UpdateCartItemDto dto, [FromServices] ICartService cartService, [FromServices] RedisOptions redisOptions, CancellationToken cancellationToken) =>
            {
                var cartKey = GetCartKey(context, redisOptions);
                var result = await cartService.UpdateCartItemAsync(cartKey, dto, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("UpdateCartItem");

            // Sepetten ürün sil
            group.MapDelete("/items/{productId:int}", async (HttpContext context, int productId, [FromServices] ICartService cartService, [FromServices] RedisOptions redisOptions, CancellationToken cancellationToken) =>
            {
                var cartKey = GetCartKey(context, redisOptions);
                var result = await cartService.RemoveFromCartAsync(cartKey, productId, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("RemoveFromCart");

            // Sepeti temizle
            group.MapDelete("/", async (HttpContext context, [FromServices] ICartService cartService, [FromServices] RedisOptions redisOptions, CancellationToken cancellationToken) =>
            {
                var cartKey = GetCartKey(context, redisOptions);
                var result = await cartService.ClearCartAsync(cartKey, cancellationToken);
                return result.ToResult();
            })
            .WithName("ClearCart");

            // Sepet TTL'ini yenile
            group.MapPost("/refresh", async (HttpContext context, [FromServices] ICartService cartService, [FromServices] RedisOptions redisOptions, CancellationToken cancellationToken) =>
            {
                var cartKey = GetCartKey(context, redisOptions);
                var result = await cartService.RefreshCartAsync(cartKey, cancellationToken);
                return result.ToResult();
            })
            .WithName("RefreshCart");
        }

        private static string GetCartKey(HttpContext context, RedisOptions redisOptions)
        {
            // Kullanıcı giriş yapmışsa UserId kullan
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"cart:user:{userId}";
            }

            // Giriş yapmamışsa SessionId kullan (cookie'den)
            var sessionId = context.Request.Cookies["CartSessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                // Yeni session oluştur
                sessionId = Guid.NewGuid().ToString();
                context.Response.Cookies.Append("CartSessionId", sessionId, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(redisOptions.CartExpirationDays)
                });
            }

            return $"cart:session:{sessionId}";
        }
    }
}