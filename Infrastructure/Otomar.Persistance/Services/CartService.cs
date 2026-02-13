using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Cart;
using Otomar.Persistance.Helpers;
using Otomar.Persistance.Options;
using System.Net;
using System.Text.Json;

namespace Otomar.Persistance.Services
{
    public class CartService(
        IDistributedCache cache,
        IProductService productService,
        ShippingOptions shippingOptions,
        RedisOptions redisOptions,
        ILogger<CartService> logger,
        IHttpContextAccessor httpContextAccessor) : ICartService
    {
        private readonly TimeSpan _cartExpiration = TimeSpan.FromDays(redisOptions.CartExpirationDays);

        // CartKey'i otomatik al
        private string GetCartKey()
        {
            return CartHelper.GetCartKey(httpContextAccessor.HttpContext!, redisOptions);
        }

        public async Task<ServiceResult<CartDto>> AddToCartAsync(
            AddToCartDto dto,
            CancellationToken cancellationToken = default)
        {
            var cartKey = GetCartKey();

            if (dto.Quantity <= 0)
            {
                return ServiceResult<CartDto>.Error(
                    "Geçersiz Miktar",
                    "Miktar 0'dan büyük olmalıdır",
                    HttpStatusCode.BadRequest);
            }

            // Ürün bilgilerini getir
            var productResult = await productService.GetProductByIdAsync(dto.ProductId);
            if (!productResult.IsSuccess || productResult.Data == null)
            {
                logger.LogWarning("Ürün bulunamadı: {ProductId}", dto.ProductId);
                return ServiceResult<CartDto>.Error(
                    "Ürün Bulunamadı",
                    $"ID'si {dto.ProductId} olan ürün bulunamadı",
                    HttpStatusCode.NotFound);
            }

            var product = productResult.Data;

            // Stok kontrolü
            if (product.STOK_BAKIYE.HasValue && product.STOK_BAKIYE < dto.Quantity)
            {
                return ServiceResult<CartDto>.Error(
                    "Yetersiz Stok",
                    "Yeterli stok yok.",
                    HttpStatusCode.BadRequest);
            }

            // Mevcut sepeti getir
            var cart = await GetCartInternalAsync(cartKey, cancellationToken);

            // Ürün sepette var mı kontrol et
            var existingItem = cart.Items.FirstOrDefault(x => x.ProductId == dto.ProductId);

            if (existingItem != null)
            {
                // Fiyat değişikliği kontrolü
                if (existingItem.UnitPrice != product.SATIS_FIYAT)
                {
                    logger.LogWarning(
                        "Sepete ekleme sırasında fiyat değişikliği tespit edildi. Ürün: {ProductId}, Eski: {OldPrice:C2}, Yeni: {NewPrice:C2}",
                        dto.ProductId, existingItem.UnitPrice, product.SATIS_FIYAT);

                    // Fiyatı otomatik güncelle
                    existingItem.UnitPrice = product.SATIS_FIYAT;
                }

                // Varsa miktarı artır
                var newQuantity = existingItem.Quantity + dto.Quantity;

                // Stok kontrolü tekrar
                if (product.STOK_BAKIYE.HasValue && product.STOK_BAKIYE < newQuantity)
                {
                    return ServiceResult<CartDto>.Error(
                        "Yetersiz Stok",
                        "Yeterli stok yok.",
                        HttpStatusCode.BadRequest);
                }

                existingItem.Quantity = newQuantity;
                // Stok bilgisini güncelle
                existingItem.StockQuantity = product.STOK_BAKIYE;
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

            // Kargo ücreti hesapla (sepet boşken 0)
            cart.ShippingCost = CalculateShippingCost(cart.SubTotal, cart.Items.Count);

            // Redis'e kaydet
            await SaveCartAsync(cartKey, cart, cancellationToken);

            return ServiceResult<CartDto>.SuccessAsOk(cart);
        }

        public async Task<ServiceResult<CartDto>> UpdateCartItemAsync(
            UpdateCartItemDto dto,
            CancellationToken cancellationToken = default)
        {
            var cartKey = GetCartKey();

            if (dto.Quantity < 0)
            {
                return ServiceResult<CartDto>.Error(
                    "Geçersiz Miktar",
                    "Miktar 0'dan küçük olamaz",
                    HttpStatusCode.BadRequest);
            }

            var cart = await GetCartInternalAsync(cartKey, cancellationToken);

            var item = cart.Items.FirstOrDefault(x => x.ProductId == dto.ProductId);
            if (item == null)
            {
                return ServiceResult<CartDto>.Error(
                    "Ürün Sepette Bulunamadı",
                    $"ID'si {dto.ProductId} olan ürün sepette bulunamadı",
                    HttpStatusCode.NotFound);
            }

            // Güncel ürün bilgisini al
            var productResult = await productService.GetProductByIdAsync(dto.ProductId);
            if (productResult.IsSuccess && productResult.Data != null)
            {
                var product = productResult.Data;

                // Fiyat değişikliği kontrolü
                if (item.UnitPrice != product.SATIS_FIYAT)
                {
                    logger.LogWarning(
                        "Sepet güncelleme sırasında fiyat değişikliği tespit edildi. Ürün: {ProductId}, Eski: {OldPrice:C2}, Yeni: {NewPrice:C2}",
                        dto.ProductId, item.UnitPrice, product.SATIS_FIYAT);

                    // Fiyatı otomatik güncelle
                    item.UnitPrice = product.SATIS_FIYAT;
                }

                // Stok bilgisini güncelle
                item.StockQuantity = product.STOK_BAKIYE;

                if (dto.Quantity == 0)
                {
                    // Miktar 0 ise sil
                    cart.Items.Remove(item);
                }
                else
                {
                    // Güncel stok kontrolü
                    if (product.STOK_BAKIYE.HasValue && product.STOK_BAKIYE < dto.Quantity)
                    {
                        return ServiceResult<CartDto>.Error(
                            "Yetersiz Stok",
                            "Yeterli stok yok.",
                            HttpStatusCode.BadRequest);
                    }

                    item.Quantity = dto.Quantity;
                }
            }
            else
            {
                // Ürün artık mevcut değilse
                logger.LogWarning("Güncellenmeye çalışılan ürün bulunamadı: {ProductId}", dto.ProductId);
                cart.Items.Remove(item);
            }

            // Kargo ücreti hesapla (sepet boşken 0)
            cart.ShippingCost = CalculateShippingCost(cart.SubTotal, cart.Items.Count);

            await SaveCartAsync(cartKey, cart, cancellationToken);

            return ServiceResult<CartDto>.SuccessAsOk(cart);
        }

        public async Task<ServiceResult<CartDto>> RemoveFromCartAsync(
            int productId,
            CancellationToken cancellationToken = default)
        {
            var cartKey = GetCartKey();
            var cart = await GetCartInternalAsync(cartKey, cancellationToken);

            var item = cart.Items.FirstOrDefault(x => x.ProductId == productId);
            if (item == null)
            {
                return ServiceResult<CartDto>.Error(
                    "Ürün Sepette Bulunamadı",
                    $"ID'si {productId} olan ürün sepette bulunamadı",
                    HttpStatusCode.NotFound);
            }

            cart.Items.Remove(item);

            // Kargo ücreti hesapla (sepet boşken 0)
            cart.ShippingCost = CalculateShippingCost(cart.SubTotal, cart.Items.Count);

            await SaveCartAsync(cartKey, cart, cancellationToken);

            return ServiceResult<CartDto>.SuccessAsOk(cart);
        }

        public async Task<ServiceResult<CartDto>> GetCartAsync(
            CancellationToken cancellationToken = default)
        {
            var cartKey = GetCartKey();

            var cart = await GetCartInternalAsync(cartKey, cancellationToken);

            if (cart.Items.Any())
            {
                var priceUpdated = false;
                var itemsToRemove = new List<CartItemDto>();

                // Her ürün için güncel fiyat ve stok kontrolü
                foreach (var item in cart.Items)
                {
                    var productResult = await productService.GetProductByIdAsync(item.ProductId);

                    if (productResult.IsSuccess && productResult.Data != null)
                    {
                        var product = productResult.Data;

                        // Fiyat değişikliği kontrolü
                        if (item.UnitPrice != product.SATIS_FIYAT)
                        {
                            logger.LogInformation(
                                "Sepet görüntüleme sırasında fiyat değişikliği tespit edildi. Ürün: {ProductId}, Eski: {OldPrice:C2}, Yeni: {NewPrice:C2}",
                                item.ProductId, item.UnitPrice, product.SATIS_FIYAT);

                            item.UnitPrice = product.SATIS_FIYAT;
                            priceUpdated = true;
                        }

                        // Stok bilgisini güncelle
                        item.StockQuantity = product.STOK_BAKIYE;
                    }
                    else
                    {
                        // Ürün artık mevcut değilse sepetten kaldır
                        logger.LogWarning("Sepetteki ürün artık mevcut değil: {ProductId}", item.ProductId);
                        itemsToRemove.Add(item);
                        priceUpdated = true;
                    }
                }

                // Mevcut olmayan ürünleri kaldır
                foreach (var item in itemsToRemove)
                {
                    cart.Items.Remove(item);
                }

                // Değişiklik varsa kaydet
                if (priceUpdated)
                {
                    await SaveCartAsync(cartKey, cart, cancellationToken);
                }
            }

            // Kargo ücreti hesapla (sepet boşken 0)
            cart.ShippingCost = CalculateShippingCost(cart.SubTotal, cart.Items.Count);

            return ServiceResult<CartDto>.SuccessAsOk(cart);
        }

        public async Task<ServiceResult> ClearCartAsync(
            CancellationToken cancellationToken = default)
        {
            var cartKey = GetCartKey();
            await cache.RemoveAsync(cartKey, cancellationToken);

            return ServiceResult.SuccessAsNoContent();
        }

        public async Task<ServiceResult> RefreshCartAsync(
            CancellationToken cancellationToken = default)
        {
            var cartKey = GetCartKey();

            // Cache'den sepeti al
            var cartJson = await cache.GetStringAsync(cartKey, cancellationToken);

            if (string.IsNullOrEmpty(cartJson))
            {
                logger.LogWarning("Yenilenecek sepet bulunamadı. CartKey: {CartKey}", cartKey);
                return ServiceResult.SuccessAsNoContent();
            }

            // Tekrar kaydet (TTL yenilenir)
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cartExpiration
            };

            await cache.SetStringAsync(cartKey, cartJson, cacheOptions, cancellationToken);

            logger.LogInformation("Sepet TTL'i yenilendi. CartKey: {CartKey}", cartKey);

            return ServiceResult.SuccessAsNoContent();
        }

        // Private helper methods
        private async Task<CartDto> GetCartInternalAsync(
            string cartKey,
            CancellationToken cancellationToken = default)
        {
            var cartJson = await cache.GetStringAsync(cartKey, cancellationToken);

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
            await cache.SetStringAsync(cartKey, cartJson, cacheOptions, cancellationToken);
        }

        private decimal CalculateShippingCost(decimal subTotal, int itemCount)
        {
            if (itemCount == 0)
                return 0;
            return subTotal >= shippingOptions.Threshold ? 0 : shippingOptions.Cost;
        }
    }
}