using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Extensions;
using Otomar.Shared.Interfaces;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [Authorize]
    [Route("bildirimler")]
    public class NotificationController(INotificationApi notificationApi, IIdentityService identityService) : Controller
    {
        [HttpGet("listele")]
        public async Task<IActionResult> GetNotifications([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var userId = identityService.GetUserId();
            return await notificationApi.GetNotificationsByUserAsync(userId, pageNumber, pageSize, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("okunmamis-sayi")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
        {
            var userId = identityService.GetUserId();
            return await notificationApi.GetUnreadCountAsync(userId, cancellationToken).ToActionResultAsync();
        }

        [HttpPut("{id:guid}/oku")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken = default)
        {
            return await notificationApi.MarkAsReadAsync(id, cancellationToken).ToActionResultAsync();
        }

        [HttpPut("tumu-oku")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
        {
            return await notificationApi.MarkAllAsReadAsync(cancellationToken).ToActionResultAsync();
        }
    }
}
