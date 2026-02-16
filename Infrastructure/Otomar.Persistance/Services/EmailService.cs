using Humanizer;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using Otomar.Application.Contracts.Services;
using Otomar.Persistance.Options;
using System.Globalization;
using System.Net;

namespace Otomar.Persistance.Services
{
    public class EmailService(ILogger<EmailService> logger, EmailOptions emailOptions, IIdentityService identityService, IServiceProvider serviceProvider) : IEmailService
    {
        public async Task SendPaymentFailedMailAsync(string orderCode, CancellationToken cancellationToken)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
            var payment = await paymentService.GetPaymentByOrderCodeAsync(orderCode);

            if (payment.Data == null)
            {
                logger.LogWarning($"{orderCode} sipariş kodlu ödeme bulunamadı");
                return;
            }

            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var order = await orderService.GetOrderByCodeAsync(orderCode);

            if (order.Data == null)
            {
                logger.LogWarning($"{orderCode} sipariş kodlu sipariş bulunamadı");
                return;
            }

            var body = LoadTemplate("PaymentFailedMailTemplate.html");
            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("PaymentFailedMailTemplate.html yüklenemedi, e-posta gönderilmedi.");
                return;
            }

            var itemsTableRows = string.Join(Environment.NewLine, order.Data.Items.Select(item =>
                     $@"
                    <tr>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: left;'>{WebUtility.HtmlEncode(item.ProductName)}</td>
       <td style='padding: 10px; border: 1px solid #ccc; text-align: right;'>{item.UnitPrice.ToString("C2", new CultureInfo("tr-TR"))}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: center;'>{item.Quantity}</td>
       <td style='padding: 10px; border: 1px solid #ccc; text-align: right;'>{(item.UnitPrice * item.Quantity).ToString("C2", new CultureInfo("tr-TR"))}</td>
                    </tr>")) +
                     $@"
                <tr>
                <td colspan='4' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>Alt Toplam</td>
                <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{order.Data.SubTotalAmount.ToString("C2", new CultureInfo("tr-TR"))}</td>
            </tr>
            <tr>
                <td colspan='4' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>Kargo</td>
                <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{order.Data.ShippingAmount.Value.ToString("C2", new CultureInfo("tr-TR"))}</td>
            </tr>
            <tr>
                <td colspan='4' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>Toplam</td>
                <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{order.Data.TotalAmount.ToString("C2", new CultureInfo("tr-TR"))}</td>
            </tr>";

            body = body
                         .Replace("{{OrderCode}}", WebUtility.HtmlEncode(payment.Data.OrderCode))
                         .Replace("{{Amount}}", payment.Data.TotalAmount.ToString("C2", new CultureInfo("tr-TR")))
                         .Replace("{{CreatedAt}}", payment.Data.CreatedAt.ToString("dd/MM/yyyy HH:mm"))
                         .Replace("{{MaskedCreditCard}}", WebUtility.HtmlEncode(payment.Data.MaskedCreditCard))
                         .Replace("{{CardBrand}}", WebUtility.HtmlEncode(payment.Data.BankCardBrand))
                         .Replace("{{CardIssuer}}", WebUtility.HtmlEncode(payment.Data.BankCardIssuer))
                         .Replace("{{BankErrorCode}}", WebUtility.HtmlEncode(payment.Data.BankErrorCode ?? "-"))
                         .Replace("{{BankErrMsg}}", WebUtility.HtmlEncode(payment.Data.BankErrMsg ?? "-"))
                         .Replace("{{BankProcReturnCode}}", WebUtility.HtmlEncode(payment.Data.BankProcReturnCode))
                         .Replace("{{Name}}", WebUtility.HtmlEncode(order.Data.BillingAddress.Name))
                         .Replace("{{IdentityNumber}}", WebUtility.HtmlEncode(order.Data.IdentityNumber))
                         .Replace("{{ItemsTable}}", itemsTableRows)
                         .Replace("{{BillingStreet}}", WebUtility.HtmlEncode(order.Data.BillingAddress.Street))
                         .Replace("{{BillingDistrict}}", WebUtility.HtmlEncode(order.Data.BillingAddress.District))
                         .Replace("{{BillingCity}}", WebUtility.HtmlEncode(order.Data.BillingAddress.City))
                         .Replace("{{BillingPhone}}", WebUtility.HtmlEncode(order.Data.BillingAddress.Phone))
                         .Replace("{{CompanyName}}", WebUtility.HtmlEncode(order.Data.Corporate?.CompanyName ?? string.Empty))
                         .Replace("{{TaxOffice}}", WebUtility.HtmlEncode(order.Data.Corporate?.TaxOffice ?? string.Empty))
                         .Replace("{{TaxNumber}}", WebUtility.HtmlEncode(order.Data.Corporate?.TaxNumber ?? string.Empty))
                         .Replace("{{IsEInvoiceUser}}", (order.Data.Corporate?.IsEInvoiceUser ?? false) ? "Evet" : "Hayır")
                         .Replace("{{Email}}", WebUtility.HtmlEncode(order.Data.Email))
                         .Replace("{{ShippingName}}", WebUtility.HtmlEncode(order.Data.ShippingAddress.Name))
                         .Replace("{{ShippingStreet}}", WebUtility.HtmlEncode(order.Data.ShippingAddress.Street))
                         .Replace("{{ShippingDistrict}}", WebUtility.HtmlEncode(order.Data.ShippingAddress.District))
                         .Replace("{{ShippingCity}}", WebUtility.HtmlEncode(order.Data.ShippingAddress.City))
                         .Replace("{{ShippingPhone}}", WebUtility.HtmlEncode(order.Data.ShippingAddress.Phone));

            const string subject = "Ödemede Hata ❌";
            await SendInternalAsync(subject, body, null, null, null, isHtml: true, cancellationToken);
        }

        public async Task SendPaymentSuccessMailAsync(Guid orderId, CancellationToken cancellationToken)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var order = await orderService.GetOrderByIdAsync(orderId);
            if (order.Data == null)
            {
                logger.LogWarning($"{orderId} ID'li sipariş bulunamadı");
                return;
            }
            var body = LoadTemplate("OrderSuccessMailTemplate.html");

            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("OrderSuccessMailTemplate.html yüklenemedi, e-posta gönderilmedi.");
                return;
            }
            var itemsTableRows = string.Join(Environment.NewLine, order.Data.Items.Select(item =>
                     $@"
                    <tr>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: left;'>{WebUtility.HtmlEncode(item.ProductName)}</td>
       <td style='padding: 10px; border: 1px solid #ccc; text-align: right;'>{item.UnitPrice.ToString("C2", new CultureInfo("tr-TR"))}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: center;'>{item.Quantity}</td>
       <td style='padding: 10px; border: 1px solid #ccc; text-align: right;'>{(item.UnitPrice * item.Quantity).ToString("C2", new CultureInfo("tr-TR"))}</td>
                    </tr>")) +
                     $@"
                <tr>
                <td colspan='4' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>Alt Toplam</td>
                <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{order.Data.SubTotalAmount.ToString("C2", new CultureInfo("tr-TR"))}</td>
            </tr>
            <tr>
                <td colspan='4' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>Kargo</td>
                <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{order.Data.ShippingAmount.Value.ToString("C2", new CultureInfo("tr-TR"))}</td>
            </tr>
            <tr>
                <td colspan='4' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>Toplam</td>
                <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{order.Data.TotalAmount.ToString("C2", new CultureInfo("tr-TR"))}</td>
            </tr>";
            body = body
                          .Replace("{{Name}}", WebUtility.HtmlEncode(order.Data.BillingAddress.Name))
                          .Replace("{{IdentityNumber}}", WebUtility.HtmlEncode(order.Data.IdentityNumber))
                          .Replace("{{OrderCode}}", WebUtility.HtmlEncode(order.Data.Code))
                          .Replace("{{CreatedAt}}", order.Data.CreatedAt.ToString("dd/MM/yyyy HH:mm"))
                          .Replace("{{ItemsTable}}", itemsTableRows)
                          .Replace("{{BillingStreet}}", WebUtility.HtmlEncode(order.Data.BillingAddress.Street))
                          .Replace("{{BillingDistrict}}", WebUtility.HtmlEncode(order.Data.BillingAddress.District))
                          .Replace("{{BillingCity}}", WebUtility.HtmlEncode(order.Data.BillingAddress.City))
                          .Replace("{{BillingPhone}}", WebUtility.HtmlEncode(order.Data.BillingAddress.Phone))
                          .Replace("{{CompanyName}}", WebUtility.HtmlEncode(order.Data.Corporate?.CompanyName ?? string.Empty))
                          .Replace("{{TaxOffice}}", WebUtility.HtmlEncode(order.Data.Corporate?.TaxOffice ?? string.Empty))
                          .Replace("{{TaxNumber}}", WebUtility.HtmlEncode(order.Data.Corporate?.TaxNumber ?? string.Empty))
                            .Replace("{{IsEInvoiceUser}}", (order.Data.Corporate?.IsEInvoiceUser ?? false) ? "Evet" : "Hayır")
                            .Replace("{{Email}}", WebUtility.HtmlEncode(order.Data.Email))
                          .Replace("{{ShippingName}}", WebUtility.HtmlEncode(order.Data.ShippingAddress.Name))
                          .Replace("{{ShippingStreet}}", WebUtility.HtmlEncode(order.Data.ShippingAddress.Street))
                          .Replace("{{ShippingDistrict}}", WebUtility.HtmlEncode(order.Data.ShippingAddress.District))
                          .Replace("{{ShippingCity}}", WebUtility.HtmlEncode(order.Data.ShippingAddress.City))
                          .Replace("{{ShippingPhone}}", WebUtility.HtmlEncode(order.Data.ShippingAddress.Phone));
            const string subject = "Sipariş Onayı ✅";
            await SendInternalAsync(subject, body, order.Data.Email, null, null, isHtml: true, cancellationToken);
        }

        public async Task SendVirtualPosPaymentSuccessMailAsync(Guid orderId, CancellationToken cancellationToken)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var order = await orderService.GetOrderByIdAsync(orderId);
            if (order.Data == null)
            {
                logger.LogWarning($"{orderId} ID'li sipariş bulunamadı");
                return;
            }
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
            var payment = await paymentService.GetPaymentByOrderCodeAsync(order.Data.Code);

            if (payment.Data == null)
            {
                logger.LogWarning($"{order.Data.Code} sipariş kodlu ödeme bulunamadı");
                return;
            }
            var body = LoadTemplate("VirtualPosPaymentSuccessMailTemplate.html");
            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("VirtualPosPaymentSuccessMailTemplate.html yüklenemedi, e-posta gönderilmedi.");
                return;
            }
            body = body
                         .Replace("{{Name}}", WebUtility.HtmlEncode(order.Data.BillingAddress.Name))
                         .Replace("{{BillingPhone}}", WebUtility.HtmlEncode(order.Data.BillingAddress.Phone ?? string.Empty))
                         .Replace("{{Email}}", WebUtility.HtmlEncode(order.Data.Email ?? string.Empty))
                         .Replace("{{CardBrand}}", WebUtility.HtmlEncode(payment.Data.BankCardBrand))
                         .Replace("{{CardIssuer}}", WebUtility.HtmlEncode(payment.Data.BankCardIssuer))
                         .Replace("{{Amount}}", payment.Data.TotalAmount.ToString("C2", new CultureInfo("tr-TR")))
                         .Replace("{{AmountInType}}", ConvertAmountToTurkishWords(payment.Data.TotalAmount))
                         .Replace("{{CreatedAt}}", order.Data.CreatedAt.ToString("dd/MM/yyyy HH:mm"))
                         .Replace("{{MaskedCreditCard}}", WebUtility.HtmlEncode(payment.Data.MaskedCreditCard))
                         .Replace("{{OrderCode}}", WebUtility.HtmlEncode(order.Data.Code));
            const string subject = "Ödeme Onayı ✅";
            await SendInternalAsync(subject, body, order.Data.Email, null, null, isHtml: true, cancellationToken);
        }

        public async Task SendListSearchMailAsync(Guid listSearchId, CancellationToken cancellationToken)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var listSearchService = scope.ServiceProvider.GetRequiredService<IListSearchService>();
            var listSearch = await listSearchService.GetListSearchByIdAsync(listSearchId);
            if (listSearch.Data == null)
            {
                logger.LogWarning($"{listSearchId} ID'li liste sorgusu bulunamadı");
                return;
            }

            var body = LoadTemplate("ListSearchMailTemplate.html");
            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("ListSearchMailTemplate.html yüklenemedi, e-posta gönderilmedi.");
                return;
            }
            //body = body
            //             .Replace("{{Name}}", WebUtility.HtmlEncode(order.Data.BillingAddress.Name))
            //             .Replace("{{BillingPhone}}", WebUtility.HtmlEncode(order.Data.BillingAddress.Phone ?? string.Empty))
            //             .Replace("{{Email}}", WebUtility.HtmlEncode(order.Data.Email ?? string.Empty))
            //             .Replace("{{CardBrand}}", WebUtility.HtmlEncode(payment.Data.BankCardBrand))
            //             .Replace("{{CardIssuer}}", WebUtility.HtmlEncode(payment.Data.BankCardIssuer))
            //             .Replace("{{Amount}}", payment.Data.TotalAmount.ToString("C2", new CultureInfo("tr-TR"))
            //             .Replace("{{AmountInType}}", ConvertAmountToTurkishWords(payment.Data.TotalAmount))
            //             .Replace("{{CreatedAt}}", order.Data.CreatedAt.ToString("dd/MM/yyyy HH:mm"))
            //             .Replace("{{OrderCode}}", WebUtility.HtmlEncode(order.Data.Code)));
            //const string subject = "Liste Sorgu ❓";
            //await SendInternalAsync(subject, body, order.Data.Email, null, null, isHtml: true, cancellationToken);
        }

        public async Task SendClientOrderMailAsync(Guid orderId, CancellationToken cancellationToken)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var order = await orderService.GetClientOrderByIdAsync(id: orderId);
            if (order.Data == null)
            {
                logger.LogWarning($"{orderId} ID'li cari siparişi bulunamadı");
                return;
            }

            var body = LoadTemplate("ClientOrderSuccessMailTemplate.html");
            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("ClientOrderSuccessMailTemplate.html yüklenemedi, e-posta gönderilmedi.");
                return;
            }
            var itemsTableRows = string.Join(Environment.NewLine, order.Data.Items.Select(item =>
                    $@"
                    <tr>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: left;'>{WebUtility.HtmlEncode(item.ProductName)}</td>
       <td style='padding: 10px; border: 1px solid #ccc; text-align: right;'>{item.UnitPrice.ToString("C2", new CultureInfo("tr-TR"))}</td>
                        <td style='padding: 10px; border: 1px solid #ccc; text-align: center;'>{item.Quantity}</td>
       <td style='padding: 10px; border: 1px solid #ccc; text-align: right;'>{(item.UnitPrice * item.Quantity).ToString("C2", new CultureInfo("tr-TR"))}</td>
                    </tr>")) +
                    $@"
            <tr>
                <td colspan='3' style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>Toplam</td>
                <td style='padding: 10px; border: 1px solid #ccc; text-align: right; font-weight: bold;'>{order.Data.TotalAmount.ToString("C2", new CultureInfo("tr-TR"))}</td>
            </tr>";
            body = body
                         .Replace("{{OrderNo}}", WebUtility.HtmlEncode(order.Data.Code))
                         .Replace("{{ItemsTable}}", itemsTableRows)
                         .Replace("{{ClientName}}", WebUtility.HtmlEncode(order.Data.ClientName))
                         .Replace("{{DocumentNo}}", WebUtility.HtmlEncode(order.Data.DocumentNo))
                         .Replace("{{LicensePlate}}", WebUtility.HtmlEncode(order.Data.LicensePlate))
                         .Replace("{{Note}}", WebUtility.HtmlEncode(order.Data.Note))
                         .Replace("{{CreatedAt}}", order.Data.CreatedAt.ToString())
                          .Replace("{{CreatedByFullName}}", WebUtility.HtmlEncode(value: order.Data.CreatedByFullName));

            const string subject = "Cari Sipariş Oluşturuldu ✅";
            await SendInternalAsync(subject, body, null, null, null, isHtml: true, cancellationToken);
        }

        private string ConvertAmountToTurkishWords(decimal amount)
        {
            // Tam sayı kısmını al
            int wholePart = (int)amount;

            // Kuruş kısmını al
            int fractionalPart = (int)((amount - wholePart) * 100);

            // Sayıları Türkçe yazıya çevir
            string wholePartInWords = wholePart.ToWords(new CultureInfo("tr-TR"));
            string fractionalPartInWords = fractionalPart > 0
                ? $"{fractionalPart.ToWords(new CultureInfo("tr-TR"))} kuruş"
                : "";

            // Sonuç oluştur
            string result = fractionalPart > 0
                ? $"{wholePartInWords} Türk lirası ve {fractionalPartInWords}"
                : $"{wholePartInWords} Türk lirası";

            // İlk harfi büyük yap
            return char.ToUpper(result[0]) + result.Substring(1);
        }

        private async Task SendInternalAsync(
            string subject,
            string body,
            string? to,
            string? cc,
            string? bcc,
            bool isHtml,
            CancellationToken cancellationToken)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(emailOptions.Credentials.UserName));

            if (!string.IsNullOrEmpty(to))
            {
                message.To.Add(MailboxAddress.Parse(to));
            }
            else
            {
                message.To.Add(MailboxAddress.Parse(emailOptions.Credentials.UserName));
            }

            // Optional CC/BCC from method parameters
            if (!string.IsNullOrWhiteSpace(cc))
            {
                message.Cc.Add(MailboxAddress.Parse(cc));
            }

            if (!string.IsNullOrWhiteSpace(bcc))
            {
                message.Bcc.Add(MailboxAddress.Parse(bcc));
            }

            // Required CC/BCC from configuration
            foreach (var requiredCc in emailOptions.RequiredCc)
            {
                message.Cc.Add(MailboxAddress.Parse(requiredCc));
            }

            foreach (var requiredBcc in emailOptions.RequiredBcc)
            {
                message.Bcc.Add(MailboxAddress.Parse(requiredBcc));
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