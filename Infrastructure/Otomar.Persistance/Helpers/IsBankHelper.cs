using Otomar.Application.Dtos.Payment;
using Otomar.Persistance.Options;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Otomar.Persistance.Helpers
{
    public static class IsBankHelper
    {
        public static string ParseIsBankRequest(Dictionary<string, string> parameters, PaymentOptions paymentOptions)
        {
            var xml = new XElement("CC5Request",
                new XElement("Name", paymentOptions.Username),
                new XElement("Password", paymentOptions.Password),
                new XElement("ClientId", paymentOptions.ClientId),
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

        public static string GenerateHash(Dictionary<string, string> formData, PaymentOptions paymentOptions)
        {
            var sortedParams = formData
                .Where(p => !string.Equals(p.Key, "encoding", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(p.Key, "hash", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Key.ToLower(new CultureInfo("en-US", false)))
                .ToList();

            var hashVal = new StringBuilder();
            var paramsKeys = new StringBuilder();

            foreach (var pair in sortedParams)
            {
                var escapedValue = pair.Value?.Replace("\\", "\\\\").Replace("|", "\\|") ?? string.Empty;
                var lowerKey = pair.Key.ToLower(new CultureInfo("en-US", false));

                hashVal.Append(escapedValue).Append("|");
                paramsKeys.Append(lowerKey).Append("|");
            }

            hashVal.Append(paymentOptions.StoreKey);

            using var sha = System.Security.Cryptography.SHA512.Create();
            var hashBytes = Encoding.UTF8.GetBytes(hashVal.ToString());
            var computedHash = sha.ComputeHash(hashBytes);

            return Convert.ToBase64String(computedHash);
        }

        public static bool ValidateHash(Dictionary<string, string> parameters, PaymentOptions paymentOptions)
        {
            var receivedHash = parameters.GetValueOrDefault("hash");

            if (string.IsNullOrEmpty(receivedHash))
                return false;

            var calculatedHash = IsBankHelper.GenerateHash(parameters, paymentOptions);

            return calculatedHash.Equals(receivedHash, StringComparison.Ordinal);
        }

        public static async Task<IsBankResponseDto> IsBankPaymentRequest(HttpClient httpClient, Dictionary<string, string> parameters, PaymentOptions paymentOptions, CancellationToken cancellationToken)
        {
            var requestAsXmlString = IsBankHelper.ParseIsBankRequest(parameters, paymentOptions);
            using var responseFromBank = await httpClient.PostAsync(paymentOptions.ApiUrl, new StringContent(requestAsXmlString, Encoding.UTF8, "text/xml"), cancellationToken);
            string responseAsString = await responseFromBank.Content.ReadAsStringAsync(cancellationToken);
            var responseAsDto = IsBankHelper.ParseIsBankResponse(responseAsString);
            return responseAsDto;
        }

        public static IsBankResponseDto ParseIsBankResponse(string responseAsString)
        {
            var serializer = new XmlSerializer(typeof(IsBankResponseDto));
            using var reader = new StringReader(responseAsString);
            return (IsBankResponseDto)serializer.Deserialize(reader);
        }

        public static bool IsPaymentSuccess(IsBankResponseDto dto)
        {
            return dto.Response.Equals("Approved", StringComparison.OrdinalIgnoreCase) && dto.ProcReturnCode == "00";
        }

        public static bool IsThreeDSecureValid(string mdStatus)
        {
            var validStatuses = new[] { "1", "2", "3", "4" };
            if (!validStatuses.Contains(mdStatus))
            {
                return true;
            }
            return false;
        }

        public static string GetThreeDSecureStatusMessage(string mdStatus)
        {
            return mdStatus switch
            {
                "1" => "3D Secure doğrulama başarılı (Full Secure)",
                "2" or "3" or "4" => "3D Secure doğrulama kısmen başarılı (Half Secure)",
                "5" or "6" or "7" or "8" => "Kart 3D Secure programına kayıtlı değil veya işlem reddedildi",
                "0" => "3D Secure doğrulama başarsız",
                _ => $"Bilinmeyen durum: {mdStatus}"
            };
        }
    }
}