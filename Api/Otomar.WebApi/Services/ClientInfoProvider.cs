using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Otomar.Application.Interfaces.Services;
using System.Net;
using System.Net.Sockets;

namespace Otomar.WebApi.Services
{
    /// <summary>
    /// HTTP isteğinden client bilgilerini (IP, User-Agent) elde eden servis.
    /// Proxy header'larını öncelik sırasıyla kontrol eder.
    /// </summary>
    public class ClientInfoProvider : IClientInfoProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ClientInfoProvider> _logger;

        public ClientInfoProvider(IHttpContextAccessor httpContextAccessor, ILogger<ClientInfoProvider> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <inheritdoc />
        public string GetClientIp()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return "127.0.0.1";

            // Öncelik sırası: X-Client-IP → CF-Connecting-IP → X-Forwarded-For → X-Real-IP → RemoteIpAddress
            var headerChecks = new (string HeaderName, bool TakeFirst)[]
            {
                ("X-Client-IP", false),
                ("CF-Connecting-IP", false),
                ("X-Forwarded-For", true),
                ("X-Real-IP", false)
            };

            foreach (var (headerName, takeFirst) in headerChecks)
            {
                if (httpContext.Request.Headers.TryGetValue(headerName, out var headerValue))
                {
                    var ip = takeFirst
                        ? headerValue.ToString().Split(',')[0].Trim()
                        : headerValue.ToString().Trim();

                    if (!string.IsNullOrEmpty(ip) && !IsPrivateIpAddress(ip))
                        return NormalizeIpAddress(ip);
                }
            }

            var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (remoteIp == "::1" || remoteIp == "127.0.0.1")
                return GetLocalIpAddress();

            return NormalizeIpAddress(remoteIp);
        }

        /// <inheritdoc />
        public string GetUserAgent()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return string.Empty;

            // X-User-Agent öncelikli (UI'dan forward edilen gerçek User-Agent)
            if (httpContext.Request.Headers.TryGetValue("X-User-Agent", out var customUa))
            {
                var ua = customUa.ToString().Trim();
                if (!string.IsNullOrEmpty(ua))
                    return ua;
            }

            if (httpContext.Request.Headers.TryGetValue("User-Agent", out var standardUa))
            {
                var ua = standardUa.ToString().Trim();
                if (!string.IsNullOrEmpty(ua))
                    return ua;
            }

            return string.Empty;
        }

        private static string NormalizeIpAddress(string ip)
        {
            return ip == "::1" ? "127.0.0.1" : ip;
        }

        private string GetLocalIpAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());

                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return ip.ToString();
                }

                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetworkV6 &&
                        !ip.IsIPv6LinkLocal && ip.ToString() != "::1")
                        return ip.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Local IP adresi alınamadı, fallback kullanılıyor");
            }

            return "127.0.0.1";
        }

        private static bool IsPrivateIpAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return true;

            if (ipAddress is "::1" or "127.0.0.1" or "localhost")
                return true;

            if (!IPAddress.TryParse(ipAddress, out var ip))
                return true;

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();

                return bytes[0] switch
                {
                    10 => true,                                                     // 10.0.0.0/8
                    127 => true,                                                    // 127.0.0.0/8
                    172 => bytes[1] >= 16 && bytes[1] <= 31,                        // 172.16.0.0/12
                    192 => bytes[1] == 168,                                         // 192.168.0.0/16
                    169 => bytes[1] == 254,                                         // 169.254.0.0/16
                    _ => false
                };
            }

            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (IPAddress.IsLoopback(ip) || ip.IsIPv6LinkLocal)
                    return true;

                var bytes = ip.GetAddressBytes();
                if (bytes[0] is 0xFC or 0xFD)                                      // fc00::/7
                    return true;
            }

            return false;
        }
    }
}
