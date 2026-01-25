using Microsoft.AspNetCore.Http;

namespace Otomar.Persistance.Helpers
{
    public static class IpHelper
    {
        /// <summary>
        /// Client IP adresini alır
        /// </summary>
        public static string GetClientIp(IHttpContextAccessor httpContextAccessor)
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
                return "127.0.0.1";

            // Önce X-Client-IP header'ını kontrol et (UI'dan gelen gerçek client IP)
            if (httpContext.Request.Headers.TryGetValue("X-Client-IP", out var clientIpHeader))
            {
                var clientIp = clientIpHeader.ToString().Trim();
                if (!string.IsNullOrEmpty(clientIp) && !IsPrivateIpAddress(clientIp))
                {
                    return NormalizeIpAddress(clientIp);
                }
            }

            // X-Forwarded-For header'ını kontrol et
            if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var forwardedIp = forwardedFor.ToString().Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(forwardedIp) && !IsPrivateIpAddress(forwardedIp))
                {
                    return NormalizeIpAddress(forwardedIp);
                }
            }

            // X-Real-IP header'ını kontrol et
            if (httpContext.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
            {
                var realIpAddress = realIp.ToString().Trim();
                if (!string.IsNullOrEmpty(realIpAddress) && !IsPrivateIpAddress(realIpAddress))
                {
                    return NormalizeIpAddress(realIpAddress);
                }
            }

            // RemoteIpAddress'i kullan
            var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // ::1 (IPv6 loopback) ise, local IP'yi almaya çalış
            if (remoteIp == "::1" || remoteIp == "127.0.0.1")
            {
                return GetLocalIpAddress();
            }

            return NormalizeIpAddress(remoteIp);
        }

        private static string NormalizeIpAddress(string ip)
        {
            // IPv6 loopback'i IPv4'e çevir
            if (ip == "::1")
            {
                return GetLocalIpAddress();
            }

            return ip;
        }

        private static string GetLocalIpAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());

                // Önce IPv4 adreslerini kontrol et
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }

                // IPv4 bulunamazsa IPv6'yı al (::1 hariç)
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 &&
                        !ip.IsIPv6LinkLocal && ip.ToString() != "::1")
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {
                // Hata durumunda fallback
            }

            return "127.0.0.1";
        }

        /// <summary>
        /// IP adresinin private (özel) olup olmadığını kontrol eder
        /// </summary>
        private static bool IsPrivateIpAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return true;

            // IPv6 loopback
            if (ipAddress == "::1")
                return true;

            // IPv4 loopback
            if (ipAddress == "127.0.0.1" || ipAddress == "localhost")
                return true;

            if (!System.Net.IPAddress.TryParse(ipAddress, out var ip))
                return true;

            // IPv4 private ranges kontrolü
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();

                // 10.0.0.0/8
                if (bytes[0] == 10)
                    return true;

                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    return true;

                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168)
                    return true;

                // 127.0.0.0/8 (loopback)
                if (bytes[0] == 127)
                    return true;

                // 169.254.0.0/16 (link-local)
                if (bytes[0] == 169 && bytes[1] == 254)
                    return true;
            }

            // IPv6 private ranges kontrolü
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                // IPv6 loopback (::1)
                if (ipAddress == "::1" || System.Net.IPAddress.IsLoopback(ip))
                    return true;

                // IPv6 link-local
                if (ip.IsIPv6LinkLocal)
                    return true;

                // IPv6 unique local (fc00::/7)
                var bytes = ip.GetAddressBytes();
                if (bytes[0] == 0xFC || bytes[0] == 0xFD)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// User-Agent'ı alır
        /// </summary>
        public static string GetUserAgent(IHttpContextAccessor httpContextAccessor)
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
                return string.Empty;

            // Önce X-User-Agent header'ını kontrol et (UI'dan gelen gerçek User-Agent)
            if (httpContext.Request.Headers.TryGetValue("X-User-Agent", out var userAgentHeader))
            {
                var userAgent = userAgentHeader.ToString().Trim();
                if (!string.IsNullOrEmpty(userAgent))
                {
                    return userAgent;
                }
            }

            // Standard User-Agent header'ını kontrol et
            if (httpContext.Request.Headers.TryGetValue("User-Agent", out var standardUserAgent))
            {
                var userAgent = standardUserAgent.ToString().Trim();
                if (!string.IsNullOrEmpty(userAgent))
                {
                    return userAgent;
                }
            }

            return string.Empty;
        }
    }
}