namespace Otomar.Application.Dtos.Auth
{
    public class CreateTokenByRefreshTokenDto
    {
        public string RefreshToken { get; set; } = default!;
    }
}
