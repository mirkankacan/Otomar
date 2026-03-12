namespace Otomar.Shared.Dtos.Auth
{
    public class CreateTokenByRefreshTokenDto
    {
        public string RefreshToken { get; set; } = default!;
    }
}
