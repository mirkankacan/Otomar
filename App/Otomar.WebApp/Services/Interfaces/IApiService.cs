using Otomar.WebApp.Responses;

namespace Otomar.WebApp.Services.Interfaces
{
    public interface IApiService
    {
        Task<ApiResponse<T>> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);

        Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default);

        Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default);

        Task<ApiResponse<T>> DeleteAsync<T>(string endpoint, CancellationToken cancellationToken = default);

        Task<ApiResponse> PostAsync(string endpoint, object data, CancellationToken cancellationToken = default);

        Task<ApiResponse> PutAsync(string endpoint, object data, CancellationToken cancellationToken = default);

        Task<ApiResponse> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
    }
}