using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Cart;
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
            group.MapGet("/", async ([FromServices] ICartService cartService, CancellationToken cancellationToken) =>
            {
                var result = await cartService.GetCartAsync(cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("GetCart");

            // Sepete ürün ekle
            group.MapPost("/items", async ([FromBody] AddToCartDto dto, [FromServices] ICartService cartService, CancellationToken cancellationToken) =>
            {
                var result = await cartService.AddToCartAsync(dto, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("AddToCart");

            // Sepetteki ürün miktarını güncelle
            group.MapPut("/items", async ([FromBody] UpdateCartItemDto dto, [FromServices] ICartService cartService, CancellationToken cancellationToken) =>
            {
                var result = await cartService.UpdateCartItemAsync(dto, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("UpdateCartItem");

            // Sepetten ürün sil
            group.MapDelete("/items/{productId:int}", async (HttpContext context, int productId, [FromServices] ICartService cartService, CancellationToken cancellationToken) =>
            {
                var result = await cartService.RemoveFromCartAsync(productId, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("RemoveFromCart");

            // Sepeti temizle
            group.MapDelete("/", async ([FromServices] ICartService cartService, CancellationToken cancellationToken) =>
            {
                var result = await cartService.ClearCartAsync(cancellationToken);
                return result.ToResult();
            })
            .WithName("ClearCart");

            // Sepet TTL'ini yenile
            group.MapPost("/refresh", async ([FromServices] ICartService cartService, CancellationToken cancellationToken) =>
            {
                var result = await cartService.RefreshCartAsync(cancellationToken);
                return result.ToResult();
            })
            .WithName("RefreshCart");
        }
    }
}