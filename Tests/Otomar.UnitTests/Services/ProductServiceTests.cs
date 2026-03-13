using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Otomar.Application.Interfaces.Services;
using Otomar.Shared.Interfaces;
using Otomar.Application.Interfaces.Repositories;
using Otomar.Application.Interfaces;
using Otomar.Application.Services;
using Otomar.Shared.Common;
using Otomar.Shared.Dtos.Product;
using System.Net;

namespace Otomar.UnitTests.Services;

/// <summary>
/// ProductService testleri - Input validasyonları ve happy-path senaryoları.
/// </summary>
public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _loggerMock = new Mock<ILogger<ProductService>>();
        _identityServiceMock = new Mock<IIdentityService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _sut = new ProductService(
            _productRepositoryMock.Object,
            _loggerMock.Object,
            _identityServiceMock.Object,
            _httpContextAccessorMock.Object);
    }

    #region GetProductByCodeAsync - Validation Tests

    [Fact]
    public async Task GetProductByCodeAsync_EmptyCode_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetProductByCodeAsync("");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Fail!.Detail.Should().Contain("boş");
    }

    [Fact]
    public async Task GetProductByCodeAsync_NullCode_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetProductByCodeAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetProductByCodeAsync - Happy Path & Not Found

    [Fact]
    public async Task GetProductByCodeAsync_ExistingCode_ReturnsOkWithProduct()
    {
        // Arrange
        var expectedProduct = new ProductDto
        {
            ID = 1,
            STOK_KODU = "PRD-001",
            STOK_ADI = "Test Ürün",
            SATIS_FIYAT = 100.50m,
            STOK_BAKIYE = 25,
            URETICI_MARKA_LOGO = "logo.png",
            DOSYA_KONUM = "/images/prd-001.jpg"
        };

        _productRepositoryMock
            .Setup(r => r.GetByCodeAsync("PRD-001"))
            .ReturnsAsync(expectedProduct);

        // Act
        var result = await _sut.GetProductByCodeAsync("PRD-001");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Should().BeEquivalentTo(expectedProduct);
        _productRepositoryMock.Verify(r => r.GetByCodeAsync("PRD-001"), Times.Once);
    }

    [Fact]
    public async Task GetProductByCodeAsync_NonExistingCode_ReturnsNotFound()
    {
        // Arrange
        _productRepositoryMock
            .Setup(r => r.GetByCodeAsync("INVALID"))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _sut.GetProductByCodeAsync("INVALID");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetProductByIdAsync - Happy Path & Not Found

    [Fact]
    public async Task GetProductByIdAsync_ExistingId_ReturnsOkWithProduct()
    {
        // Arrange
        var expectedProduct = new ProductDto
        {
            ID = 42,
            STOK_KODU = "PRD-042",
            STOK_ADI = "Test Ürün By Id",
            SATIS_FIYAT = 250.00m,
            STOK_BAKIYE = 10,
            URETICI_MARKA_LOGO = "brand.png",
            DOSYA_KONUM = "/images/prd-042.jpg"
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(42, null))
            .ReturnsAsync(expectedProduct);

        // Act
        var result = await _sut.GetProductByIdAsync(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Should().BeEquivalentTo(expectedProduct);
        _productRepositoryMock.Verify(r => r.GetByIdAsync(42, null), Times.Once);
    }

    [Fact]
    public async Task GetProductByIdAsync_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(999, null))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _sut.GetProductByIdAsync(999);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetFeaturedProductsAsync - Happy Path

    [Fact]
    public async Task GetFeaturedProductsAsync_UnauthenticatedUser_ReturnsOkWithProducts()
    {
        // Arrange - unauthenticated user means no HttpContext / no user claims,
        // so GetUserGlobalFiltersAsync won't be called with a real userId.
        // We set up the httpContextAccessor to return null context (unauthenticated).
        _httpContextAccessorMock
            .Setup(h => h.HttpContext)
            .Returns((HttpContext?)null);

        var expectedFeatured = new FeaturedProductDto
        {
            Latest = new List<ProductDto> { CreateSampleProduct(1, "LATEST-001") },
            BestSeller = new List<ProductDto> { CreateSampleProduct(2, "BEST-001") },
            Lowestprice = new List<ProductDto> { CreateSampleProduct(3, "LOW-001") },
            HighestPrice = new List<ProductDto> { CreateSampleProduct(4, "HIGH-001") }
        };

        _productRepositoryMock
            .Setup(r => r.GetFeaturedProductsAsync(It.IsAny<List<(string FilterType, string FilterValue)>>()))
            .ReturnsAsync(expectedFeatured);

        // Act
        var result = await _sut.GetFeaturedProductsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Should().BeEquivalentTo(expectedFeatured);
    }

    #endregion

    #region GetProductsAsync - Happy Path

    [Fact]
    public async Task GetProductsAsync_ValidFilter_ReturnsOkWithPagedProducts()
    {
        // Arrange
        _httpContextAccessorMock
            .Setup(h => h.HttpContext)
            .Returns((HttpContext?)null);

        var filter = new ProductFilterRequestDto
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "test"
        };

        var products = new List<ProductDto>
        {
            CreateSampleProduct(1, "SEARCH-001"),
            CreateSampleProduct(2, "SEARCH-002")
        };
        var pagedResult = new PagedResult<ProductDto>(products, 1, 10, 2);

        _productRepositoryMock
            .Setup(r => r.GetFilteredAsync(filter, It.IsAny<List<(string FilterType, string FilterValue)>>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.GetProductsAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Should().BeEquivalentTo(pagedResult);
    }

    #endregion

    #region GetSimilarProductsByCodeAsync - Happy Path

    [Fact]
    public async Task GetSimilarProductsByCodeAsync_ValidCode_ReturnsOkWithSimilarProducts()
    {
        // Arrange
        var similarProducts = new List<SimilarProductDto?>
        {
            new SimilarProductDto(10, "SIM-001", "Benzer Ürün 1", "MFR-001", 120.00m, "/images/sim-001.jpg", "brand1.png"),
            new SimilarProductDto(11, "SIM-002", "Benzer Ürün 2", "MFR-002", 130.00m, "/images/sim-002.jpg", "brand2.png")
        };

        _productRepositoryMock
            .Setup(r => r.GetSimilarByCodeAsync("PRD-001"))
            .ReturnsAsync(similarProducts);

        // Act
        var result = await _sut.GetSimilarProductsByCodeAsync("PRD-001");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Should().BeEquivalentTo(similarProducts);
        _productRepositoryMock.Verify(r => r.GetSimilarByCodeAsync("PRD-001"), Times.Once);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates a sample <see cref="ProductDto"/> for test arrangements.
    /// </summary>
    private static ProductDto CreateSampleProduct(int id, string code) => new()
    {
        ID = id,
        STOK_KODU = code,
        STOK_ADI = $"Ürün {code}",
        SATIS_FIYAT = 100.00m + id,
        STOK_BAKIYE = id * 5,
        URETICI_MARKA_LOGO = "logo.png",
        DOSYA_KONUM = $"/images/{code.ToLowerInvariant()}.jpg"
    };

    #endregion
}
