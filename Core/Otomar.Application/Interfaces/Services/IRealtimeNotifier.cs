using Otomar.Shared.Dtos.Notification;

namespace Otomar.Application.Interfaces.Services
{
    /// <summary>
    /// Gerçek zamanlı bildirim gönderme soyutlaması. Infrastructure katmanını SignalR'a bağımlı kılmaz.
    /// </summary>
    public interface IRealtimeNotifier
    {
        /// <summary>
        /// Bildirimleri ilgili kullanıcılara anlık olarak gönderir.
        /// </summary>
        Task SendNotificationsAsync(List<NotificationDto> notifications);
    }
}
