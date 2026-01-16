using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Cart;
using Otomar.Persistance.Options;
using System.Net;
using System.Text.Json;

namespace Otomar.Persistance.Services
{
    public class CartService : ICartService
    {
        private readonly IDistributedCache _cache;
        private readonly IProductService _productService;
        private readonly ShippingOptions _shippingOptions;
        private readonly ILogger<CartService> _logger;
        private readonly TimeSpan _cartExpiration;

        public CartService(
            IDistributedCache cache,
            IProductService productService,
            IOptions<ShippingOptions> shippingOptions,
            IOptions<RedisOptions> redisOptions,
            ILogger<CartService> logger)
        {
            _cache = cache;
            _productService = productService;
            _shippingOptions = shippingOptions.Value;
            _logger = logger;
            _cartExpiration = TimeSpan.FromDays(redisOptions.Value.CartExpirationDays);
        }

        public async Task<ServiceResult<CartDto>> AddToCartAsync(
            string cartKey,
            AddToCartDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto.Quantity <= 0)
                {
                    return ServiceResult<CartDto>.Error(
                        "Miktar 0'dan büyük olmalıdır",
                        HttpStatusCode.BadRequest);
                }

                // Ürün bilgilerini getir
                var productResult = await _productService.GetProductByIdAsync(dto.ProductId);
                if (!productResult.IsSuccess || productResult.Data == null)
                {
                    _logger.LogWarning("Ürün bulunamadı: {ProductId}", dto.ProductId);
                    return ServiceResult<CartDto>.Error(
                        "Ürün bulunamadı",
                        HttpStatusCode.NotFound);
                }

                var product = productResult.Data;

                // Stok kontrolü
                if (product.STOK_BAKIYE.HasValue && product.STOK_BAKIYE < dto.Quantity)
                {
                    return ServiceResult<CartDto>.Error(
                        $"Yeterli stok yok. Mevcut: {product.STOK_BAKIYE}",
                        HttpStatusCode.BadRequest);
                }

                // Mevcut sepeti getir
                var cart = await GetCartInternalAsync(cartKey, cancellationToken);

                // Ürün sepette var mı kontrol et
                var existingItem = cart.Items.FirstOrDefault(x => x.ProductId == dto.ProductId);

                if (existingItem != null)
                {
                    // Varsa miktarı artır
                    var newQuantity = existingItem.Quantity + dto.Quantity;

                    // Stok kontrolü tekrar
                    if (product.STOK_BAKIYE.HasValue && product.STOK_BAKIYE < newQuantity)
                    {
                        return ServiceResult<CartDto>.Error(
                            $"Yeterli stok yok. Mevcut: {product.STOK_BAKIYE}",
                            HttpStatusCode.BadRequest);
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
                cart.ShippingCost = CalculateShippingCost(cart.SubTotal);

                // Redis'e kaydet
                await SaveCartAsync(cartKey, cart, cancellationToken);

                _logger.LogInformation(
                    "Sepete ürün eklendi. CartKey: {CartKey}, ProductId: {ProductId}, Quantity: {Quantity}",
                    cartKey, dto.ProductId, dto.Quantity);

                return ServiceResult<CartDto>.SuccessAsOk(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddToCartAsync işleminde hata. CartKey: {CartKey}", cartKey);
                throw;
            }
        }

        public async Task<ServiceResult<CartDto>> UpdateCartItemAsync(
            string cartKey,
            UpdateCartItemDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto.Quantity < 0)
                {
                    return ServiceResult<CartDto>.Error(
                        "Miktar 0'dan küçük olamaz",
                        HttpStatusCode.BadRequest);
                }

                var cart = await GetCartInternalAsync(cartKey, cancellationToken);

                var item = cart.Items.FirstOrDefault(x => x.ProductId == dto.ProductId);
                if (item == null)
                {
                    return ServiceResult<CartDto>.Error(
                        "Ürün sepette bulunamadı",
                        HttpStatusCode.NotFound);
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
                        return ServiceResult<CartDto>.Error(
                            $"Yeterli stok yok. Mevcut: {item.StockQuantity}",
                            HttpStatusCode.BadRequest);
                    }

                    item.Quantity = dto.Quantity;
                }

