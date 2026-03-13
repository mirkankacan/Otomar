using Otomar.Shared.Dtos.Payment;

namespace Otomar.Application.Interfaces.Services
{
    /// <summary>
    /// İş Bankası sanal POS entegrasyonu için ödeme servisi.
    /// Hash üretimi, XML request/response işleme ve HTTP iletişimini sağlar.
    /// </summary>
    public interface IIsBankPaymentService
    {
        /// <summary>
        /// Sanal POS'a ödeme isteği gönderir ve cevabı döndürür.
        /// </summary>
        /// <param name="parameters">3D Secure callback parametreleri.</param>
        /// <param name="cancellationToken">İptal token'ı.</param>
        /// <returns>Banka cevap DTO'su.</returns>
        Task<IsBankResponseDto> SendPaymentRequestAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Form verilerinden SHA-512 hash üretir.
        /// </summary>
        /// <param name="formData">Hash'lenecek form verileri.</param>
        /// <returns>Base64 encoded SHA-512 hash.</returns>
        string GenerateHash(Dictionary<string, string> formData);

        /// <summary>
        /// Callback'ten gelen hash'i doğrular.
        /// </summary>
        /// <param name="parameters">Hash dahil callback parametreleri.</param>
        /// <returns>Hash geçerliyse true.</returns>
        bool ValidateHash(Dictionary<string, string> parameters);

        /// <summary>
        /// CC5Request XML'i oluşturur.
        /// </summary>
        /// <param name="parameters">İstek parametreleri.</param>
        /// <returns>XML string.</returns>
        string BuildRequestXml(Dictionary<string, string> parameters);

        /// <summary>
        /// Banka XML cevabını DTO'ya parse eder.
        /// </summary>
        /// <param name="responseXml">XML string.</param>
        /// <returns>Parse edilmiş DTO.</returns>
        IsBankResponseDto ParseResponse(string responseXml);
    }
}
