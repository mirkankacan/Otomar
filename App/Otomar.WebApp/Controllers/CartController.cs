using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Otomar.Contracts.Dtos.Cart;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [AllowAnonymous]
    [Route("sepet")]
    public class CartController(ICartApi cartApi) : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("getir")]
        public async Task<IActionResult> GetCart(CancellationToken cancellationToken = default)
        {
            return await cartApi.GetCartAsync(cancellationToken).ToActionResultAsync();
        }

        [HttpPost("ekle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto, CancellationToken cancellationToken = default)
        {
            return await cartApi.AddToCartAsync(dto, cancellationToken).ToActionResultAsync();
        }

        [HttpPut("guncelle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemDto dto, CancellationToken cancellationToken = default)
        {
            return await cartApi.UpdateCartItemAsync(dto, cancellationToken).ToActionResultAsync();
        }

        [HttpDelete("sil/{productId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int productId, CancellationToken cancellationToken = default)
        {
            return await cartApi.RemoveFromCartAsync(productId, cancellationToken).ToActionResultAsync();
        }

        [HttpDelete("temizle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart(CancellationToken cancellationToken = default)
        {
            return await cartApi.ClearCartAsync(cancellationToken).ToActionResultAsync();
        }

        [HttpPost("yenile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshCart(CancellationToken cancellationToken = default)
        {
            return await cartApi.RefreshCartAsync(cancellationToken).ToActionResultAsync();
        }
    }
}