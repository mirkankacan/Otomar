namespace Otomar.Application.Dtos.Auth
{
    public class TokenDto
    {
        public string Token { get; set; }
        public DateTime TokenExpires { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpires { get; set; }
        public string FullName { get; set; }
        public string RoleName { get; set; }
        public string? ClientCode { get; set; }
        public string PhoneNumber { get; set; }
    }
}