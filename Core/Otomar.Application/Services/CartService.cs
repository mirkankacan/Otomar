using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Otomar.Shared.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Shared.Dtos.Cart;
using Otomar.Application.Helpers;
using Otomar.Application.Options;
using Otomar.Application.Contracts.Persistence;
using System.Net;
using System.Text.Json;

namespace Otomar.Application.Services
{
    public class CartService(
        IDistributedCache cache,
        IProductService productService,
        IListSearchService listSearchService,
        ShippingOptions shippingOptions,
        RedisOptions redisOptions,
        ILogger<CartService> logger,
        IIdentityService identityService,
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

            // Override fiyat belirle
            var hasOverride = dto.OverridePrice.HasValue && dto.ListSearchAnswerId.HasValue;
            var unitPrice = hasOverride
                ? Math.Round(dto.OverridePrice!.Value, 2, MidpointRounding.AwayFromZero)
                : Math.Round(product.SATIS_FIYAT, 2, MidpointRounding.AwayFromZero);

            // Ürün sepette var mı kontrol et
            var existingItem = cart.Items.FirstOrDefault(x => x.ProductId == dto.ProductId);

            if (existingItem != null)
            {
                // Override fiyatlı ürün zaten sepetteyse, farklı fiyat kaynağı ile eklenmesini engelle
                if (existingItem.ListSearchAnswerId.HasValue != hasOverride)
                {
                    return ServiceResult<CartDto>.Error(
                        "Fiyat Çakışması",
                        "Bu ürün sepette farklı bir fiyat kaynağı ile zaten mevcut. Lütfen önce mevcut ürünü sepetten çıkarın.",
                        HttpStatusCode.Conflict);
                }

                // Teklif fiyatlı ürün zaten sepetteyse miktar artırılamaz
                if (existingItem.ListSearchAnswerId.HasValue)
                {
                    return ServiceResult<CartDto>.SuccessAsOk(cart);
                }

                // Override olmayan ürünler için fiyat değişikliği kontrolü
                if (existingItem.UnitPrice != product.SATIS_FIYAT)
                {
                    logger.LogWarning(
                        "Sepete ekleme sırasında fiyat değişikliği tespit edildi. Ürün: {ProductId}, Eski: {OldPrice:C2}, Yeni: {NewPrice:C2}",
                        dto.ProductId, existingItem.UnitPrice, product.SATIS_FIYAT);

                    existingItem.UnitPrice = Math.Round(product.SATIS_FIYAT, 2, MidpointRounding.AwayFromZero);
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
                    UnitPrice = unitPrice,
                    Quantity = dto.Quantity,
                    ImagePath = product.DOSYA_KONUM?.Split(';', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(),
                    ManufacturerLogo = product.URETICI_MARKA_LOGO,
                    StockQuantity = product.STOK_BAKIYE,
                    OverridePrice = hasOverride ? dto.OverridePrice : null,
                    ListSearchAnswerId = hasOverride ? dto.ListSearchAnswerId : null
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

            // Teklif fiyatlı ürünlerin miktarı değiştirilemez
            if (item.ListSearchAnswerId.HasValue && dto.Quantity != 0)
            {
                return ServiceResult<CartDto>.Error(
                    "Miktar Değiştirilemez",
                    "Teklif fiyatlı ürünlerin miktarı değiştirilemez. Sadece sepetten çıkarabilirsiniz.",
                    HttpStatusCode.BadRequest);
            }

            // Güncel ürün bilgisini al
            var productResult = await productService.GetProductByIdAsync(dto.ProductId);
            if (productResult.IsSuccess && productResult.Data != null)
            {
                var product = productResult.Data;

                // Override fiyatlı ürünlerin fiyatını güncelleme (teklif fiyatı korunur)
                if (!item.ListSearchAnswerId.HasValue && item.UnitPrice != product.SATIS_FIYAT)
                {
                    logger.LogWarning(
                        "Sepet güncelleme sırasında fiyat değişikliği tespit edildi. Ürün: {ProductId}, Eski: {OldPrice:C2}, Yeni: {NewPrice:C2}",
                        dto.ProductId, item.UnitPrice, product.SATIS_FIYAT);

                    // Fiyatı otomatik güncelle
                    item.UnitPrice = Math.Round(product.SATIS_FIYAT, 2, MidpointRounding.AwayFromZero);
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
            CancellationToken cancellationToken = default, IUnitOfWork? unitOfWork = null)
        {
            // Login sonrası session sepetini user sepetine otomatik birleştir
            await TryMergeSessionCartAsync(cancellationToken);

            var cartKey = GetCartKey();

            var cart = await GetCartInternalAsync(cartKey, cancellationToken);

            if (cart.Items.Any())
            {
                var priceUpdated = false;
                var itemsToRemove = new List<CartItemDto>();

                // Her ürün için güncel fiyat ve stok kontrolü
                foreach (var item in cart.Items)
                {
                    var productResult = await productService.GetProductByIdAsync(item.ProductId, unitOfWork);

                    if (productResult.IsSuccess && productResult.Data != null)
                    {
                        var product = productResult.Data;

                        // Override fiyatlı ürünlerin fiyatını güncelleme (teklif fiyatı korunur)
                        if (!item.ListSearchAnswerId.HasValue && item.UnitPrice != product.SATIS_FIYAT)
                        {
                            logger.LogInformation(
                                "Sepet görüntüleme sırasında fiyat değişikliği tespit edildi. Ürün: {ProductId}, Eski: {OldPrice:C2}, Yeni: {NewPrice:C2}",
                                item.ProductId, item.UnitPrice, product.SATIS_FIYAT);

                            item.UnitPrice = Math.Round(product.SATIS_FIYAT, 2, MidpointRounding.AwayFromZero);
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

        public async Task<ServiceResult> ClearCartBySessionIdAsync(
            string cartSessionId, CancellationToken cancellationToken = default)
        {
            await cache.RemoveAsync(cartSessionId, cancellationToken);

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

        private async Task TryMergeSessionCartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var context = httpContextAccessor.HttpContext;
                if (context == null) return;

                // Kullanıcı authenticated değilse merge gerekmez
                if (context.User?.Identity?.IsAuthenticated != true) return;

                // Session ID yoksa merge edilecek sepet yok
                var sessionId = CartHelper.GetSessionId(context);
                if (string.IsNullOrEmpty(sessionId)) return;

                // Session sepetinde veri var mı kontrol et
                var sessionCartKey = $"{redisOptions.InstanceName}cart:session:{sessionId}";
                var sessionCartJson = await cache.GetStringAsync(sessionCartKey, cancellationToken);
                if (string.IsNullOrEmpty(sessionCartJson)) return;

                await CartHelper.MergeCartsOnLoginAsync(context, cache, redisOptions, shippingOptions);
                logger.LogInformation("Session sepeti user sepetine birleştirildi. SessionId: {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Session sepet birleştirme sırasında hata oluştu");
            }
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

        public string GetCurrentCartKey()
        {
            return CartHelper.GetCartKey(httpContextAccessor.HttpContext!, redisOptions);
        }

        public async Task<ServiceResult<CartDto>> AddToCartFromListSearchAsync(
            string requestNo,
            CancellationToken cancellationToken = default)
        {
            // Liste sorguyu getir
            var listSearchResult = await listSearchService.GetListSearchByRequestNoAsync(requestNo);
            if (!listSearchResult.IsSuccess || listSearchResult.Data == null)
            {
                return ServiceResult<CartDto>.Error(
                    "Liste Sorgu Bulunamadı",
                    $"Talep numarası '{requestNo}' olan liste sorgu bulunamadı",
                    HttpStatusCode.NotFound);
            }

            var listSearch = listSearchResult.Data;

            // Cevaplanmış mı kontrol et
            if (listSearch.Status != Otomar.Shared.Enums.ListSearchStatus.Answered)
            {
                return ServiceResult<CartDto>.Error(
                    "Cevaplanmamış Sorgu",
                    "Bu liste sorgu henüz cevaplanmamış",
                    HttpStatusCode.BadRequest);
            }

            // Cevaplanan parçaları filtrele
            var answeredParts = listSearch.Parts
                .Where(p => p.Answer != null && !string.IsNullOrWhiteSpace(p.Answer.StockCode))
                .ToList();

            if (!answeredParts.Any())
            {
                return ServiceResult<CartDto>.Error(
                    "Cevap Bulunamadı",
                    "Bu liste sorguda cevaplanmış parça bulunamadı",
                    HttpStatusCode.BadRequest);
            }

            // Her cevaplanan parçayı sepete ekle
            var errors = new List<string>();
            ServiceResult<CartDto>? lastResult = null;

            foreach (var part in answeredParts)
            {
                var answer = part.Answer!;

                // Stok kodu ile ürünü bul
                var productResult = await productService.GetProductByCodeAsync(answer.StockCode!);
                if (!productResult.IsSuccess || productResult.Data == null)
                {
                    errors.Add($"'{answer.StockCode}' stok kodlu ürün bulunamadı");
                    continue;
                }

                var addDto = new AddToCartDto
                {
                    ProductId = productResult.Data.ID,
                    Quantity = answer.Quantity,
                    OverridePrice = answer.UnitPrice,
                    ListSearchAnswerId = answer.Id
                };

                lastResult = await AddToCartAsync(addDto, cancellationToken);

                if (!lastResult.IsSuccess)
                {
                    errors.Add($"'{answer.StockCode}': {lastResult.Fail?.Detail}");
                }
            }

            if (errors.Any() && lastResult == null)
            {
                return ServiceResult<CartDto>.Error(
                    "Sepet Oluşturulamadı",
                    string.Join("; ", errors),
                    HttpStatusCode.BadRequest);
            }

            // Son başarılı sepet durumunu döndür
            var cartResult = await GetCartAsync(cancellationToken);

            if (errors.Any())
            {
                logger.LogWarning(
                    "Liste sorgu sipariş oluşturulurken bazı ürünler eklenemedi. RequestNo: {RequestNo}, Hatalar: {Errors}",
                    requestNo, string.Join("; ", errors));
            }

            return cartResult;
        }

        private decimal CalculateShippingCost(decimal subTotal, int itemCount)
        {
            if (itemCount == 0)
                return 0;

            if (httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true && identityService.IsUserPaymentExempt() == true)
                return 0;

            return subTotal >= shippingOptions.Threshold ? 0 : shippingOptions.Cost;
        }
    }
}