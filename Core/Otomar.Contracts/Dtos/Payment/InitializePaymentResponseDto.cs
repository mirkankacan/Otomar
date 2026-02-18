namespace Otomar.Contracts.Dtos.Payment
{
    public class InitializePaymentResponseDto
    {
        public Dictionary<string, string> Parameters { get; set; }
        public string ThreeDVerificationUrl { get; set; }
    }
}
