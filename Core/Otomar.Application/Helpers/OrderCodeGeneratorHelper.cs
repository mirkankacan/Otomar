namespace Otomar.Application.Helpers
{
    /// <summary>
    /// Benzersiz sipariş kodu üretici.
    /// Format: OTOMAR-{yyyyMMddHHmmss}-{5 haneli random}
    /// </summary>
    public static class OrderCodeGeneratorHelper
    {
        /// <summary>
        /// Benzersiz sipariş kodu üretir.
        /// Thread-safe random kullanır ve UTC zaman damgası ile çakışma riskini minimize eder.
        /// </summary>
        /// <returns>OTOMAR-{timestamp}-{random} formatında sipariş kodu.</returns>
        public static string Generate()
        {
            var randomNumber = Random.Shared.Next(10000, 99999);
            return $"OTOMAR-{DateTime.UtcNow:yyyyMMddHHmmss}-{randomNumber}";
        }
    }
}
