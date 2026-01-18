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

            // X-Forwarded-For header'ını kontrol et (proxy/load balancer için)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                var ip = ips[0].Trim();
                return NormalizeIpAddress(ip);
            }

            // X-Real-IP header'ını kontrol et
            var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
                return NormalizeIpAddress(realIp);

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
    }
}