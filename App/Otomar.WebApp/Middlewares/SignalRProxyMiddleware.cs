using System.Net.WebSockets;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Otomar.WebApp.Options;

namespace Otomar.WebApp.Middlewares;

/// <summary>
/// WebApp üzerinden gelen SignalR isteklerini WebApi hub'ına proxy eder.
/// JS tarafı token görmez; middleware cookie auth'tan JWT alıp WebApi'ye iletir.
/// </summary>
public class SignalRProxyMiddleware(RequestDelegate next)
{
    private const string HubPath = "/hubs/notification";

    public async Task InvokeAsync(HttpContext context, IOptions<ApiOptions> apiOptions)
    {
        var path = context.Request.Path.Value ?? "";
        if (!path.StartsWith(HubPath, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var apiBaseUrl = apiOptions.Value.BaseUrl.TrimEnd('/');
        var accessToken = await context.GetTokenAsync("access_token") ?? "";

        if (context.WebSockets.IsWebSocketRequest)
        {
            await ProxyWebSocketAsync(context, apiBaseUrl, accessToken);
        }
        else
        {
            await ProxyHttpAsync(context, apiBaseUrl, accessToken);
        }
    }

    /// <summary>
    /// SignalR negotiate ve diğer HTTP isteklerini WebApi'ye proxy eder.
    /// </summary>
    private static async Task ProxyHttpAsync(HttpContext context, string apiBaseUrl, string accessToken)
    {
        var targetUri = BuildTargetUri(apiBaseUrl, context.Request);

        using var httpClient = new HttpClient();
        var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = new Uri(targetUri)
        };

        if (!string.IsNullOrEmpty(accessToken))
        {
            requestMessage.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        // Request body'yi kopyala (POST negotiate için)
        if (context.Request.ContentLength > 0 || context.Request.ContentType != null)
        {
            var bodyStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(bodyStream);
            bodyStream.Position = 0;
            requestMessage.Content = new StreamContent(bodyStream);
            if (context.Request.ContentType != null)
            {
                requestMessage.Content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(context.Request.ContentType);
            }
        }

        using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        // Hop-by-hop header'ları kaldır
        context.Response.Headers.Remove("transfer-encoding");

        await response.Content.CopyToAsync(context.Response.Body);
    }

    /// <summary>
    /// WebSocket bağlantısını WebApi hub'ına proxy eder.
    /// </summary>
    private static async Task ProxyWebSocketAsync(HttpContext context, string apiBaseUrl, string accessToken)
    {
        var targetWsUri = BuildTargetWsUri(apiBaseUrl, context.Request, accessToken);

        using var clientSocket = await context.WebSockets.AcceptWebSocketAsync();
        using var serverSocket = new ClientWebSocket();

        if (!string.IsNullOrEmpty(accessToken))
        {
            serverSocket.Options.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        }

        try
        {
            await serverSocket.ConnectAsync(new Uri(targetWsUri), CancellationToken.None);
        }
        catch
        {
            await clientSocket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable,
                "Backend unavailable", CancellationToken.None);
            return;
        }

        // Çift yönlü veri aktarımı
        var clientToServer = RelayAsync(clientSocket, serverSocket, CancellationToken.None);
        var serverToClient = RelayAsync(serverSocket, clientSocket, CancellationToken.None);

        await Task.WhenAny(clientToServer, serverToClient);

        await CloseSocketSafe(clientSocket);
        await CloseSocketSafe(serverSocket);
    }

    private static async Task RelayAsync(WebSocket source, WebSocket destination, CancellationToken ct)
    {
        var buffer = new byte[4096];
        try
        {
            while (source.State == WebSocketState.Open && destination.State == WebSocketState.Open)
            {
                var result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
                await destination.SendAsync(
                    new ArraySegment<byte>(buffer, 0, result.Count),
                    result.MessageType,
                    result.EndOfMessage,
                    ct);
            }
        }
        catch (WebSocketException)
        {
            // Bağlantı koptuğunda sessizce çık
        }
    }

    private static async Task CloseSocketSafe(WebSocket socket)
    {
        try
        {
            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
        catch
        {
            // Zaten kapalı
        }
    }

    private static string BuildTargetUri(string apiBaseUrl, HttpRequest request)
    {
        var path = request.Path.Value ?? "";
        var query = request.QueryString.Value ?? "";
        return $"{apiBaseUrl}{path}{query}";
    }

    private static string BuildTargetWsUri(string apiBaseUrl, HttpRequest request, string accessToken)
    {
        var wsBase = apiBaseUrl
            .Replace("https://", "wss://", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "ws://", StringComparison.OrdinalIgnoreCase);

        var path = request.Path.Value ?? "";
        var query = request.QueryString.Value ?? "";

        // access_token'ı query string'e ekle (SignalR WebSocket protokolü bunu bekler)
        var separator = string.IsNullOrEmpty(query) ? "?" : "&";
        if (!string.IsNullOrEmpty(accessToken))
        {
            query += $"{separator}access_token={Uri.EscapeDataString(accessToken)}";
        }

        return $"{wsBase}{path}{query}";
    }
}

/// <summary>
/// SignalR proxy middleware extension metodu.
/// </summary>
public static class SignalRProxyMiddlewareExtensions
{
    public static IApplicationBuilder UseSignalRProxy(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SignalRProxyMiddleware>();
    }
}
