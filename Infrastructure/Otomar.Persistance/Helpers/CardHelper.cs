namespace Otomar.Persistance.Helpers
{
    public static class CardHelper
    {
        /// <summary>
        /// Kart numarasından kart kuruluşunu (BrandName) belirler
        /// </summary>
        /// <param name="cardNumber">Kart numarası (boşluksuz)</param>
        /// <returns>VakıfBank BrandName kodu</returns>
        public static string GetBrandName(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                throw new ArgumentException("Kart numarası boş olamaz", nameof(cardNumber));

            // Boşlukları temizle
            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

            // İlk rakamları kontrol et
            if (cardNumber.StartsWith("4"))
                return "100"; // VISA

            if (cardNumber.StartsWith("5"))
                return "200"; // MASTERCARD

            if (cardNumber.StartsWith("9"))
                return "300"; // TROY

            if (cardNumber.StartsWith("34") || cardNumber.StartsWith("37"))
                return "400"; // AMEX

            // Bilinmeyen kart tipi - default VISA
            return "100";
        }

        /// <summary>
        /// Kart numarasından kart kuruluşu ismini döndürür
        /// </summary>
        /// <param name="cardNumber">Kart numarası</param>
        /// <returns>Kart kuruluşu ismi</returns>
        public static string GetBrandDisplayName(string cardNumber)
        {
            var brandCode = GetBrandName(cardNumber);

            return brandCode switch
            {
                "100" => "VISA",
                "200" => "MASTERCARD",
                "300" => "TROY",
                "400" => "AMERICAN EXPRESS",
                _ => "UNKNOWN"
            };
        }

        /// <summary>
        /// Kart numarasının geçerli olup olmadığını kontrol eder (Luhn algoritması)
        /// </summary>
        /// <param name="cardNumber">Kart numarası</param>
        /// <returns>Geçerli ise true</returns>
        public static bool IsValidCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                return false;

            // Boşlukları temizle
            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

            // Sadece rakam kontrolü
            if (!cardNumber.All(char.IsDigit))
                return false;

            // Uzunluk kontrolü (13-19 arası)
            if (cardNumber.Length < 13 || cardNumber.Length > 19)
                return false;

            // Luhn algoritması
            int sum = 0;
            bool isSecond = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int digit = cardNumber[i] - '0';

                if (isSecond)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                isSecond = !isSecond;
            }

            return sum % 10 == 0;
        }

        /// <summary>
        /// Kart numarasını maskeler (örn: 1234 5678 **** 3456)
        /// </summary>
        /// <param name="cardNumber">Kart numarası</param>
        /// <returns>Maskelenmiş kart numarası</returns>
        public static string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                return string.Empty;

            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

            if (cardNumber.Length < 12)
                return cardNumber;

            // İlk 6 ve son 4 rakamı göster
            var firstSix = cardNumber.Substring(0, 6);
            var lastFour = cardNumber.Substring(cardNumber.Length - 4);
            var masked = $"{firstSix}****{lastFour}";

            // 4'lü gruplara böl
            return string.Join(" ", Enumerable.Range(0, masked.Length / 4)
                .Select(i => masked.Substring(i * 4, 4)));
        }

        /// <summary>
        /// Son kullanma tarihini YYMM formatına çevirir
        /// </summary>
        /// <param name="month">Ay (1-12)</param>
        /// <param name="year">Yıl (2025, 26, vb.)</param>
        /// <returns>YYMM formatında string (örn: 2512)</returns>
        public static string ConvertExpiryDateToYYMM(string expiryDate)
        {
            if (string.IsNullOrWhiteSpace(expiryDate))
                return string.Empty;

            // MM/YY formatından MM ve YY'yi al
            var parts = expiryDate.Split('/');
            if (parts.Length != 2)
                return expiryDate;

            var month = parts[0].PadLeft(2, '0');
            var year = parts[1].PadLeft(2, '0');

            // YYMM formatına çevir
            return $"{year}{month}";
        }

        /// <summary>
        /// Tutarı VakıfBank formatına çevirir
        /// </summary>
        /// <param name="amount">Tutar</param>
        /// <returns>VakıfBank format (nokta ile, 2 hane küsurat)</returns>
        public static string FormatAmount(decimal amount)
        {
            // VakıfBank format: 123.45 (nokta ile ayrılmış, 2 hane küsurat)
            return amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}