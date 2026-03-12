using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Otomar.Shared.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Shared.Dtos.Cart;
using Otomar.Shared.Dtos.Order;
using Otomar.Application.Contracts.Persistence;
using Otomar.Application.Contracts.Persistence.Repositories;
using Otomar.Application.Options;
using Otomar.Application.Services;
using System.Net;

namespace Otomar.UnitTests.Services;

/// <summary>
/// OrderService testleri - Input validasyonları ve yetki kontrolü senaryoları.
/// </summary>
public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<ILogger<OrderService>> _loggerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ICartService> _cartServiceMock;
    private readonly ShippingOptions _shippingOptions;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _identityServiceMock = new Mock<IIdentityService>();
        _loggerMock = new Mock<ILogger<OrderService>>();
        _emailServiceMock = new Mock<IEmailService>();
        _cartServiceMock = new Mock<ICartService>();
        _shippingOptions = new ShippingOptions { Threshold = 500, Cost = 49.90m };

        _sut = new OrderService(
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _identityServiceMock.Object,
            _loggerMock.Object,
            _shippingOptions,
            _emailServiceMock.Object,
            _cartServiceMock.Object);
    }

    #region CreateClientOrderAsync Tests

    [Fact]
    public async Task CreateClientOrderAsync_UserNotPaymentExempt_ReturnsForbidden()
    {
        // Arrange
        _identityServiceMock.Setup(x => x.IsUserPaymentExempt()).Returns(false);
        var dto = new CreateClientOrderDto
        {
            ClientName = "Test Client",
            ClientAddress = "Test Address",
            ClientPhone = "555-1234"
        };

        // Act
        var result = await _sut.CreateClientOrderAsync(dto, CancellationToken.None);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GetOrderByCodeAsync - Validation Tests

    [Fact]
    public async Task GetOrderByCodeAsync_EmptyOrderCode_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetOrderByCodeAsync("");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Fail!.Detail.Should().Contain("boş");
    }

    [Fact]
    public async Task GetOrderByCodeAsync_NullOrderCode_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetOrderByCodeAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetOrdersByUserAsync - Validation Tests

    [Fact]
    public async Task GetOrdersByUserAsync_EmptyUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetOrdersByUserAsync("");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrdersByUserAsync_NullUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetOrdersByUserAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetOrdersByUserAsync (Paged) - Validation Tests

    [Fact]
    public async Task GetOrdersByUserAsync_Paged_EmptyUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetOrdersByUserAsync("", 1, 10);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrdersByUserAsync_Paged_NullUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetOrdersByUserAsync(null!, 1, 10);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetOrdersByUserAsync (Non-Paged) - Happy Path Tests

    [Fact]
    public async Task GetOrdersByUserAsync_ValidUserId_ReturnsSuccessWithOrders()
    {
        // Arrange
        var userId = "user-1";
        var orders = new List<OrderDto>
        {
            new OrderDto { Id = Guid.NewGuid(), Code = "OTOMAR-001", BuyerId = userId },
            new OrderDto { Id = Guid.NewGuid(), Code = "OTOMAR-002", BuyerId = userId }
        };
        _orderRepositoryMock
            .Setup(x => x.GetByUserAsync(userId))
            .ReturnsAsync(orders);

        // Act
        var result = await _sut.GetOrdersByUserAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Should().HaveCount(2);
    }

    #endregion

    #region GetOrdersByUserAsync (Paged) - Happy Path Tests

    [Fact]
    public async Task GetOrdersByUserAsync_Paged_ValidUserId_ReturnsPagedResult()
    {
        // Arrange
        var userId = "user-1";
        var orders = new List<OrderDto>
        {
            new OrderDto { Id = Guid.NewGuid(), Code = "OTOMAR-001" }
        };
        _orderRepositoryMock
            .Setup(x => x.GetByUserPagedAsync(userId, 1, 10))
            .ReturnsAsync((orders.AsEnumerable(), 1));

        // Act
        var result = await _sut.GetOrdersByUserAsync(userId, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data!.Data.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrdersByUserAsync_Paged_PageSizeExceeds100_ClampedTo100()
    {
        // Arrange
        var userId = "user-1";
        _orderRepositoryMock
            .Setup(x => x.GetByUserPagedAsync(userId, 1, 100))
            .ReturnsAsync((Enumerable.Empty<OrderDto>(), 0));

        // Act
        var result = await _sut.GetOrdersByUserAsync(userId, 1, 999);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _orderRepositoryMock.Verify(x => x.GetByUserPagedAsync(userId, 1, 100), Times.Once);
    }

    [Fact]
    public async Task GetOrdersByUserAsync_Paged_PageNumberLessThan1_ClampedTo1()
    {
        // Arrange
        var userId = "user-1";
        _orderRepositoryMock
            .Setup(x => x.GetByUserPagedAsync(userId, 1, 10))
            .ReturnsAsync((Enumerable.Empty<OrderDto>(), 0));

        // Act
        var result = await _sut.GetOrdersByUserAsync(userId, -5, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _orderRepositoryMock.Verify(x => x.GetByUserPagedAsync(userId, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetOrdersByUserAsync_Paged_PageSizeLessThan1_ClampedTo10()
    {
        // Arrange
        var userId = "user-1";
        _orderRepositoryMock
            .Setup(x => x.GetByUserPagedAsync(userId, 1, 10))
            .ReturnsAsync((Enumerable.Empty<OrderDto>(), 0));

        // Act
        var result = await _sut.GetOrdersByUserAsync(userId, 1, 0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _orderRepositoryMock.Verify(x => x.GetByUserPagedAsync(userId, 1, 10), Times.Once);
    }

    #endregion

    #region GetClientOrdersByUserAsync - Validation Tests

    [Fact]
    public async Task GetClientOrdersByUserAsync_EmptyUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetClientOrdersByUserAsync("");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetClientOrdersByUserAsync_NullUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetClientOrdersByUserAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
