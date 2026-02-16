using Otomar.WebApp.Options;
using Otomar.WebApp.Services.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Otomar.WebApp.Services
{
    public class RecaptchaService(
        IHttpClientFactory httpClientFactory,
        RecaptchaOptions options,
        ILogger<RecaptchaService> logger) : IRecaptchaService
    {
        private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

        public async Task<bool> VerifyAsync(
            string? token,
            string expectedAction,
            string? remoteIp = null,
            CancellationToken cancellationToken = default)
        {
            if (!options.Enabled)
                return true;

            if (string.IsNullOrWhiteSpace(token))
            {
                logger.LogWarning("reCAPTCHA token boş");
                return false;
            }

            var parameters = new Dictionary<string, string>
            {
                { "secret", options.SecretKey },
                { "response", token }
            };

            if (!string.IsNullOrWhiteSpace(remoteIp))
                parameters.Add("remoteip", remoteIp);

            var client = httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                VerifyUrl,
                new FormUrlEncodedContent(parameters),
                cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<RecaptchaResponse>(json);

            if (result == null)
            {
                logger.LogWarning("reCAPTCHA yanıtı deserialize edilemedi");
                return false;
            }

            if (!result.Success)
            {
                logger.LogWarning("reCAPTCHA doğrulaması başarısız. Hatalar: {Errors}",
                    string.Join(", ", result.ErrorCodes ?? []));
                return false;
            }

            if (result.Score < options.MinimumScore)
            {
                logger.LogWarning("reCAPTCHA skoru düşük. Skor: {Score}, Minimum: {Min}",
                    result.Score, options.MinimumScore);
                return false;
            }

            if (!string.Equals(result.Action, expectedAction, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("reCAPTCHA action uyuşmazlığı. Beklenen: {Expected}, Gelen: {Actual}",
                    expectedAction, result.Action);
                return false;
            }

            return true;
        }

        private class RecaptchaResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("score")]
            public double Score { get; set; }

            [JsonPropertyName("action")]
            public string? Action { get; set; }

            [JsonPropertyName("challenge_ts")]
            public DateTime ChallengeTs { get; set; }

            [JsonPropertyName("hostname")]
            public string? Hostname { get; set; }

            [JsonPropertyName("error-codes")]
            public string[]? ErrorCodes { get; set; }
        }
    }
}
