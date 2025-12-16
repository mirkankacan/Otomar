using Otomar.WebApp.Responses;
using Otomar.WebApp.Services.Interfaces;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Otomar.WebApp.Services
{
    public class ApiService(HttpClient httpClient, ILogger<ApiService> logger) : IApiService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("GET isteği gönderiliyor: {Endpoint}", endpoint);

                var response = await httpClient.GetAsync(endpoint, cancellationToken);
                return await HandleResponse<T>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GET isteği başarısız: {Endpoint}", endpoint);
                return ApiResponse<T>.Failure("İstek sırasında bir hata oluştu", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("POST isteği gönderiliyor: {Endpoint}", endpoint);

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(endpoint, content, cancellationToken);
                return await HandleResponse<T>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST isteği başarısız: {Endpoint}", endpoint);
                return ApiResponse<T>.Failure("İstek sırasında bir hata oluştu", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("PUT isteği gönderiliyor: {Endpoint}", endpoint);

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync(endpoint, content, cancellationToken);
                return await HandleResponse<T>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT isteği başarısız: {Endpoint}", endpoint);
                return ApiResponse<T>.Failure("İstek sırasında bir hata oluştu", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("DELETE isteği gönderiliyor: {Endpoint}", endpoint);

                var response = await httpClient.DeleteAsync(endpoint, cancellationToken);
                return await HandleResponse<T>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DELETE isteği başarısız: {Endpoint}", endpoint);
                return ApiResponse<T>.Failure("İstek sırasında bir hata oluştu", HttpStatusCode.InternalServerError);
            }
        }

        // Response döndürmeyen metodlar (NoContent gibi)
        public async Task<ApiResponse> PostAsync(string endpoint, object data, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("POST isteği gönderiliyor: {Endpoint}", endpoint);

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(endpoint, content, cancellationToken);
                return await HandleResponse(response, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "POST isteği başarısız: {Endpoint}", endpoint);
                return ApiResponse.Failure("İstek sırasında bir hata oluştu", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ApiResponse> PutAsync(string endpoint, object data, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("PUT isteği gönderiliyor: {Endpoint}", endpoint);

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync(endpoint, content, cancellationToken);
                return await HandleResponse(response, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PUT isteği başarısız: {Endpoint}", endpoint);
                return ApiResponse.Failure("İstek sırasında bir hata oluştu", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ApiResponse> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("DELETE isteği gönderiliyor: {Endpoint}", endpoint);

                var response = await httpClient.DeleteAsync(endpoint, cancellationToken);
                return await HandleResponse(response, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DELETE isteği başarısız: {Endpoint}", endpoint);
                return ApiResponse.Failure("İstek sırasında bir hata oluştu", HttpStatusCode.InternalServerError);
            }
        }

        private async Task<ApiResponse<T>> HandleResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var statusCode = response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                if (statusCode == HttpStatusCode.NoContent)
                {
                    return ApiResponse<T>.Success(default, statusCode);
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                return ApiResponse<T>.Success(data, statusCode);
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("API hatası: {StatusCode} - {Error}", statusCode, errorContent);

            return ApiResponse<T>.Failure(errorContent, statusCode);
        }

        private async Task<ApiResponse> HandleResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var statusCode = response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                return ApiResponse.Success(statusCode);
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("API hatası: {StatusCode} - {Error}", statusCode, errorContent);

            return ApiResponse.Failure(errorContent, statusCode);
        }
    }
}