                // Kargo ücreti hesapla
                cart.ShippingCost = CalculateShippingCost(cart.SubTotal);

                await SaveCartAsync(cartKey, cart, cancellationToken);

                _logger.LogInformation(
                    "Sepet güncellendi. CartKey: {CartKey}, ProductId: {ProductId}, Quantity: {Quantity}",
                    cartKey, dto.ProductId, dto.Quantity);

                return ServiceResult<CartDto>.SuccessAsOk(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateCartItemAsync işleminde hata. CartKey: {CartKey}", cartKey);
                throw;
            }
        }

        public async Task<ServiceResult<CartDto>> RemoveFromCartAsync(
            string cartKey,
            int productId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cart = await GetCartInternalAsync(cartKey, cancellationToken);

                var item = cart.Items.FirstOrDefault(x => x.ProductId == productId);
                if (item == null)
                {
                    return ServiceResult<CartDto>.Error(
                        "Ürün sepette bulunamadı",
                        HttpStatusCode.NotFound);
                }

                cart.Items.Remove(item);

                // Kargo ücreti hesapla
                cart.ShippingCost = CalculateShippingCost(cart.SubTotal);

                await SaveCartAsync(cartKey, cart, cancellationToken);

                _logger.LogInformation(
                    "Ürün sepetten silindi. CartKey: {CartKey}, ProductId: {ProductId}",
                    cartKey, productId);

                return ServiceResult<CartDto>.SuccessAsOk(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveFromCartAsync işleminde hata. CartKey: {CartKey}", cartKey);
                throw;
            }
        }

        public async Task<ServiceResult<CartDto>> GetCartAsync(
            string cartKey,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cart = await GetCartInternalAsync(cartKey, cancellationToken);

                // Kargo ücreti hesapla
                cart.ShippingCost = CalculateShippingCost(cart.SubTotal);

                return ServiceResult<CartDto>.SuccessAsOk(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCartAsync işleminde hata. CartKey: {CartKey}", cartKey);
                throw;
            }
        }

        public async Task<ServiceResult> ClearCartAsync(
            string cartKey,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _cache.RemoveAsync(cartKey, cancellationToken);

                _logger.LogInformation("Sepet temizlendi. CartKey: {CartKey}", cartKey);

                return ServiceResult.SuccessAsNoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClearCartAsync işleminde hata. CartKey: {CartKey}", cartKey);
                throw;
            }
        }

        public async Task<ServiceResult> RefreshCartAsync(
            string cartKey,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Cache'den sepeti al
                var cartJson = await _cache.GetStringAsync(cartKey, cancellationToken);

                if (string.IsNullOrEmpty(cartJson))
                {
                    _logger.LogWarning("Yenilenecek sepet bulunamadı. CartKey: {CartKey}", cartKey);
                    return ServiceResult.SuccessAsNoContent();
                }

                // Tekrar kaydet (TTL yenilenir)
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cartExpiration
                };

                await _cache.SetStringAsync(cartKey, cartJson, cacheOptions, cancellationToken);

                _logger.LogInformation("Sepet TTL'i yenilendi. CartKey: {CartKey}", cartKey);

                return ServiceResult.SuccessAsNoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RefreshCartAsync işleminde hata. CartKey: {CartKey}", cartKey);
                throw;
            }
        }

        // Private helper methods
        private async Task<CartDto> GetCartInternalAsync(
            string cartKey,
            CancellationToken cancellationToken = default)
        {
            var cartJson = await _cache.GetStringAsync(cartKey, cancellationToken);

            if (string.IsNullOrEmpty(cartJson))
            {
                return new CartDto();
            }

            var cart = JsonSerializer.Deserialize<CartDto>(cartJson);
            return cart ?? new CartDto();
        }

        private async Task SaveCartAsync(
            string cartKey,
            CartDto cart,
            CancellationToken cancellationToken = default)
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cartExpiration
            };

            var cartJson = JsonSerializer.Serialize(cart);
            await _cache.SetStringAsync(cartKey, cartJson, cacheOptions, cancellationToken);
        }

        private decimal CalculateShippingCost(decimal subTotal)
        {
            return subTotal >= _shippingOptions.Threshold ? 0 : _shippingOptions.Cost;
        }
    }
}