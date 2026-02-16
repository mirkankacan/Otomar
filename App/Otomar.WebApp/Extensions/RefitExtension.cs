using Otomar.WebApp.Handlers;
using Otomar.WebApp.Options;
using Otomar.WebApp.Services.Refit;
using Refit;

namespace Otomar.WebApp.Extensions
{
    public static class RefitExtension
    {
        public static IServiceCollection AddRefitClients(this IServiceCollection services, IConfiguration configuration)
        {
            var apiOptions = configuration.GetSection(nameof(ApiOptions)).Get<ApiOptions>()!;
            var baseAddress = new Uri(apiOptions.BaseUrl);
            var timeout = TimeSpan.FromSeconds(60);
            services.AddTransient<CartSessionHandler>();
            services.AddTransient<BearerTokenHandler>();

            // IAuthApi: Bearer eklenmez (login/register/refresh bu client ile yapılıyor)
            services.AddRefitClient<IAuthApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = baseAddress;
                    c.DefaultRequestHeaders.Add("Accept", "application/json");
                    c.Timeout = timeout;
                });

            services.AddRefitClient<ICartApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = baseAddress;
                    c.DefaultRequestHeaders.Add("Accept", "application/json");
                    c.Timeout = timeout;
                })
                .AddHttpMessageHandler<CartSessionHandler>()
                .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddRefitClient<ICategoryApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = baseAddress;
                    c.DefaultRequestHeaders.Add("Accept", "application/json");
                    c.Timeout = timeout;
                });

            services.AddRefitClient<IClientApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = baseAddress;
                    c.DefaultRequestHeaders.Add("Accept", "application/json");
                    c.Timeout = timeout;
                })
                .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddRefitClient<IListSearchApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = baseAddress;
                    c.DefaultRequestHeaders.Add("Accept", "application/json");
                    c.Timeout = timeout;
                })
                .AddHttpMessageHandler<BearerTokenHandler>();

            // FormData (multipart) istekleri için aynı base URL + Bearer
            services.AddHttpClient("OtomarApi", c =>
                {
                    c.BaseAddress = baseAddress;
                    c.DefaultRequestHeaders.Add("Accept", "application/json");
                    c.Timeout = timeout;
                })
                .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddRefitClient<IOptionApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = baseAddress;
                    c.DefaultRequestHeaders.Add("Accept", "application/json");
                    c.Timeout = timeout;
                });

            services.AddRefitClient<IOrderApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = baseAddress;
                    c.DefaultRequestHeaders.Add("Accept", "application/json");
                    c.Timeout = timeout;
                })
                .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddRefitClient<IPaymentApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = baseAddress;
                    c.DefaultRequestHeaders.Add("Accept", "application/json");
                    c.Timeout = timeout;
                })
                .AddHttpMessageHandler<CartSessionHandler>();

            services.AddRefitClient<IProductApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = baseAddress;
                    c.DefaultRequestHeaders.Add("Accept", "application/json");
                    c.Timeout = timeout;
                })
                .AddHttpMessageHandler<BearerTokenHandler>();

            return services;
        }
    }
}