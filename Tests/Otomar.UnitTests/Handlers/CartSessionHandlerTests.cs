using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Otomar.WebApp.Handlers;
using System.Net;

namespace Otomar.UnitTests.Handlers;

/// <summary>
/// CartSessionHandler testleri - Sepet session yönetimi ve cookie işlemleri.
/// DelegatingHandler test etmek için InnerHandler olarak mock HttpMessageHandler kullanılır.
/// </summary>
public class CartSessionHandlerTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    public CartSessionHandlerTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    }

    private CartSessionHandler CreateHandler(HttpContext? httpContext)
    {
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var handler = new CartSessionHandler(_httpContextAccessorMock.Object)
        {
            InnerHandler = new TestMessageHandler()
        };

        return handler;
    }

    [Fact]
    public async Task SendAsync_NullHttpContext_DoesNotAddHeader()
    {
        // Arrange
        var handler = CreateHandler(null);
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://test.example.com/api/cart");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        request.Headers.Contains("X-Cart-Session-Id").Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WithHttpContext_AddsCartSessionHeader()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Session = new TestSession();
        var handler = CreateHandler(httpContext);
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://test.example.com/api/cart");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        request.Headers.Contains("X-Cart-Session-Id").Should().BeTrue();
        var sessionId = request.Headers.GetValues("X-Cart-Session-Id").First();
        sessionId.Should().NotBeNullOrEmpty();
        sessionId.Length.Should().Be(32); // Guid "N" format = 32 chars
    }

    [Fact]
    public async Task SendAsync_ExistingSessionId_ReusesSameId()
    {
        // Arrange
        var existingSessionId = Guid.NewGuid().ToString("N");
        var httpContext = new DefaultHttpContext();
        var session = new TestSession();
        session.SetString("CartSessionId", existingSessionId);
        httpContext.Session = session;

        var handler = CreateHandler(httpContext);
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://test.example.com/api/cart");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        var sessionId = request.Headers.GetValues("X-Cart-Session-Id").First();
        sessionId.Should().Be(existingSessionId);
    }

    [Fact]
    public async Task SendAsync_SessionIdFromCookie_UseCookieValue()
    {
        // Arrange
        var cookieSessionId = Guid.NewGuid().ToString("N");
        var httpContext = new DefaultHttpContext();
        httpContext.Session = new TestSession();
        httpContext.Request.Headers["Cookie"] = $".OTOMAR.CartSessionId={cookieSessionId}";

        var handler = CreateHandler(httpContext);
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://test.example.com/api/cart");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        var sessionId = request.Headers.GetValues("X-Cart-Session-Id").First();
        sessionId.Should().Be(cookieSessionId);
    }

    /// <summary>
    /// Test için basit HttpMessageHandler - her isteğe 200 OK döner.
    /// </summary>
    private class TestMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    /// <summary>
    /// Test için basit ISession implementasyonu.
    /// </summary>
    private class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public string Id => Guid.NewGuid().ToString();
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[]? value) => _store.TryGetValue(key, out value);
    }
}
