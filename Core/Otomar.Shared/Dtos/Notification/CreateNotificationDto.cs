using Otomar.Shared.Enums;

namespace Otomar.Shared.Dtos.Notification
{
    public class CreateNotificationDto
    {
        public string Title { get; set; } = default!;
        public string Message { get; set; } = default!;
        public NotificationType Type { get; set; }
        public string? RedirectUrl { get; set; }
        /// <summary>
        /// Belirli bir kullanıcıya bildirim göndermek için.
        /// </summary>
        public string? TargetUserId { get; set; }
        /// <summary>
        /// Bir roldeki tüm kullanıcılara bildirim göndermek için (ör. "Admin").
        /// </summary>
        public string? TargetRoleName { get; set; }
    }
}
