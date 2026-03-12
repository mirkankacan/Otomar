namespace Otomar.Application.Helpers
{
    /// <summary>
    /// Kredi kartı numarası doğrulama, maskeleme ve kart kuruluşu belirleme yardımcısı.
    /// VakıfBank entegrasyonu için BrandName kodları döndürür.
    /// </summary>
    public static class CardHelper
    {
        private const string BrandCodeVisa = "100";
        private const string BrandCodeMastercard = "200";
        private const string BrandCodeTroy = "300";
        private const string BrandCodeAmex = "400";

        /// <summary>
        /// Kart numarasından kart kuruluşunu (BrandName) belirler.
        /// </summary>
        /// <param name="cardNumber">Kart numarası (boşluksuz veya boşluklu).</param>
        /// <returns>VakıfBank BrandName kodu.</returns>
        public static string GetBrandName(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                throw new ArgumentException("Kart numarası boş olamaz.", nameof(cardNumber));

            cardNumber = StripFormatting(cardNumber);

            if (cardNumber.StartsWith("34") || cardNumber.StartsWith("37"))
                return BrandCodeAmex;

            if (cardNumber.StartsWith('4'))
                return BrandCodeVisa;

            if (cardNumber.StartsWith('5') || IsMastercardNewRange(cardNumber))
                return BrandCodeMastercard;

            if (cardNumber.StartsWith('9'))
                return BrandCodeTroy;

            return BrandCodeVisa;
        }

        /// <summary>
        /// Kart numarasından kart kuruluşu ismini döndürür.
        /// </summary>
        /// <param name="cardNumber">Kart numarası.</param>
        /// <returns>Kart kuruluşu ismi.</returns>
        public static string GetBrandDisplayName(string cardNumber)
        {
            return GetBrandName(cardNumber) switch
            {
                BrandCodeVisa => "VISA",
                BrandCodeMastercard => "MASTERCARD",
                BrandCodeTroy => "TROY",
                BrandCodeAmex => "AMERICAN EXPRESS",
                _ => "UNKNOWN"
            };
        }

        /// <summary>
        /// Kart numarasının geçerli olup olmadığını Luhn algoritması ile kontrol eder.
        /// </summary>
        /// <param name="cardNumber">Kart numarası.</param>
        /// <returns>Geçerli ise true.</returns>
        public static bool IsValidCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                return false;

            cardNumber = StripFormatting(cardNumber);

            if (!cardNumber.All(char.IsDigit))
                return false;

            if (cardNumber.Length < 13 || cardNumber.Length > 19)
                return false;

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
        /// Kart numarasını maskeler (örn: 1234 56** **** 7890).
        /// PCI DSS uyumlu: ilk 6 ve son 4 rakam görünür.
        /// </summary>
        /// <param name="cardNumber">Kart numarası.</param>
        /// <returns>Maskelenmiş kart numarası.</returns>
        public static string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                return string.Empty;

            cardNumber = StripFormatting(cardNumber);

            if (cardNumber.Length < 12)
                return cardNumber;

            var firstSix = cardNumber[..6];
            var lastFour = cardNumber[^4..];
            var maskedMiddle = new string('*', cardNumber.Length - 10);
            var masked = $"{firstSix}{maskedMiddle}{lastFour}";

            return string.Join(" ", Chunk(masked, 4));
        }

        /// <summary>
        /// Son kullanma tarihini YYMM formatına çevirir.
        /// </summary>
        /// <param name="expiryDate">MM/YY formatında son kullanma tarihi.</param>
        /// <returns>YYMM formatında string (örn: 2512).</returns>
        public static string ConvertExpiryDateToYYMM(string expiryDate)
        {
            if (string.IsNullOrWhiteSpace(expiryDate))
                return string.Empty;

            var parts = expiryDate.Split('/');
            if (parts.Length != 2)
                return expiryDate;

            var month = parts[0].PadLeft(2, '0');
            var year = parts[1].PadLeft(2, '0');

            return $"{year}{month}";
        }

        /// <summary>
        /// Tutarı VakıfBank formatına çevirir.
        /// </summary>
        /// <param name="amount">Tutar.</param>
        /// <returns>VakıfBank format (nokta ile ayrılmış, 2 hane küsurat).</returns>
        public static string FormatAmount(decimal amount)
        {
            return amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Mastercard 2221-2720 aralığını kontrol eder (2017 sonrası yeni BIN aralığı).
        /// </summary>
        private static bool IsMastercardNewRange(string cardNumber)
        {
            if (cardNumber.Length < 4)
                return false;

            if (!int.TryParse(cardNumber[..4], out var prefix))
                return false;

            return prefix >= 2221 && prefix <= 2720;
        }

        private static string StripFormatting(string cardNumber)
        {
            return cardNumber.Replace(" ", "").Replace("-", "");
        }

        private static IEnumerable<string> Chunk(string text, int size)
        {
            for (int i = 0; i < text.Length; i += size)
            {
                yield return text[i..Math.Min(i + size, text.Length)];
            }
        }
    }
}
