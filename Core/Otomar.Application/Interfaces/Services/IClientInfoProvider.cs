namespace Otomar.Application.Interfaces.Services
{
    /// <summary>
    /// HTTP isteğinden client bilgilerini (IP, User-Agent) elde etmeye yarayan servis.
    /// Implementation, Presentation/Infrastructure katmanında HttpContext üzerinden çalışır.
    /// </summary>
    public interface IClientInfoProvider
    {
        /// <summary>
        /// Client'ın gerçek IP adresini döndürür.
        /// Proxy header'larını (X-Client-IP, CF-Connecting-IP, X-Forwarded-For, X-Real-IP) öncelik sırasıyla kontrol eder.
        /// </summary>
        /// <returns>Client IP adresi. Belirlenemezse "127.0.0.1" döner.</returns>
        string GetClientIp();

        /// <summary>
        /// Client'ın User-Agent bilgisini döndürür.
        /// Önce X-User-Agent, sonra standart User-Agent header'ını kontrol eder.
        /// </summary>
        /// <returns>User-Agent string'i. Belirlenemezse boş string döner.</returns>
        string GetUserAgent();
    }
}
