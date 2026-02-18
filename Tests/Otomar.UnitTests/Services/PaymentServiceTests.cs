using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Otomar.Application.Contracts.Services;
using Otomar.Persistance.Data;
using Otomar.Persistance.Options;
using Otomar.Persistance.Services;
using System.Net;

namespace Otomar.UnitTests.Services;

/// <summary>
/// PaymentService testleri - Input validasyonları ve erken dönüş senaryoları.
/// Not: Dapper extension method'ları mock'lanamadığı için DB-bağımlı senaryolar integration test gerektirir.
/// </summary>
public class PaymentServiceTests
{
    private readonly Mock<IAppDbContext> _contextMock;
    private readonly Mock<IHttpContextAccessor> _accessorMock;
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly Mock<IOrderService> _orderServiceMock;
    private readonly Mock<ICartService> _cartServiceMock;
    private readonly Mock<IProductService> _productServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly PaymentOptions _paymentOptions;
    private readonly RedisOptions _redisOptions;
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _contextMock = new Mock<IAppDbContext>();
        _accessorMock = new Mock<IHttpContextAccessor>();
        _identityServiceMock = new Mock<IIdentityService>();
        _loggerMock = new Mock<ILogger<PaymentService>>();
        _orderServiceMock = new Mock<IOrderService>();
        _cartServiceMock = new Mock<ICartService>();
        _productServiceMock = new Mock<IProductService>();
        _emailServiceMock = new Mock<IEmailService>();

        _paymentOptions = new PaymentOptions
        {
            ClientId = "100100100",
            Username = "testuser",
            Password = "testpass",
            StoreKey = "SKEY123456",
            ApiUrl = "https://test.example.com/api",
            ThreeDVerificationUrl = "https://test.example.com/3d",
            TransactionType = "Auth",
            Currency = "949",
            OkUrl = "https://test.example.com/ok",
            FailUrl = "https://test.example.com/fail",
            StoreType = "3D_PAY",
            HashAlgorithm = "ver3",
            Lang = "tr",
            RefreshTime = "5"
        };

        _redisOptions = new RedisOptions
        {
            ConnectionString = "localhost",
            InstanceName = "test:",
            CartExpirationDays = 7
        };

        _sut = new PaymentService(
            _contextMock.Object,
            new HttpClient(),
            _accessorMock.Object,
            _identityServiceMock.Object,
            _paymentOptions,
            _redisOptions,
            _loggerMock.Object,
            _orderServiceMock.Object,
            _cartServiceMock.Object,
            _productServiceMock.Object,
            _emailServiceMock.Object);
    }

    #region CompletePaymentAsync Tests

    [Fact]
    public async Task CompletePaymentAsync_NullParameters_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.CompletePaymentAsync(null!, CancellationToken.None);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompletePaymentAsync_InvalidMdStatus_ReturnsBadRequest()
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            { "mdStatus", "0" },
            { "oid", "OTOMAR-TEST-001" }
        };

        // Act
        var result = await _sut.CompletePaymentAsync(parameters, CancellationToken.None);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Fail!.Title.Should().Contain("3D Secure");
    }

    [Theory]
    [InlineData("5")]
    [InlineData("6")]
    [InlineData("7")]
    [InlineData("8")]
    public async Task CompletePaymentAsync_InvalidMdStatuses_ReturnsBadRequest(string mdStatus)
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            { "mdStatus", mdStatus }
        };

        // Act
        var result = await _sut.CompletePaymentAsync(parameters, CancellationToken.None);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompletePaymentAsync_ValidMdStatusButMissingOid_ReturnsBadRequest()
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            { "mdStatus", "1" }
        };

        // Act
        var result = await _sut.CompletePaymentAsync(parameters, CancellationToken.None);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Fail!.Title.Should().Contain("Sipariş Kodu");
    }

    #endregion

    #region GetPaymentByOrderCodeAsync - Validation Tests

    [Fact]
    public async Task GetPaymentByOrderCodeAsync_EmptyOrderCode_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetPaymentByOrderCodeAsync("");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPaymentByOrderCodeAsync_NullOrderCode_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetPaymentByOrderCodeAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetPaymentsByUserAsync - Validation Tests

    [Fact]
    public async Task GetPaymentsByUserAsync_EmptyUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetPaymentsByUserAsync("");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPaymentsByUserAsync_NullUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetPaymentsByUserAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
