namespace Otomar.Shared.Dtos.User
{
    public class UpdateUserProfileDto
    {
        public string Name { get; set; }
        public string? Surname { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
