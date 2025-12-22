// Infrastructure/Otomar.Persistance/Services/CartService.cs
using Microsoft.Extensions.Logging;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Cart;
using Otomar.Persistance.Options;
using StackExchange.Redis;
using System.Net;
using System.Text.Json;

namespace Otomar.Persistance.Services
{
    public class CartService(
        IConnectionMultiplexer redis,
        IProductService productService,
        ShippingOptions shippingOptions,
        ILogger<CartService> logger,
        RedisOptions redisOptions) : ICartService
    {
        private readonly IDatabase _redis = redis.GetDatabase();
        private readonly TimeSpan _cartExpiration = TimeSpan.FromDays(redisOptions.CartExpirationDays);


        public async Task<ServiceResult<CartDto>> AddToCartAsync(string cartKey, AddToCartDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto.Quantity <= 0)
                {
                    return ServiceResult<CartDto>.Error("Miktar 0'dan büyük olmalıdır", HttpStatusCode.BadRequest);
                }

                // Ürün bilgilerini getir
                var productResult = await productService.GetProductByIdAsync(dto.ProductId);
                if (!productResult.IsSuccess || productResult.Data == null)
                {
                    logger.LogWarning($"Ürün bulunamadı: {dto.ProductId}");
                    return ServiceResult<CartDto>.Error("Ürün bulunamadı", HttpStatusCode.NotFound);
                }

                var product = productResult.Data;

                // Stok kontrolü
                if (product.STOK_BAKIYE.HasValue && product.STOK_BAKIYE < dto.Quantity)
                {
                    return ServiceResult<CartDto>.Error($"Yeterli stok yok. Mevcut: {product.STOK_BAKIYE}", HttpStatusCode.BadRequest);
                }

                // Mevcut sepeti getir
                var cart = await GetCartInternalAsync(cartKey);

                // Ürün sepette var mı kontrol et
                var existingItem = cart.Items.FirstOrDefault(x => x.ProductId == dto.ProductId);

                if (existingItem != null)
                {
                    // Varsa miktarı artır
                    var newQuantity = existingItem.Quantity + dto.Quantity;

                    // Stok kontrolü tekrar
                    if (product.STOK_BAKIYE.HasValue && product.STOK_BAKIYE < newQuantity)
                    {
                        return ServiceResult<CartDto>.Error($"Yeterli stok yok. Mevcut: {product.STOK_BAKIYE}", HttpStatusCode.BadRequest);
                    }

                    existingItem.Quantity = newQuantity;
                }
                else
                {
                    // Yoksa yeni ekle
                    cart.Items.Add(new CartItemDto
                    {
                        ProductId = product.ID,
                        ProductCode = product.STOK_KODU,
                        ProductName = product.STOK_ADI,
                        UnitPrice = product.SATIS_FIYAT,
                        Quantity = dto.Quantity,
                        ImagePath = product.DOSYA_KONUM,
                        ManufacturerLogo = product.URETICI_MARKA_LOGO,
                        StockQuantity = product.STOK_BAKIYE
                    });
                }

                // Kargo ücreti hesapla
                cart.ShippingCost = cart.SubTotal >= shippingOptions.Threshold ? 0 : shippingOptions.Cost;

                // Redis'e kaydet
                await SaveCartAsync(cartKey, cart);

                logger.LogInformation($"Sepete ürün eklendi. CartKey: {cartKey}, ProductId: {dto.ProductId}, Quantity: {dto.Quantity}");

                return ServiceResult<CartDto>.SuccessAsOk(cart);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AddToCartAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<CartDto>> UpdateCartItemAsync(string cartKey, UpdateCartItemDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto.Quantity < 0)
                {
                    return ServiceResult<CartDto>.Error("Miktar 0'dan küçük olamaz", HttpStatusCode.BadRequest);
                }

                var cart = await GetCartInternalAsync(cartKey);

                var item = cart.Items.FirstOrDefault(x => x.ProductId == dto.ProductId);
                if (item == null)
                {
                    return ServiceResult<CartDto>.Error("Ürün sepette bulunamadı", HttpStatusCode.NotFound);
                }

                if (dto.Quantity == 0)
                {
                    // Miktar 0 ise sil
                    cart.Items.Remove(item);
                }
                else
                {
                    // Stok kontrolü
                    if (item.StockQuantity.HasValue && item.StockQuantity < dto.Quantity)
                    {
                        return ServiceResult<CartDto>.Error($"Yeterli stok yok. Mevcut: {item.StockQuantity}", HttpStatusCode.BadRequest);
                    }

                    item.Quantity = dto.Quantity;
                }

                // Kargo ücreti hesapla
                cart.ShippingCost = cart.SubTotal >= shippingOptions.Threshold ? 0 : shippingOptions.Cost;

                await SaveCartAsync(cartKey, cart);

                logger.LogInformation($"Sepet güncellendi. CartKey: {cartKey}, ProductId: {dto.ProductId}, Quantity: {dto.Quantity}");

                return ServiceResult<CartDto>.SuccessAsOk(cart);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "UpdateCartItemAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<CartDto>> RemoveFromCartAsync(string cartKey, int productId, CancellationToken cancellationToken = default)
        {
            try
            {
                var cart = await GetCartInternalAsync(cartKey);

                var item = cart.Items.FirstOrDefault(x => x.ProductId == productId);
                if (item == null)
                {
                    return ServiceResult<CartDto>.Error("Ürün sepette bulunamadı", HttpStatusCode.NotFound);
                }

                cart.Items.Remove(item);

                // Kargo ücreti hesapla
                cart.ShippingCost = cart.SubTotal >= shippingOptions.Threshold ? 0 : shippingOptions.Cost;

                await SaveCartAsync(cartKey, cart);

                logger.LogInformation($"Ürün sepetten silindi. CartKey: {cartKey}, ProductId: {productId}");

                return ServiceResult<CartDto>.SuccessAsOk(cart);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RemoveFromCartAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<CartDto>> GetCartAsync(string cartKey, CancellationToken cancellationToken = default)
        {
            try
            {
                var cart = await GetCartInternalAsync(cartKey);

                // Kargo ücreti hesapla
                cart.ShippingCost = cart.SubTotal >= shippingOptions.Threshold ? 0 : shippingOptions.Cost;

                return ServiceResult<CartDto>.SuccessAsOk(cart);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetCartAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult> ClearCartAsync(string cartKey, CancellationToken cancellationToken = default)
        {
            try
            {
                await _redis.KeyDeleteAsync(cartKey);

                logger.LogInformation($"Sepet temizlendi. CartKey: {cartKey}");

                return ServiceResult.SuccessAsNoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ClearCartAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult> RefreshCartAsync(string cartKey, CancellationToken cancellationToken = default)
        {
            try
            {
                var exists = await _redis.KeyExistsAsync(cartKey);
                if (exists)
                {
                    await _redis.KeyExpireAsync(cartKey, _cartExpiration);
                    logger.LogInformation($"Sepet TTL'i yenilendi. CartKey: {cartKey}");
                }

                return ServiceResult.SuccessAsNoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RefreshCartAsync işleminde hata");
                throw;
            }
        }

        // Private helper methods
        private async Task<CartDto> GetCartInternalAsync(string cartKey)
        {
            var cartJson = await _redis.StringGetAsync(cartKey);

            if (cartJson.IsNullOrEmpty)
            {
                return new CartDto();
            }

            var cart = JsonSerializer.Deserialize<CartDto>(cartJson.ToString());
            return cart ?? new CartDto();
        }

        private async Task SaveCartAsync(string cartKey, CartDto cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            await _redis.StringSetAsync(cartKey, cartJson, _cartExpiration);
        }
    }
}