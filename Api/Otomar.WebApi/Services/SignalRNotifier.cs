using Microsoft.AspNetCore.SignalR;
using Otomar.Application.Contracts.Services;
using Otomar.Shared.Dtos.Notification;
using Otomar.WebApi.Hubs;

namespace Otomar.WebApi.Services
{
    public class SignalRNotifier(IHubContext<NotificationHub> hubContext) : IRealtimeNotifier
    {
        public async Task SendNotificationsAsync(List<NotificationDto> notifications)
        {
            foreach (var notification in notifications)
            {
                await hubContext.Clients.Group($"user-{notification.UserId}")
                    .SendAsync("ReceiveNotification", notification);
            }
        }
    }
}
