using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Otomar.Persistance.Options;

namespace Otomar.Persistance.Extensions
{
    public static class OptionsExtension
    {
        public static IServiceCollection AddOptionsExtensions(this IServiceCollection services)
        {
            services.AddValidatedOptions<PaymentOptions>();
            services.AddValidatedOptions<EmailOptions>();
            services.AddValidatedOptions<ShippingOptions>();

            return services;
        }

        private static IServiceCollection AddValidatedOptions<TOptions>(this IServiceCollection services)
            where TOptions : class
        {
            services.AddOptions<TOptions>()
                .BindConfiguration(typeof(TOptions).Name)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<TOptions>>().Value);

            return services;
        }
    }
}