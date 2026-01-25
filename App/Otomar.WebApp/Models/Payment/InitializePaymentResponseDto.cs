namespace Otomar.WebApp.Models.Payment
{
    public class InitializePaymentResponseDto
    {
        public Dictionary<string, string> Parameters { get; set; }
        public string ThreeDVerificationUrl { get; set; }
    }
}