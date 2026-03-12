using Otomar.Shared.Common;
using Otomar.Shared.Dtos.Notification;

namespace Otomar.Application.Contracts.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Bildirim oluşturur. TargetRoleName set ise o roldeki tüm kullanıcılara, TargetUserId set ise belirli kullanıcıya gönderir.
        /// </summary>
        Task<ServiceResult<List<NotificationDto>>> CreateNotificationAsync(CreateNotificationDto dto);

        /// <summary>
        /// Kullanıcının bildirimlerini sayfalı olarak getirir.
        /// </summary>
        Task<ServiceResult<PagedResult<NotificationDto>>> GetNotificationsByUserAsync(string userId, int pageNumber, int pageSize);

        /// <summary>
        /// Kullanıcının okunmamış bildirim sayısını döner.
        /// </summary>
        Task<ServiceResult<UnreadCountDto>> GetUnreadCountAsync(string userId);

        /// <summary>
        /// Tek bir bildirimi okundu olarak işaretler.
        /// </summary>
        Task<ServiceResult> MarkAsReadAsync(Guid notificationId, string userId);

        /// <summary>
        /// Kullanıcının tüm bildirimlerini okundu olarak işaretler.
        /// </summary>
        Task<ServiceResult> MarkAllAsReadAsync(string userId);
    }
}
