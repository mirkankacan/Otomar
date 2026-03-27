namespace Otomar.Shared.Dtos.Auth
{
    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string? RecaptchaToken { get; set; }
    }
}
