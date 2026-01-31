namespace Otomar.WebApp.Handlers
{
    public class CartSessionHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CartSessionKey = "CartSessionId";
        private const string CartSessionHeader = "X-Cart-Session-Id";

        public CartSessionHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;

            if (context?.Session != null)
            {
                var sessionId = GetOrCreateCartSessionId(context.Session);
                request.Headers.Add(CartSessionHeader, sessionId);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private static string GetOrCreateCartSessionId(ISession session)
        {
            var sessionId = session.GetString(CartSessionKey);

            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString("N");
                session.SetString(CartSessionKey, sessionId);
            }

            return sessionId;
        }
    }
}