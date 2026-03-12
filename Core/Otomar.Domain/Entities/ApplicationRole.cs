using Microsoft.AspNetCore.Identity;

namespace Otomar.Domain.Entities
{
    public class ApplicationRole : IdentityRole<string>
    {
        public ApplicationRole()
        {
            Id = Guid.NewGuid().ToString().ToUpper();
            IsActive = true;
        }

        public bool IsActive { get; set; }
    }
}