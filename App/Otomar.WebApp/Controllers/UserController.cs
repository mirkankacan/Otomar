using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Otomar.Shared.Dtos.User;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [Authorize]
    [Route("")]
    public class UserController(IUserApi userApi) : Controller
    {
        [HttpGet("hesabim")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("profilim")]
        public IActionResult Profile()
        {
            return View();
        }

        [HttpGet("kullanici/profil")]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        {
            return await userApi.GetProfileAsync(cancellationToken).ToActionResultAsync();
        }

        [HttpPut("kullanici/profil")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto, CancellationToken cancellationToken)
        {
            return await userApi.UpdateProfileAsync(dto, cancellationToken).ToActionResultAsync();
        }

        [HttpPost("kullanici/sifre-degistir")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
        {
            return await userApi.ChangePasswordAsync(dto, cancellationToken).ToActionResultAsync();
        }
    }
}
