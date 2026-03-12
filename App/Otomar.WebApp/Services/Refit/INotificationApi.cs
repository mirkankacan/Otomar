using Otomar.Shared.Common;
using Otomar.Shared.Dtos.Notification;
using Refit;

namespace Otomar.WebApp.Services.Refit
{
    public interface INotificationApi
    {
        [Post("/api/notifications")]
        Task<List<NotificationDto>> CreateNotificationAsync([Body] CreateNotificationDto dto, CancellationToken cancellationToken = default);

        [Get("/api/notifications/user/{userId}")]
        Task<PagedResult<NotificationDto>> GetNotificationsByUserAsync(string userId, [Query] int pageNumber, [Query] int pageSize, CancellationToken cancellationToken = default);

        [Get("/api/notifications/unread-count/{userId}")]
        Task<UnreadCountDto> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

        [Put("/api/notifications/{id}/read")]
        Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);

        [Put("/api/notifications/read-all")]
        Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);
    }
}
