using Otomar.Shared.Dtos.Payment;
using System.Globalization;

namespace Otomar.Application.Helpers
{
    /// <summary>
    /// İş Bankası sanal POS entegrasyonu için pure utility fonksiyonları.
    /// Durum kontrolü, format dönüşümü gibi side effect içermeyen işlemler.
    /// </summary>
    public static class IsBankHelper
    {
        /// <summary>
        /// Tutarı İş Bankası formatına çevirir (virgül ile ayrılmış, 2 hane).
        /// </summary>
        /// <param name="amount">Tutar.</param>
        /// <returns>Banka formatında tutar (örn: "123,45").</returns>
        public static string IsBankAmountConvert(decimal amount)
        {
            return amount.ToString("F2", CultureInfo.InvariantCulture).Replace(".", ",");
        }

        /// <summary>
        /// Banka cevabının başarılı olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="dto">Banka cevap DTO'su.</param>
        /// <returns>Ödeme onaylanmışsa true.</returns>
        public static bool IsPaymentSuccess(IsBankResponseDto dto)
        {
            return dto.Response.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                && dto.ProcReturnCode == "00";
        }

        /// <summary>
        /// ProcReturnCode ile işlem başarısını kontrol eder.
        /// </summary>
        /// <param name="procReturnCode">Banka işlem kodu.</param>
        /// <returns>"00" ise true.</returns>
        public static bool IsSuccess(string procReturnCode)
        {
            return procReturnCode == "00";
        }

        /// <summary>
        /// 3D Secure MdStatus değerinin geçerli olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="mdStatus">MdStatus değeri.</param>
        /// <returns>Geçerli statülerden biriyse (1-4) true.</returns>
        public static bool IsThreeDSecureValid(string mdStatus)
        {
            return mdStatus is "1" or "2" or "3" or "4";
        }

        /// <summary>
        /// 3D Secure MdStatus değerinin Türkçe açıklamasını döndürür.
        /// </summary>
        /// <param name="mdStatus">MdStatus değeri.</param>
        /// <returns>İnsan-okunabilir durum mesajı.</returns>
        public static string GetThreeDSecureStatusMessage(string mdStatus)
        {
            return mdStatus switch
            {
                "1" => "3D Secure doğrulama başarılı (Full Secure)",
                "2" or "3" or "4" => "3D Secure doğrulama kısmen başarılı (Half Secure)",
                "5" or "6" or "7" or "8" => "Kart 3D Secure programına kayıtlı değil veya işlem reddedildi",
                "0" => "3D Secure doğrulama başarısız",
                _ => $"Bilinmeyen durum: {mdStatus}"
            };
        }
    }
}
