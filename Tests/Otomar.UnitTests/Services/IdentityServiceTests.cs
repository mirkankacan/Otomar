using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Otomar.Persistance.Services;
using System.Security.Claims;

namespace Otomar.UnitTests.Services;

/// <summary>
/// IdentityService testleri - Mock kullanımına giriş.
/// IHttpContextAccessor mock'lanarak farklı claim senaryoları test edilir.
/// </summary>
public class IdentityServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly IdentityService _sut; // SUT = System Under Test (test edilen sınıf)

    public IdentityServiceTests()
    {
        // Her test için temiz bir mock oluştur
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _sut = new IdentityService(_httpContextAccessorMock.Object);
    }

    /// <summary>
    /// HttpContext'e claim'ler ekleyen yardımcı method.
    /// Test'lerde tekrar eden setup kodunu azaltır.
    /// </summary>
    private void SetupHttpContextWithClaims(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    #region GetUserId Tests

    [Fact]
    public void GetUserId_UserAuthenticated_ReturnsUserId()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid().ToString().ToUpper();
        SetupHttpContextWithClaims(
            new Claim(ClaimTypes.NameIdentifier, expectedUserId)
        );

        // Act
        var result = _sut.GetUserId();

        // Assert
        result.Should().Be(expectedUserId);
    }

    #endregion

    #region GetUserEmail Tests

    [Fact]
    public void GetUserEmail_UserAuthenticated_ReturnsEmail()
    {
        // Arrange
        SetupHttpContextWithClaims(
            new Claim(ClaimTypes.Email, "test@otomar.com")
        );

        // Act
        var result = _sut.GetUserEmail();

        // Assert
        result.Should().Be("test@otomar.com");
    }

    #endregion

    #region GetUserFullName Tests

    [Fact]
    public void GetUserFullName_UserAuthenticated_ReturnsFullName()
    {
        // Arrange
        SetupHttpContextWithClaims(
            new Claim(ClaimTypes.GivenName, "Ahmet Yılmaz")
        );

        // Act
        var result = _sut.GetUserFullName();

        // Assert
        result.Should().Be("Ahmet Yılmaz");
    }

    #endregion

    #region IsUserPaymentExempt Tests

    [Theory]
    [InlineData("True", true)]
    [InlineData("False", false)]
    public void IsUserPaymentExempt_WithClaim_ReturnsCorrectValue(string claimValue, bool expected)
    {
        // Arrange
        SetupHttpContextWithClaims(
            new Claim("IsPaymentExempt", claimValue)
        );

        // Act
        var result = _sut.IsUserPaymentExempt();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region GetClientCode Tests

    [Fact]
    public void GetClientCode_WithClientCode_ReturnsCode()
    {
        // Arrange
        SetupHttpContextWithClaims(
            new Claim("ClientCode", "C-12345")
        );

        // Act
        var result = _sut.GetClientCode();

        // Assert
        result.Should().Be("C-12345");
    }

    [Fact]
    public void GetClientCode_WithoutClientCode_ReturnsNull()
    {
        // Arrange - ClientCode claim'i yok
        SetupHttpContextWithClaims(
            new Claim(ClaimTypes.NameIdentifier, "some-user-id")
        );

        // Act
        var result = _sut.GetClientCode();

        // Assert
        result.Should().BeNull("B2B müşterisi olmayan kullanıcıların ClientCode'u null olmalı");
    }

    #endregion

    #region GetUserPhoneNumber Tests

    [Fact]
    public void GetUserPhoneNumber_WithPhoneClaim_ReturnsPhoneNumber()
    {
        // Arrange
        SetupHttpContextWithClaims(
            new Claim(ClaimTypes.MobilePhone, "+905551234567")
        );

        // Act
        var result = _sut.GetUserPhoneNumber();

        // Assert
        result.Should().Be("+905551234567");
    }

    #endregion
}
