using Microsoft.Extensions.DependencyInjection;
using Otomar.Application.Contracts.Services;
using Otomar.Persistance.Data;
using Otomar.Persistance.Services;

namespace Otomar.Persistance.Extensions
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddPersistanceServices(this IServiceCollection services)
        {
            services.AddHttpClient();

            services.AddHttpContextAccessor();
            services.AddScoped<IAppDbContext, AppDbContext>();
            services.AddScoped<IIdentityService, IdentityService>();

            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IListSearchService, ListSearchService>();
            return services;
        }
    }
}