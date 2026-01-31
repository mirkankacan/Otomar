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
                }).AddHttpMessageHandler<CartSessionHandler>();

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
                });

            services.AddRefitClient<IListSearchApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = baseAddress;
                    c.DefaultRequestHeaders.Add("Accept", "application/json");
                    c.Timeout = timeout;
                });

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
                });

            services.AddRefitClient<IPaymentApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = baseAddress;
                    c.DefaultRequestHeaders.Add("Accept", "application/json");
                    c.Timeout = timeout;
                }).AddHttpMessageHandler<CartSessionHandler>();

            services.AddRefitClient<IProductApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = baseAddress;
                    c.DefaultRequestHeaders.Add("Accept", "application/json");
                    c.Timeout = timeout;
                });

            return services;
        }
    }
}