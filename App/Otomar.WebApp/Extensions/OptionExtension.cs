using Microsoft.Extensions.Options;
using Otomar.WebApp.Options;

namespace Otomar.WebApp.Extensions
{
    public static class OptionsExtension
    {
        public static IServiceCollection AddOptionsExtensions(this IServiceCollection services)
        {
            services.AddValidatedOptions<ApiOptions>();
            services.AddValidatedOptions<RecaptchaOptions>();

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