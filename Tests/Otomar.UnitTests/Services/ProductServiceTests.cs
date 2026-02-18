using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Otomar.Application.Contracts.Services;
using Otomar.Persistance.Data;
using Otomar.Persistance.Services;
using System.Net;

namespace Otomar.UnitTests.Services;

/// <summary>
/// ProductService testleri - Input validasyonları.
/// </summary>
public class ProductServiceTests
{
    private readonly Mock<IAppDbContext> _contextMock;
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _contextMock = new Mock<IAppDbContext>();
        _loggerMock = new Mock<ILogger<ProductService>>();
        _identityServiceMock = new Mock<IIdentityService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _sut = new ProductService(
            _contextMock.Object,
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
}
