using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Otomar.Contracts.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Contracts.Dtos.Cart;
using Otomar.Contracts.Dtos.Product;
using Otomar.Persistance.Options;
using Otomar.Persistance.Services;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Otomar.UnitTests.Services;

/// <summary>
/// CartService testleri - Redis mock ile sepet işlemleri.
/// IDistributedCache, IProductService ve diğer bağımlılıkların mock'lanmasını gösterir.
/// </summary>
public class CartServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<IProductService> _productServiceMock;
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<CartService>> _loggerMock;
    private readonly ShippingOptions _shippingOptions;
    private readonly RedisOptions _redisOptions;
    private readonly CartService _sut;

    public CartServiceTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _productServiceMock = new Mock<IProductService>();
        _identityServiceMock = new Mock<IIdentityService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<CartService>>();

        _shippingOptions = new ShippingOptions
        {
            Threshold = 1000, // 1000 TL üzeri ücretsiz kargo
            Cost = 240.0m
        };

        _redisOptions = new RedisOptions
        {
            ConnectionString = "localhost",
            InstanceName = "otomar:",
            CartExpirationDays = 7
        };

        // Authenticated user ile HttpContext setup
        SetupAuthenticatedUser("test-user-id");

        _sut = new CartService(
            _cacheMock.Object,
            _productServiceMock.Object,
            _shippingOptions,
            _redisOptions,
            _loggerMock.Object,
            _identityServiceMock.Object,
            _httpContextAccessorMock.Object);
    }

    #region Helper Methods

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("IsPaymentExempt", "False")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _identityServiceMock.Setup(x => x.IsUserPaymentExempt()).Returns(false);
    }

    private void SetupEmptyCart()
    {
        // Redis'te boş sepet (null döner = yeni sepet)
        _cacheMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
    }

    private void SetupCartWithItems(CartDto cart)
    {
        var cartJson = JsonSerializer.Serialize(cart);
        var cartBytes = Encoding.UTF8.GetBytes(cartJson);
        _cacheMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartBytes);
    }

    private ProductDto CreateTestProduct(int id = 1, decimal price = 100m, decimal? stock = 10)
    {
        return new ProductDto
        {
            ID = id,
            STOK_KODU = $"STK-{id:D5}",
            STOK_ADI = $"Test Ürün {id}",
            SATIS_FIYAT = price,
            STOK_BAKIYE = stock,
            URETICI_MARKA_LOGO = "logo.png",
            DOSYA_KONUM = "img1.jpg;img2.jpg"
        };
    }

    #endregion Helper Methods

    #region AddToCartAsync Tests

    [Fact]
    public async Task AddToCartAsync_NewProduct_AddsToEmptyCart()
    {
        // Arrange
        var dto = new AddToCartDto { ProductId = 1, Quantity = 2 };
        var product = CreateTestProduct(id: 1, price: 150m, stock: 10);

        SetupEmptyCart();
        _productServiceMock
            .Setup(x => x.GetProductByIdAsync(1, null))
            .ReturnsAsync(ServiceResult<ProductDto?>.SuccessAsOk(product));

        // Act
        var result = await _sut.AddToCartAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].ProductId.Should().Be(1);
        result.Data.Items[0].Quantity.Should().Be(2);
        result.Data.Items[0].UnitPrice.Should().Be(150m);
        result.Data.SubTotal.Should().Be(300m); // 150 * 2

        // Redis'e kaydedildiğini doğrula
        _cacheMock.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddToCartAsync_ExistingProduct_IncreasesQuantity()
    {
        // Arrange
        var existingCart = new CartDto();
        existingCart.Items.Add(new CartItemDto
        {
            ProductId = 1,
            ProductCode = "STK-00001",
            ProductName = "Test Ürün 1",
            UnitPrice = 150m,
            Quantity = 1
        });

        SetupCartWithItems(existingCart);

        var product = CreateTestProduct(id: 1, price: 150m, stock: 10);
        _productServiceMock
            .Setup(x => x.GetProductByIdAsync(1, null))
            .ReturnsAsync(ServiceResult<ProductDto?>.SuccessAsOk(product));

        var dto = new AddToCartDto { ProductId = 1, Quantity = 3 };

        // Act
        var result = await _sut.AddToCartAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Quantity.Should().Be(4, "mevcut 1 + yeni 3 = 4");
    }

    [Fact]
    public async Task AddToCartAsync_ZeroQuantity_ReturnsBadRequest()
    {
        // Arrange
        var dto = new AddToCartDto { ProductId = 1, Quantity = 0 };

        // Act
        var result = await _sut.AddToCartAsync(dto);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Fail!.Title.Should().Be("Geçersiz Miktar");
    }

    [Fact]
    public async Task AddToCartAsync_ProductNotFound_ReturnsNotFound()
    {
        // Arrange
        var dto = new AddToCartDto { ProductId = 999, Quantity = 1 };

        _productServiceMock
            .Setup(x => x.GetProductByIdAsync(999, null))
            .ReturnsAsync(ServiceResult<ProductDto?>.Error("Not Found", HttpStatusCode.NotFound));

        // Act
        var result = await _sut.AddToCartAsync(dto);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddToCartAsync_InsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var dto = new AddToCartDto { ProductId = 1, Quantity = 100 };
        var product = CreateTestProduct(id: 1, price: 50m, stock: 5); // Stokta sadece 5 adet

        SetupEmptyCart();
        _productServiceMock
            .Setup(x => x.GetProductByIdAsync(1, null))
            .ReturnsAsync(ServiceResult<ProductDto?>.SuccessAsOk(product));

        // Act
        var result = await _sut.AddToCartAsync(dto);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Fail!.Title.Should().Be("Yetersiz Stok");
    }

    [Fact]
    public async Task AddToCartAsync_ShippingAboveThreshold_FreeShipping()
    {
        // Arrange - 1000 TL üzeri ücretsiz kargo
        var dto = new AddToCartDto { ProductId = 1, Quantity = 1 };
        var product = CreateTestProduct(id: 1, price: 1100m, stock: 10);

        SetupEmptyCart();
        _productServiceMock
            .Setup(x => x.GetProductByIdAsync(1, null))
            .ReturnsAsync(ServiceResult<ProductDto?>.SuccessAsOk(product));

        // Act
        var result = await _sut.AddToCartAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.ShippingCost.Should().Be(0, "1000 TL üzeri ücretsiz kargo");
        result.Data.Total.Should().Be(1100m);
    }

    [Fact]
    public async Task AddToCartAsync_ShippingBelowThreshold_ChargesShipping()
    {
        // Arrange - 1000 TL altı kargo ücreti var
        var dto = new AddToCartDto { ProductId = 1, Quantity = 1 };
        var product = CreateTestProduct(id: 1, price: 100m, stock: 10);

        SetupEmptyCart();
        _productServiceMock
            .Setup(x => x.GetProductByIdAsync(1, null))
            .ReturnsAsync(ServiceResult<ProductDto?>.SuccessAsOk(product));

        // Act
        var result = await _sut.AddToCartAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.ShippingCost.Should().Be(240.0m);
        result.Data.Total.Should().Be(340.0m, "100 + 240.00 kargo");
    }

    #endregion AddToCartAsync Tests

    #region RemoveFromCartAsync Tests

    [Fact]
    public async Task RemoveFromCartAsync_ExistingProduct_RemovesAndReturnsUpdatedCart()
    {
        // Arrange
        var existingCart = new CartDto();
        existingCart.Items.Add(new CartItemDto { ProductId = 1, UnitPrice = 100m, Quantity = 2 });
        existingCart.Items.Add(new CartItemDto { ProductId = 2, UnitPrice = 200m, Quantity = 1 });
        SetupCartWithItems(existingCart);

        // Act
        var result = await _sut.RemoveFromCartAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].ProductId.Should().Be(2);
    }

    [Fact]
    public async Task RemoveFromCartAsync_ProductNotInCart_ReturnsNotFound()
    {
        // Arrange
        SetupEmptyCart();

        // Act
        var result = await _sut.RemoveFromCartAsync(999);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion RemoveFromCartAsync Tests

    #region ClearCartAsync Tests

    [Fact]
    public async Task ClearCartAsync_Always_RemovesFromCacheAndReturnsNoContent()
    {
        // Act
        var result = await _sut.ClearCartAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);

        _cacheMock.Verify(
            x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion ClearCartAsync Tests
}