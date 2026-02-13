using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Otomar.WebApp.Options;
using Otomar.WebApp.Services;

namespace Otomar.WebApp.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ValidateRecaptchaAttribute(string expectedAction) : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var options = context.HttpContext.RequestServices.GetRequiredService<RecaptchaOptions>();

            if (!options.Enabled)
            {
                await next();
                return;
            }

            var recaptchaService = context.HttpContext.RequestServices.GetRequiredService<RecaptchaService>();

            // Token'ı al: FormData veya JSON body
            string? token = null;

            if (context.HttpContext.Request.HasFormContentType)
            {
                token = context.HttpContext.Request.Form["recaptchaToken"].ToString();
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                foreach (var arg in context.ActionArguments.Values)
                {
                    var prop = arg?.GetType().GetProperty("RecaptchaToken");
                    if (prop != null)
                    {
                        token = prop.GetValue(arg)?.ToString();
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                context.Result = new BadRequestObjectResult(new
                {
                    title = "Güvenlik Doğrulaması",
                    detail = "reCAPTCHA doğrulaması başarısız. Lütfen sayfayı yenileyip tekrar deneyin."
                });
                return;
            }

            var remoteIp = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            var isValid = await recaptchaService.VerifyAsync(token, expectedAction, remoteIp);

            if (!isValid)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    title = "Güvenlik Doğrulaması Başarısız",
                    detail = "İstek şüpheli olarak değerlendirildi. Lütfen daha sonra tekrar deneyin."
                });
                return;
            }

            await next();
        }
    }
}
