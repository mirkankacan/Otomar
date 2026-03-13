using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Otomar.WebApi.Services;
using System.Net;

namespace Otomar.UnitTests.Helpers;

/// <summary>
/// ClientInfoProvider testleri - IP adresi ve User-Agent tespit işlemleri.
/// </summary>
public class ClientInfoProviderTests
{
    private static ClientInfoProvider CreateProvider(
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
        var loggerMock = new Mock<ILogger<ClientInfoProvider>>();

        return new ClientInfoProvider(accessorMock.Object, loggerMock.Object);
    }

    private static ClientInfoProvider CreateProviderWithNullContext()
    {
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var loggerMock = new Mock<ILogger<ClientInfoProvider>>();

        return new ClientInfoProvider(accessorMock.Object, loggerMock.Object);
    }

    #region GetClientIp Tests

    [Fact]
    public void GetClientIp_NullHttpContext_ReturnsLoopback()
    {
        var provider = CreateProviderWithNullContext();

        provider.GetClientIp().Should().Be("127.0.0.1");
    }

    [Fact]
    public void GetClientIp_XClientIPHeader_ReturnsHeaderValue()
    {
        var provider = CreateProvider(
            new Dictionary<string, string> { { "X-Client-IP", "85.100.50.25" } });

        provider.GetClientIp().Should().Be("85.100.50.25");
    }

    [Fact]
    public void GetClientIp_CFConnectingIPHeader_ReturnsHeaderValue()
    {
        var provider = CreateProvider(
            new Dictionary<string, string> { { "CF-Connecting-IP", "203.0.113.50" } });

        provider.GetClientIp().Should().Be("203.0.113.50");
    }

    [Fact]
    public void GetClientIp_XForwardedForHeader_ReturnsFirstIp()
    {
        var provider = CreateProvider(
            new Dictionary<string, string> { { "X-Forwarded-For", "203.0.113.50, 70.41.3.18, 150.172.238.178" } });

        provider.GetClientIp().Should().Be("203.0.113.50");
    }

    [Fact]
    public void GetClientIp_XRealIPHeader_ReturnsHeaderValue()
    {
        var provider = CreateProvider(
            new Dictionary<string, string> { { "X-Real-IP", "198.51.100.25" } });

        provider.GetClientIp().Should().Be("198.51.100.25");
    }

    [Fact]
    public void GetClientIp_PrivateIPInHeader_SkipsToNextSource()
    {
        var provider = CreateProvider(
            new Dictionary<string, string>
            {
                { "X-Client-IP", "192.168.1.100" },
                { "CF-Connecting-IP", "85.100.50.25" }
            });

        provider.GetClientIp().Should().Be("85.100.50.25");
    }

    [Fact]
    public void GetClientIp_RemoteIpAddress_ReturnsRemoteIp()
    {
        var provider = CreateProvider(remoteIp: "85.100.50.25");

        provider.GetClientIp().Should().Be("85.100.50.25");
    }

    [Fact]
    public void GetClientIp_HeaderPriority_XClientIPFirst()
    {
        var provider = CreateProvider(
            new Dictionary<string, string>
            {
                { "X-Client-IP", "85.100.50.1" },
                { "CF-Connecting-IP", "85.100.50.2" },
                { "X-Forwarded-For", "85.100.50.3" },
                { "X-Real-IP", "85.100.50.4" }
            },
            remoteIp: "85.100.50.5");

        provider.GetClientIp().Should().Be("85.100.50.1");
    }

    [Theory]
    [InlineData("10.0.0.1")]      // 10.x.x.x
    [InlineData("172.16.0.1")]    // 172.16-31.x.x
    [InlineData("192.168.1.1")]   // 192.168.x.x
    [InlineData("169.254.1.1")]   // Link-local
    public void GetClientIp_PrivateIPsInHeaders_AreSkipped(string privateIp)
    {
        var provider = CreateProvider(
            new Dictionary<string, string>
            {
                { "X-Client-IP", privateIp },
                { "CF-Connecting-IP", "85.100.50.25" }
            });

        provider.GetClientIp().Should().Be("85.100.50.25");
    }

    #endregion

    #region GetUserAgent Tests

    [Fact]
    public void GetUserAgent_NullHttpContext_ReturnsEmpty()
    {
        var provider = CreateProviderWithNullContext();

        provider.GetUserAgent().Should().BeEmpty();
    }

    [Fact]
    public void GetUserAgent_XUserAgentHeader_ReturnsHeaderValue()
    {
        var provider = CreateProvider(
            new Dictionary<string, string> { { "X-User-Agent", "Mozilla/5.0 Custom" } });

        provider.GetUserAgent().Should().Be("Mozilla/5.0 Custom");
    }

    [Fact]
    public void GetUserAgent_StandardUserAgent_ReturnsHeaderValue()
    {
        var provider = CreateProvider(
            new Dictionary<string, string> { { "User-Agent", "Mozilla/5.0 Standard" } });

        provider.GetUserAgent().Should().Be("Mozilla/5.0 Standard");
    }

    [Fact]
    public void GetUserAgent_XUserAgentPriority_ReturnsCustomHeader()
    {
        var provider = CreateProvider(
            new Dictionary<string, string>
            {
                { "X-User-Agent", "Custom UA" },
                { "User-Agent", "Standard UA" }
            });

        provider.GetUserAgent().Should().Be("Custom UA");
    }

    [Fact]
    public void GetUserAgent_NoHeaders_ReturnsEmpty()
    {
        var provider = CreateProvider();

        provider.GetUserAgent().Should().BeEmpty();
    }

    #endregion
}
