using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Otomar.Persistance.Helpers;
using System.Net;

namespace Otomar.UnitTests.Helpers;

/// <summary>
/// IpHelper testleri - IP adresi ve User-Agent tespit işlemleri.
/// </summary>
public class IpHelperTests
{
    private static Mock<IHttpContextAccessor> CreateAccessorWithHeaders(
        Dictionary<string, string>? headers = null,
        string? remoteIp = null)
    {
        var accessorMock = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();

        if (headers != null)
        {
            foreach (var header in headers)
            {
                httpContext.Request.Headers[header.Key] = header.Value;
            }
        }

        if (remoteIp != null)
        {
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        }

        accessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        return accessorMock;
    }

    #region GetClientIp Tests

    [Fact]
    public void GetClientIp_NullHttpContext_ReturnsLoopback()
    {
        // Arrange
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = IpHelper.GetClientIp(accessorMock.Object);

        // Assert
        result.Should().Be("127.0.0.1");
    }

    [Fact]
    public void GetClientIp_XClientIPHeader_ReturnsHeaderValue()
    {
        // Arrange
        var accessor = CreateAccessorWithHeaders(
            new Dictionary<string, string> { { "X-Client-IP", "85.100.50.25" } });

        // Act
        var result = IpHelper.GetClientIp(accessor.Object);

        // Assert
        result.Should().Be("85.100.50.25");
    }

    [Fact]
    public void GetClientIp_CFConnectingIPHeader_ReturnsHeaderValue()
    {
        // Arrange
        var accessor = CreateAccessorWithHeaders(
            new Dictionary<string, string> { { "CF-Connecting-IP", "203.0.113.50" } });

        // Act
        var result = IpHelper.GetClientIp(accessor.Object);

        // Assert
        result.Should().Be("203.0.113.50");
    }

    [Fact]
    public void GetClientIp_XForwardedForHeader_ReturnsFirstIp()
    {
        // Arrange
        var accessor = CreateAccessorWithHeaders(
            new Dictionary<string, string> { { "X-Forwarded-For", "203.0.113.50, 70.41.3.18, 150.172.238.178" } });

        // Act
        var result = IpHelper.GetClientIp(accessor.Object);

        // Assert
        result.Should().Be("203.0.113.50");
    }

    [Fact]
    public void GetClientIp_XRealIPHeader_ReturnsHeaderValue()
    {
        // Arrange
        var accessor = CreateAccessorWithHeaders(
            new Dictionary<string, string> { { "X-Real-IP", "198.51.100.25" } });

        // Act
        var result = IpHelper.GetClientIp(accessor.Object);

        // Assert
        result.Should().Be("198.51.100.25");
    }

    [Fact]
    public void GetClientIp_PrivateIPInHeader_SkipsToNextSource()
    {
        // Arrange - Private IP in X-Client-IP, public IP in CF-Connecting-IP
        var accessor = CreateAccessorWithHeaders(
            new Dictionary<string, string>
            {
                { "X-Client-IP", "192.168.1.100" },
                { "CF-Connecting-IP", "85.100.50.25" }
            });

        // Act
        var result = IpHelper.GetClientIp(accessor.Object);

        // Assert
        result.Should().Be("85.100.50.25");
    }

    [Fact]
    public void GetClientIp_RemoteIpAddress_ReturnsRemoteIp()
    {
        // Arrange
        var accessor = CreateAccessorWithHeaders(remoteIp: "85.100.50.25");

        // Act
        var result = IpHelper.GetClientIp(accessor.Object);

        // Assert
        result.Should().Be("85.100.50.25");
    }

    [Fact]
    public void GetClientIp_HeaderPriority_XClientIPFirst()
    {
        // Arrange - Tüm header'lar dolu, X-Client-IP öncelikli olmalı
        var accessor = CreateAccessorWithHeaders(
            new Dictionary<string, string>
            {
                { "X-Client-IP", "85.100.50.1" },
                { "CF-Connecting-IP", "85.100.50.2" },
                { "X-Forwarded-For", "85.100.50.3" },
                { "X-Real-IP", "85.100.50.4" }
            },
            remoteIp: "85.100.50.5");

        // Act
        var result = IpHelper.GetClientIp(accessor.Object);

        // Assert
        result.Should().Be("85.100.50.1");
    }

    [Theory]
    [InlineData("10.0.0.1")]      // 10.x.x.x
    [InlineData("172.16.0.1")]    // 172.16-31.x.x
    [InlineData("192.168.1.1")]   // 192.168.x.x
    [InlineData("169.254.1.1")]   // Link-local
    public void GetClientIp_PrivateIPsInHeaders_AreSkipped(string privateIp)
    {
        // Arrange
        var accessor = CreateAccessorWithHeaders(
            new Dictionary<string, string>
            {
                { "X-Client-IP", privateIp },
                { "CF-Connecting-IP", "85.100.50.25" }
            });

        // Act
        var result = IpHelper.GetClientIp(accessor.Object);

        // Assert
        result.Should().Be("85.100.50.25");
    }

    #endregion

    #region GetUserAgent Tests

    [Fact]
    public void GetUserAgent_NullHttpContext_ReturnsEmpty()
    {
        // Arrange
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = IpHelper.GetUserAgent(accessorMock.Object);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetUserAgent_XUserAgentHeader_ReturnsHeaderValue()
    {
        // Arrange
        var accessor = CreateAccessorWithHeaders(
            new Dictionary<string, string> { { "X-User-Agent", "Mozilla/5.0 Custom" } });

        // Act
        var result = IpHelper.GetUserAgent(accessor.Object);

        // Assert
        result.Should().Be("Mozilla/5.0 Custom");
    }

    [Fact]
    public void GetUserAgent_StandardUserAgent_ReturnsHeaderValue()
    {
        // Arrange
        var accessor = CreateAccessorWithHeaders(
            new Dictionary<string, string> { { "User-Agent", "Mozilla/5.0 Standard" } });

        // Act
        var result = IpHelper.GetUserAgent(accessor.Object);

        // Assert
        result.Should().Be("Mozilla/5.0 Standard");
    }

    [Fact]
    public void GetUserAgent_XUserAgentPriority_ReturnsCustomHeader()
    {
        // Arrange
        var accessor = CreateAccessorWithHeaders(
            new Dictionary<string, string>
            {
                { "X-User-Agent", "Custom UA" },
                { "User-Agent", "Standard UA" }
            });

        // Act
        var result = IpHelper.GetUserAgent(accessor.Object);

        // Assert
        result.Should().Be("Custom UA");
    }

    [Fact]
    public void GetUserAgent_NoHeaders_ReturnsEmpty()
    {
        // Arrange
        var accessor = CreateAccessorWithHeaders();

        // Act
        var result = IpHelper.GetUserAgent(accessor.Object);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
