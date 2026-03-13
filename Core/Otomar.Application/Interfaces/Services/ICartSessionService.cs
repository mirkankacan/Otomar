namespace Otomar.Application.Interfaces.Services
{
    /// <summary>
    /// HTTP isteğinden sepet oturumunu yöneten servis.
    /// Cart key oluşturma, session ID tespiti ve sepet birleştirme işlemlerini sağlar.
    /// Implementation, Presentation/Infrastructure katmanında HttpContext ve Redis üzerinden çalışır.
    /// </summary>
    public interface ICartSessionService
    {
        /// <summary>
        /// Mevcut kullanıcı veya session için Redis cart key'ini döndürür.
        /// Öncelik: 1) Authenticated user ID, 2) Header, 3) Cookie
        /// </summary>
        /// <returns>Redis cart key (örn: "otomar:cart:user:{userId}" veya "otomar:cart:session:{sessionId}").</returns>
        string GetCartKey();

        /// <summary>
        /// Mevcut HTTP isteğinden session ID'yi döndürür (header veya cookie'den).
        /// </summary>
        /// <returns>Session ID veya null.</returns>
        string? GetSessionId();

        /// <summary>
        /// Kullanıcı login olduğunda session sepetini user sepetine taşır/birleştirir.
        /// </summary>
        Task MergeCartsOnLoginAsync(CancellationToken cancellationToken = default);
    }
}
