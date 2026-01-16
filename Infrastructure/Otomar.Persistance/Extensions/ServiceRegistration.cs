using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Otomar.Application.Contracts.Services;
using Otomar.Persistance.Data;
using Otomar.Persistance.Options;
using Otomar.Persistance.Services;

namespace Otomar.Persistance.Extensions
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddPersistanceServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient();

            services.AddHttpContextAccessor();
            services.AddStackExchangeRedisCache(options =>
            {
                var redisOptions = configuration.GetSection(nameof(RedisOptions)).Get<RedisOptions>()!;

                options.Configuration = redisOptions.ConnectionString;
                options.InstanceName = redisOptions.InstanceName;
            });

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