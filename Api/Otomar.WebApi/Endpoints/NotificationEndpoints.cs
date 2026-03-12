using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
using Otomar.Shared.Dtos.Notification;
using Otomar.WebApi.Extensions;

namespace Otomar.WebApi.Endpoints
{
    public class NotificationEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/notifications")
                .WithTags("Notifications")
                .RequireAuthorization();

            group.MapPost("/", async (
                [FromBody] CreateNotificationDto dto,
                [FromServices] INotificationService notificationService) =>
            {
                var result = await notificationService.CreateNotificationAsync(dto);
                return result.ToGenericResult();
            })
            .WithName("CreateNotification");

            group.MapGet("/user/{userId}", async (
                string userId,
                [FromServices] INotificationService notificationService,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 20) =>
            {
                var result = await notificationService.GetNotificationsByUserAsync(userId, pageNumber, pageSize);
                return result.ToGenericResult();
            })
            .WithName("GetNotificationsByUser");

            group.MapGet("/unread-count/{userId}", async (
                string userId,
                [FromServices] INotificationService notificationService) =>
            {
                var result = await notificationService.GetUnreadCountAsync(userId);
                return result.ToGenericResult();
            })
            .WithName("GetUnreadCount");

            group.MapPut("/{id:guid}/read", async (
                Guid id,
                [FromServices] INotificationService notificationService,
                [FromServices] IIdentityService identityService) =>
            {
                var userId = identityService.GetUserId();
                var result = await notificationService.MarkAsReadAsync(id, userId);
                return result.ToResult();
            })
            .WithName("MarkNotificationAsRead");

            group.MapPut("/read-all", async (
                [FromServices] INotificationService notificationService,
                [FromServices] IIdentityService identityService) =>
            {
                var userId = identityService.GetUserId();
                var result = await notificationService.MarkAllAsReadAsync(userId);
                return result.ToResult();
            })
            .WithName("MarkAllNotificationsAsRead");
        }
    }
}
