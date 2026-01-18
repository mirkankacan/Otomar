namespace Otomar.Application.Dtos.Auth
{
    public class RegisterDto
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public bool IsCommerceMailAccepted { get; set; }
        public bool IsClarificationAgreementAccepted { get; set; }
        public bool IsMembershipAgreementAccepted { get; set; }
    }
}