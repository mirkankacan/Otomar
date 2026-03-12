namespace Otomar.WebApp.Handlers
{
    public class CartSessionHandler : DelegatingHandler
    {
        private const string CartSessionKey = "CartSessionId";
        private const string CartSessionHeader = "X-Cart-Session-Id";
        private const string CartSessionCookieName = ".OTOMAR.CartSessionId";
        private static readonly TimeSpan CartSessionCookieMaxAge = TimeSpan.FromDays(7);

        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartSessionHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return await base.SendAsync(request, cancellationToken);

            var sessionId = GetOrCreateCartSessionId(context);
            request.Headers.Add(CartSessionHeader, sessionId);

            return await base.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Session'dan veya cookie'den cart session id alır; yoksa yeni üretir.
        /// Sunucu yeniden başlasa bile cookie sayesinde aynı sepet korunur.
        /// </summary>
        private string GetOrCreateCartSessionId(HttpContext context)
        {
            // 1) Önce session'a bak
            var sessionId = context.Session?.GetString(CartSessionKey);
            if (!string.IsNullOrEmpty(sessionId) && IsValidGuid(sessionId))
            {
                EnsureCartSessionCookie(context, sessionId);
                return sessionId;
            }

            // 2) Session boşsa (örn. restart sonrası) cookie'den oku
            if (context.Request.Cookies.TryGetValue(CartSessionCookieName, out var cookieValue) &&
                !string.IsNullOrEmpty(cookieValue) && IsValidGuid(cookieValue))
            {
                sessionId = cookieValue;
                context.Session?.SetString(CartSessionKey, sessionId);
                return sessionId;
            }

            // 3) Yeni id üret, session ve cookie'e yaz
            sessionId = Guid.NewGuid().ToString("N");
            context.Session?.SetString(CartSessionKey, sessionId);
            EnsureCartSessionCookie(context, sessionId);
            return sessionId;
        }

        private static void EnsureCartSessionCookie(HttpContext context, string sessionId)
        {
            context.Response.Cookies.Append(CartSessionCookieName, sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                MaxAge = CartSessionCookieMaxAge,
                IsEssential = true
            });
        }

        private static bool IsValidGuid(string value)
        {
            return value != null && value.Length == 32 && Guid.TryParseExact(value, "N", out _);
        }
    }
}