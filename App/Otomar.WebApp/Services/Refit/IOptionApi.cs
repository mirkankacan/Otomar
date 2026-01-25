using Otomar.WebApp.Options;
using Refit;

namespace Otomar.WebApp.Services.Refit
{
    public interface IOptionApi
    {
        [Get("/api/options/shipping")]
        Task<ShippingOptions> GetShippingOptionsAsync(CancellationToken cancellationToken = default);
    }
}
