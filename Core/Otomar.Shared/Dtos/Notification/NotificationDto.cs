using Otomar.Shared.Enums;

namespace Otomar.Shared.Dtos.Notification
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Message { get; set; } = default!;
        public NotificationType Type { get; set; }
        public string? RedirectUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
