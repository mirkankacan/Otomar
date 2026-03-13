using Otomar.Application.Interfaces.Services;
using Otomar.Application.Options;
using Otomar.Shared.Dtos.Payment;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Otomar.Persistence.Services
{
    /// <summary>
    /// İş Bankası sanal POS entegrasyonu için ödeme servisi.
    /// Hash üretimi, XML request/response işleme ve HTTP iletişimini sağlar.
    /// </summary>
    public class IsBankPaymentService : IIsBankPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly PaymentOptions _paymentOptions;

        public IsBankPaymentService(HttpClient httpClient, PaymentOptions paymentOptions)
        {
            _httpClient = httpClient;
            _paymentOptions = paymentOptions;
        }

        /// <inheritdoc />
        public async Task<IsBankResponseDto> SendPaymentRequestAsync(
            Dictionary<string, string> parameters,
            CancellationToken cancellationToken)
        {
            var requestXml = BuildRequestXml(parameters);

            using var response = await _httpClient.PostAsync(
                _paymentOptions.ApiUrl,
                new StringContent(requestXml, Encoding.UTF8, "text/xml"),
                cancellationToken);

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseResponse(responseString);
        }

        /// <inheritdoc />
        public string GenerateHash(Dictionary<string, string> formData)
        {
            var sortedParams = formData
                .Where(p => !string.Equals(p.Key, "encoding", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(p.Key, "hash", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Key.ToLower(new CultureInfo("en-US", false)))
                .ToList();

            var hashVal = new StringBuilder();

            foreach (var pair in sortedParams)
            {
                var escapedValue = pair.Value?.Replace("\\", "\\\\").Replace("|", "\\|") ?? string.Empty;
                hashVal.Append(escapedValue).Append('|');
            }

            hashVal.Append(_paymentOptions.StoreKey);

            var hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(hashVal.ToString()));
            return Convert.ToBase64String(hashBytes);
        }

        /// <inheritdoc />
        public bool ValidateHash(Dictionary<string, string> parameters)
        {
            var receivedHash = parameters.GetValueOrDefault("hash");

            if (string.IsNullOrEmpty(receivedHash))
                return false;

            var calculatedHash = GenerateHash(parameters);
            return calculatedHash.Equals(receivedHash, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public string BuildRequestXml(Dictionary<string, string> parameters)
        {
            var xml = new XElement("CC5Request",
                new XElement("Name", _paymentOptions.Username),
                new XElement("Password", _paymentOptions.Password),
                new XElement("ClientId", _paymentOptions.ClientId),
                new XElement("Type", parameters["TranType"]),
                new XElement("Email", parameters["Email"]),
                new XElement("OrderId", parameters["oid"]),
                new XElement("Total", parameters["amount"]),
                new XElement("Currency", parameters["currency"]),
                new XElement("Instalment", parameters["Instalment"]),
                new XElement("Number", parameters["md"]),
                new XElement("PayerAuthenticationCode", parameters["cavv"]),
                new XElement("PayerSecurityLevel", parameters["eci"]),
                new XElement("PayerTxnId", parameters["xid"])
            );

            return xml.ToString();
        }

        /// <inheritdoc />
        public IsBankResponseDto ParseResponse(string responseXml)
        {
            var serializer = new XmlSerializer(typeof(IsBankResponseDto));
            using var reader = new StringReader(responseXml);
            return (IsBankResponseDto)(serializer.Deserialize(reader)
                ?? throw new InvalidOperationException("Banka cevabı parse edilemedi."));
        }
    }
}
