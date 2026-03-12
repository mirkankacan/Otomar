using Microsoft.AspNetCore.Identity;

namespace Otomar.Domain.Entities
{
    public class ApplicationUser : IdentityUser<string>
    {
        public ApplicationUser()
        {
            Id = Guid.NewGuid().ToString().ToUpper();
            IsActive = true;
            IsPaymentExempt = false;
        }

        public string Name { get; set; }
        public string? Surname { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpires { get; set; }
        public string? ClientCode { get; set; }
        public string? NoHashPassword { get; set; }
        public bool IsPaymentExempt { get; set; }
        public bool CommerceMail { get; set; }
        public bool ClarificationAgreement { get; set; }
        public bool MembershipAgreement { get; set; }
        public virtual ICollection<UserGlobalFilter>? UserGlobalFilters { get; set; }
    }
}