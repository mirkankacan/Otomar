using Humanizer;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using Otomar.Application.Interfaces.Services;
using Otomar.Persistence.Options;
using Otomar.Shared.Dtos.ListSearch;
using Otomar.Shared.Dtos.Order;
using Otomar.Shared.Dtos.Payment;
using System.Globalization;
using System.Net;

namespace Otomar.Persistence.Services
{
    public class EmailService(ILogger<EmailService> logger, EmailOptions emailOptions) : IEmailService
    {
        public async Task SendPaymentFailedMailAsync(OrderDto order, PaymentDto payment, CancellationToken cancellationToken)
        {
            var body = LoadTemplate("PaymentFailedMailTemplate.html");
            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("PaymentFailedMailTemplate.html yüklenemedi, e-posta gönderilmedi.");
                return;
            }
            string eInoviceUser = order.Corporate?.IsEInvoiceUser == true ? "Evet"
                          : order.Corporate?.IsEInvoiceUser == false ? "Hayır"
                          : string.Empty;
            var itemsTableRows = string.Join(Environment.NewLine, order.Items.Select(item =>
                     $@"
                    <tr>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: left;'>{WebUtility.HtmlEncode(item.ProductName)}</td>
       <td style='padding: 10px; border: 1px solid #ccc; text-align: right;'>{item.UnitPrice.ToString("C2", new CultureInfo("tr-TR"))}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: center;'>{item.Quantity}</td>
       <td style='padding: 10px; border: 1px solid #ccc; text-align: right;'>{(item.UnitPrice * item.Quantity).ToString("C2", new CultureInfo("tr-TR"))}</td>
                    </tr>")) +
                     $@"
                <tr>
                <td colspan='3' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>Alt Toplam</td>
                <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{order.SubTotalAmount.ToString("C2", new CultureInfo("tr-TR"))}</td>
            </tr>
            <tr>
                <td colspan='3' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>Kargo</td>
                <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{order.ShippingAmount.Value.ToString("C2", new CultureInfo("tr-TR"))}</td>
            </tr>
            <tr>
                <td colspan='3' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>Toplam</td>
                <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{order.TotalAmount.ToString("C2", new CultureInfo("tr-TR"))}</td>
            </tr>";

            body = body
                         .Replace("{{OrderCode}}", WebUtility.HtmlEncode(payment.OrderCode))
                         .Replace("{{Amount}}", payment.TotalAmount.ToString("C2", new CultureInfo("tr-TR")))
                         .Replace("{{CreatedAt}}", payment.CreatedAt.ToString("dd/MM/yyyy HH:mm"))
                         .Replace("{{MaskedCreditCard}}", WebUtility.HtmlEncode(payment.MaskedCreditCard))
                         .Replace("{{CardBrand}}", WebUtility.HtmlEncode(payment.BankCardBrand))
                         .Replace("{{CardIssuer}}", WebUtility.HtmlEncode(payment.BankCardIssuer))
                         .Replace("{{BankErrorCode}}", WebUtility.HtmlEncode(payment.BankErrorCode ?? "-"))
                         .Replace("{{BankErrMsg}}", WebUtility.HtmlEncode(payment.BankErrMsg ?? "-"))
                         .Replace("{{BankProcReturnCode}}", WebUtility.HtmlEncode(payment.BankProcReturnCode))
                         .Replace("{{Name}}", WebUtility.HtmlEncode(order.BillingAddress.Name))
                         .Replace("{{IdentityNumber}}", WebUtility.HtmlEncode(order.IdentityNumber))
                         .Replace("{{ItemsTable}}", itemsTableRows)
                         .Replace("{{BillingStreet}}", WebUtility.HtmlEncode(order.BillingAddress.Street))
                         .Replace("{{BillingDistrict}}", WebUtility.HtmlEncode(order.BillingAddress.District))
                         .Replace("{{BillingCity}}", WebUtility.HtmlEncode(order.BillingAddress.City))
                         .Replace("{{BillingPhone}}", WebUtility.HtmlEncode(order.BillingAddress.Phone))
                         .Replace("{{CompanyName}}", WebUtility.HtmlEncode(order.Corporate?.CompanyName ?? string.Empty))
                         .Replace("{{TaxOffice}}", WebUtility.HtmlEncode(order.Corporate?.TaxOffice ?? string.Empty))
                         .Replace("{{TaxNumber}}", WebUtility.HtmlEncode(order.Corporate?.TaxNumber ?? string.Empty))
                         .Replace("{{IsEInvoiceUser}}", WebUtility.HtmlEncode(eInoviceUser))
                         .Replace("{{Email}}", WebUtility.HtmlEncode(order.Email))
                         .Replace("{{ShippingName}}", WebUtility.HtmlEncode(order.ShippingAddress.Name))
                         .Replace("{{ShippingStreet}}", WebUtility.HtmlEncode(order.ShippingAddress.Street))
                         .Replace("{{ShippingDistrict}}", WebUtility.HtmlEncode(order.ShippingAddress.District))
                         .Replace("{{ShippingCity}}", WebUtility.HtmlEncode(order.ShippingAddress.City))
                         .Replace("{{ShippingPhone}}", WebUtility.HtmlEncode(order.ShippingAddress.Phone));

            const string subject = "Ödemede Hata ❌";
            await SendInternalAsync(subject, body, null, null, null, isHtml: true, true, cancellationToken);
        }

        public async Task SendPaymentSuccessMailAsync(OrderDto order, CancellationToken cancellationToken)
        {
            var body = LoadTemplate("OrderSuccessMailTemplate.html");
            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("OrderSuccessMailTemplate.html yüklenemedi, e-posta gönderilmedi.");
                return;
            }

            string eInoviceUser = order.Corporate?.IsEInvoiceUser == true ? "Evet"
                                : order.Corporate?.IsEInvoiceUser == false ? "Hayır"
                                : string.Empty;

            var itemsTableRows = string.Join(Environment.NewLine, order.Items.Select(item =>
                     $@"
                    <tr>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: left;'>{WebUtility.HtmlEncode(item.ProductName)}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: left;'>{WebUtility.HtmlEncode(item.ProductCode)}</td>
       <td style='padding: 10px; border: 1px solid #ccc; text-align: right;'>{item.UnitPrice.ToString("C2", new CultureInfo("tr-TR"))}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: center;'>{item.Quantity}</td>
       <td style='padding: 10px; border: 1px solid #ccc; text-align: right;'>{(item.UnitPrice * item.Quantity).ToString("C2", new CultureInfo("tr-TR"))}</td>
                    </tr>")) +
                     $@"
                <tr>
                <td colspan='4' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>Alt Toplam</td>
                <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{order.SubTotalAmount.ToString("C2", new CultureInfo("tr-TR"))}</td>
            </tr>
            <tr>
                <td colspan='4' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>Kargo</td>
                <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{order.ShippingAmount.Value.ToString("C2", new CultureInfo("tr-TR"))}</td>
            </tr>
            <tr>
                <td colspan='4' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>Toplam</td>
                <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{order.TotalAmount.ToString("C2", new CultureInfo("tr-TR"))}</td>
            </tr>";

            body = body
                          .Replace("{{Name}}", WebUtility.HtmlEncode(order.BillingAddress.Name))
                          .Replace("{{IdentityNumber}}", WebUtility.HtmlEncode(order.IdentityNumber))
                          .Replace("{{OrderCode}}", WebUtility.HtmlEncode(order.Code))
                          .Replace("{{CreatedAt}}", order.CreatedAt.ToString("dd/MM/yyyy HH:mm"))
                          .Replace("{{ItemsTable}}", itemsTableRows)
                          .Replace("{{BillingStreet}}", WebUtility.HtmlEncode(order.BillingAddress.Street))
                          .Replace("{{BillingDistrict}}", WebUtility.HtmlEncode(order.BillingAddress.District))
                          .Replace("{{BillingCity}}", WebUtility.HtmlEncode(order.BillingAddress.City))
                          .Replace("{{BillingPhone}}", WebUtility.HtmlEncode(order.BillingAddress.Phone))
                          .Replace("{{CompanyName}}", WebUtility.HtmlEncode(order.Corporate?.CompanyName ?? string.Empty))
                          .Replace("{{TaxOffice}}", WebUtility.HtmlEncode(order.Corporate?.TaxOffice ?? string.Empty))
                          .Replace("{{TaxNumber}}", WebUtility.HtmlEncode(order.Corporate?.TaxNumber ?? string.Empty))
                          .Replace("{{IsEInvoiceUser}}", WebUtility.HtmlEncode(eInoviceUser))
                          .Replace("{{Email}}", WebUtility.HtmlEncode(order.Email))
                          .Replace("{{ShippingName}}", WebUtility.HtmlEncode(order.ShippingAddress.Name))
                          .Replace("{{ShippingStreet}}", WebUtility.HtmlEncode(order.ShippingAddress.Street))
                          .Replace("{{ShippingDistrict}}", WebUtility.HtmlEncode(order.ShippingAddress.District))
                          .Replace("{{ShippingCity}}", WebUtility.HtmlEncode(order.ShippingAddress.City))
                          .Replace("{{ShippingPhone}}", WebUtility.HtmlEncode(order.ShippingAddress.Phone));

            const string subject = "Sipariş Onayı ✅";
            await SendInternalAsync(subject, body, order.Email, null, null, isHtml: true, false, cancellationToken);
        }

        public async Task SendVirtualPosPaymentSuccessMailAsync(OrderDto order, PaymentDto payment, CancellationToken cancellationToken)
        {
            var body = LoadTemplate("VirtualPosPaymentSuccessMailTemplate.html");
            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("VirtualPosPaymentSuccessMailTemplate.html yüklenemedi, e-posta gönderilmedi.");
                return;
            }

            body = body
                         .Replace("{{Name}}", WebUtility.HtmlEncode(order.BillingAddress.Name))
                         .Replace("{{VKN}}", WebUtility.HtmlEncode(order.IdentityNumber ?? order.Corporate?.TaxNumber))
                         .Replace("{{BillingPhone}}", WebUtility.HtmlEncode(order.BillingAddress.Phone ?? string.Empty))
                         .Replace("{{Email}}", WebUtility.HtmlEncode(order.Email ?? string.Empty))
                         .Replace("{{CardBrand}}", WebUtility.HtmlEncode(payment.BankCardBrand))
                         .Replace("{{CardIssuer}}", WebUtility.HtmlEncode(payment.BankCardIssuer))
                         .Replace("{{Amount}}", payment.TotalAmount.ToString("C2", new CultureInfo("tr-TR")))
                         .Replace("{{AmountInType}}", ConvertAmountToTurkishWords(payment.TotalAmount))
                         .Replace("{{CreatedAt}}", order.CreatedAt.ToString("dd/MM/yyyy HH:mm"))
                         .Replace("{{MaskedCreditCard}}", WebUtility.HtmlEncode(payment.MaskedCreditCard))
                         .Replace("{{OrderCode}}", WebUtility.HtmlEncode(order.Code));

            const string subject = "Ödeme Onayı ✅";
            await SendInternalAsync(subject, body, order.Email, null, null, isHtml: true, false, cancellationToken);
        }

        public async Task SendListSearchMailAsync(ListSearchDto listSearch, CancellationToken cancellationToken)
        {
            var body = LoadTemplate("ListSearchMailTemplate.html");
            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("ListSearchMailTemplate.html yüklenemedi, e-posta gönderilmedi.");
                return;
            }

            var itemsTableRows = string.Join(Environment.NewLine, listSearch.Parts.Select((part, index) =>
                     $@"
                    <tr>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: center;'>{index + 1}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: left;'>{WebUtility.HtmlEncode(part.Definition)}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: center;'>{part.Quantity}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: left;'>{WebUtility.HtmlEncode(part.Note ?? "-")}</td>
                    </tr>"));

            var redirectUrl = $"https://otomar.com.tr/liste-sorgu/cevapla/{WebUtility.UrlEncode(listSearch.RequestNo)}";

            body = body
                         .Replace("{{RequestNo}}", WebUtility.HtmlEncode(listSearch.RequestNo))
                         .Replace("{{NameSurname}}", WebUtility.HtmlEncode(listSearch.NameSurname))
                         .Replace("{{CompanyName}}", WebUtility.HtmlEncode(listSearch.CompanyName ?? string.Empty))
                         .Replace("{{PhoneNumber}}", WebUtility.HtmlEncode(listSearch.PhoneNumber))
                         .Replace("{{Email}}", WebUtility.HtmlEncode(listSearch.Email ?? string.Empty))
                         .Replace("{{ChassisNumber}}", WebUtility.HtmlEncode(listSearch.ChassisNumber))
                         .Replace("{{Brand}}", WebUtility.HtmlEncode(listSearch.Brand))
                         .Replace("{{Model}}", WebUtility.HtmlEncode(listSearch.Model))
                         .Replace("{{Year}}", WebUtility.HtmlEncode(listSearch.Year))
                         .Replace("{{Engine}}", WebUtility.HtmlEncode(listSearch.Engine ?? string.Empty))
                         .Replace("{{LicensePlate}}", WebUtility.HtmlEncode(listSearch.LicensePlate ?? string.Empty))
                         .Replace("{{Note}}", WebUtility.HtmlEncode(listSearch.Note ?? string.Empty))
                         .Replace("{{CreatedAt}}", listSearch.CreatedAt.ToString("dd/MM/yyyy HH:mm"))
                         .Replace("{{ItemsTable}}", itemsTableRows)
                         .Replace("{{RedirectUrl}}", redirectUrl);

            const string subject = "Liste Sorgu Talebi 📋";
            await SendInternalAsync(subject, body, null, null, null, isHtml: true, false, cancellationToken);
        }

        public async Task SendListSearchAnsweredMailAsync(ListSearchDto listSearch, CancellationToken cancellationToken)
        {
            var body = LoadTemplate("ListSearchAnsweredMailTemplate.html");
            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("ListSearchAnsweredMailTemplate.html yüklenemedi, e-posta gönderilmedi.");
                return;
            }

            var answeredParts = listSearch.Parts.Where(p => p.Answer != null).ToList();
            var grandTotal = answeredParts.Sum(p => p.Answer!.UnitPrice * p.Answer.Quantity);

            var answerTableRows = string.Join(Environment.NewLine, answeredParts.Select(part =>
                $@"
                    <tr>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: left;'>{WebUtility.HtmlEncode(part.Definition)}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: left;'>{WebUtility.HtmlEncode(part.Answer!.StockName ?? "-")}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: left;'>{WebUtility.HtmlEncode(part.Answer.StockCode ?? "-")}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: right;'>{part.Answer.UnitPrice.ToString("C2", new CultureInfo("tr-TR"))}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: center;'>{part.Answer.Quantity}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: center;'>{(part.Answer.KdvIncluded ? "Dahil" : "Hariç")}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{(part.Answer.UnitPrice * part.Answer.Quantity).ToString("C2", new CultureInfo("tr-TR"))}</td>
                    </tr>")) +
                $@"
                    <tr style='border-top: 2px solid #000;'>
                        <td colspan='6' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold; font-size: 15px;'>Genel Toplam</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold; font-size: 15px;'>{grandTotal.ToString("C2", new CultureInfo("tr-TR"))}</td>
                    </tr>";

            var redirectUrl = $"https://otomar.com.tr/liste-sorgu/talep-no/{WebUtility.UrlEncode(listSearch.RequestNo)}";

            body = body
                         .Replace("{{RequestNo}}", WebUtility.HtmlEncode(listSearch.RequestNo))
                         .Replace("{{NameSurname}}", WebUtility.HtmlEncode(listSearch.NameSurname))
                         .Replace("{{Brand}}", WebUtility.HtmlEncode(listSearch.Brand))
                         .Replace("{{Model}}", WebUtility.HtmlEncode(listSearch.Model))
                         .Replace("{{Year}}", WebUtility.HtmlEncode(listSearch.Year))
                         .Replace("{{LicensePlate}}", WebUtility.HtmlEncode(listSearch.LicensePlate ?? string.Empty))
                         .Replace("{{ChassisNumber}}", WebUtility.HtmlEncode(listSearch.ChassisNumber))
                         .Replace("{{AnswerTable}}", answerTableRows)
                         .Replace("{{RedirectUrl}}", redirectUrl);

            const string subject = "Liste Sorgunuz Cevaplandı ✅";
            await SendInternalAsync(subject, body, listSearch.Email, null, null, isHtml: true, false, cancellationToken);
        }

        public async Task SendClientOrderMailAsync(ClientOrderDto order, CancellationToken cancellationToken)
        {
            var body = LoadTemplate("ClientOrderSuccessMailTemplate.html");
            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("ClientOrderSuccessMailTemplate.html yüklenemedi, e-posta gönderilmedi.");
                return;
            }

            var itemsTableRows = string.Join(Environment.NewLine, order.Items.Select(item =>
                    $@"
                    <tr>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: left;'>{WebUtility.HtmlEncode(item.ProductName)}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: left;'>{WebUtility.HtmlEncode(item.ProductCode)}</td>
       <td style='padding: 10px; border: 1px solid #ccc; text-align: right;'>{item.UnitPrice.ToString("C2", new CultureInfo("tr-TR"))}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: center;'>{item.Quantity}</td>
       <td style='padding: 10px; border: 1px solid #ccc; text-align: right;'>{(item.UnitPrice * item.Quantity).ToString("C2", new CultureInfo("tr-TR"))}</td>
                    </tr>")) +
                    $@"
            <tr>
                <td colspan='4' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>Toplam</td>
                <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{order.TotalAmount.ToString("C2", new CultureInfo("tr-TR"))}</td>
            </tr>";

            body = body
                         .Replace("{{OrderNo}}", WebUtility.HtmlEncode(order.Code))
                         .Replace("{{ItemsTable}}", itemsTableRows)
                         .Replace("{{ClientName}}", WebUtility.HtmlEncode(order.ClientName))
                         .Replace("{{DocumentNo}}", WebUtility.HtmlEncode(order.DocumentNo))
                         .Replace("{{LicensePlate}}", WebUtility.HtmlEncode(order.LicensePlate))
                         .Replace("{{Note}}", WebUtility.HtmlEncode(order.Note))
                         .Replace("{{CreatedAt}}", order.CreatedAt.ToString())
                         .Replace("{{CreatedByFullName}}", WebUtility.HtmlEncode(value: order.CreatedByFullName));

            const string subject = "Cari Sipariş Oluşturuldu ✅";
            await SendInternalAsync(subject, body, null, null, null, isHtml: true, false, cancellationToken);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string name, string resetUrl, CancellationToken cancellationToken)
        {
            var body = LoadTemplate("PasswordResetMailTemplate.html");
            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("PasswordResetMailTemplate.html yüklenemedi, e-posta gönderilmedi.");
                return;
            }

            body = body
                .Replace("{{Name}}", WebUtility.HtmlEncode(name))
                .Replace("{{ResetUrl}}", resetUrl);

            const string subject = "Şifre Sıfırlama";
            await SendInternalAsync(subject, body, toEmail, cc: null, bcc: null, isHtml: true, isError: false, cancellationToken, privateOnly: true);
        }

        private string ConvertAmountToTurkishWords(decimal amount)
        {
            int wholePart = (int)amount;
            int fractionalPart = (int)((amount - wholePart) * 100);

            string wholePartInWords = wholePart.ToWords(new CultureInfo("tr-TR"));
            string fractionalPartInWords = fractionalPart > 0
                ? $"{fractionalPart.ToWords(new CultureInfo("tr-TR"))} kuruş"
                : "";

            string result = fractionalPart > 0
                ? $"{wholePartInWords} Türk lirası ve {fractionalPartInWords}"
                : $"{wholePartInWords} Türk lirası";

            return char.ToUpper(result[0]) + result.Substring(1);
        }

        public async Task SendHealthAlertAsync(string checkName, string status, string? description, string? errorMessage, CancellationToken cancellationToken)
        {
            var body = LoadTemplate("HealthAlertMailTemplate.html");
            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("HealthAlertMailTemplate.html yüklenemedi, e-posta gönderilmedi.");
                return;
            }

            bool isUnhealthy = status == "Unhealthy";
            body = body
                .Replace("{{BannerBg}}", isUnhealthy ? "#fee2e2" : "#fff7ed")
                .Replace("{{BannerBorder}}", isUnhealthy ? "#fca5a5" : "#fed7aa")
                .Replace("{{BannerText}}", isUnhealthy ? "#991b1b" : "#9a3412")
                .Replace("{{BannerTitle}}", isUnhealthy ? "Sistem bileşeni hata durumuna geçti!" : "Sistem bileşeninde performans düşüşü tespit edildi!")
                .Replace("{{BannerSubtitle}}", $"{checkName} servisi beklenmedik bir durumla karşılaştı.")
                .Replace("{{CheckName}}", WebUtility.HtmlEncode(checkName))
                .Replace("{{Status}}", WebUtility.HtmlEncode(status))
                .Replace("{{StatusColor}}", isUnhealthy ? "#b91c1c" : "#c2410c")
                .Replace("{{Description}}", WebUtility.HtmlEncode(description ?? "—"))
                .Replace("{{ErrorMessage}}", WebUtility.HtmlEncode(errorMessage ?? "—"))
                .Replace("{{Timestamp}}", DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"));

            string subject = $"[OTOMAR] {status}: {checkName} Sağlık Kontrolü";
            await SendInternalAsync(subject, body, null, cc: null, bcc: null, isHtml: true, true, cancellationToken);
        }

        private async Task SendInternalAsync(
            string subject,
            string body,
            string? to,
            string? cc,
            string? bcc,
            bool isHtml,
            bool isError,
            CancellationToken cancellationToken,
            bool privateOnly = false)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("OTOMAR Yedek Parça", emailOptions.Credentials.UserName));

            if (!isError)
            {
                if (!string.IsNullOrEmpty(to))
                {
                    message.To.Add(MailboxAddress.Parse(to));
                }
                if (!privateOnly)
                {
                    message.To.Add(address: MailboxAddress.Parse(emailOptions.Credentials.UserName));
                }
            }
            else
            {
                foreach (var err in emailOptions.ErrorTo)
                {
                    message.To.Add(MailboxAddress.Parse(err));
                }
            }

            if (!string.IsNullOrWhiteSpace(cc))
            {
                message.Cc.Add(MailboxAddress.Parse(cc));
            }

            if (!string.IsNullOrWhiteSpace(bcc))
            {
                message.Bcc.Add(MailboxAddress.Parse(bcc));
            }

            if (!privateOnly)
            {
                foreach (var requiredCc in emailOptions.RequiredCc)
                {
                    message.Cc.Add(MailboxAddress.Parse(requiredCc));
                }

                foreach (var requiredBcc in emailOptions.RequiredBcc)
                {
                    message.Bcc.Add(MailboxAddress.Parse(requiredBcc));
                }
            }

            message.Subject = subject;

            var builder = new BodyBuilder();
            if (isHtml)
            {
                builder.HtmlBody = body;
            }
            else
            {
                builder.TextBody = body;
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            var secureOption = emailOptions.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(emailOptions.Host, emailOptions.Port, secureOption, cancellationToken);
            await client.AuthenticateAsync(emailOptions.Credentials.UserName, emailOptions.Credentials.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            logger.LogInformation("E-posta gönderildi. Konu: {Subject}, Alıcı: {To}", subject, to);
        }

        private string LoadTemplate(string templateFileName)
        {
            try
            {
                var baseDirectory = AppContext.BaseDirectory;
                var fullPath = Path.Combine(baseDirectory, "MailTemplates", templateFileName);

                if (!File.Exists(fullPath))
                {
                    logger.LogError("Mail template dosyası bulunamadı. Path: {Path}", fullPath);
                    return string.Empty;
                }

                return File.ReadAllText(fullPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Mail template okunurken hata oluştu. Template: {Template}", templateFileName);
                return string.Empty;
            }
        }
    }
}