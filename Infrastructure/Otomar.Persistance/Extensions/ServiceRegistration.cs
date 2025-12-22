using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Otomar.Application.Contracts.Services;
using Otomar.Persistance.Data;
using Otomar.Persistance.Services;
using StackExchange.Redis;

namespace Otomar.Persistance.Extensions
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddPersistanceServices(this IServiceCollection services)
        {
            services.AddHttpClient();

            services.AddHttpContextAccessor();
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var redisConnectionString = configuration.GetValue<string>("RedisOptions:ConnectionString");
                return ConnectionMultiplexer.Connect(redisConnectionString);
            });
            services.AddDistributedMemoryCache();

            services.AddScoped<IAppDbContext, AppDbContext>();
            services.AddScoped<IIdentityService, IdentityService>();

            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IListSearchService, ListSearchService>();
            services.AddScoped<ICartService, CartService>();

            return services;
        }
    }
}