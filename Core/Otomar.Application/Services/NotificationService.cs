using MassTransit;
using Microsoft.Extensions.Logging;
using Otomar.Application.Interfaces.Repositories;
using Otomar.Application.Interfaces.Services;
using Otomar.Shared.Interfaces;
using Otomar.Shared.Common;
using Otomar.Shared.Dtos.Notification;
using System.Net;

namespace Otomar.Application.Services
{
    public class NotificationService(ILogger<NotificationService> logger, INotificationRepository notificationRepository, IIdentityService identityService, IRealtimeNotifier realtimeNotifier) : INotificationService
    {
        public async Task<ServiceResult<List<NotificationDto>>> CreateNotificationAsync(CreateNotificationDto dto)
        {
            var createdBy = identityService.GetUserId();
            var notifications = new List<NotificationDto>();

            if (string.IsNullOrEmpty(dto.TargetUserId) && string.IsNullOrEmpty(dto.TargetRoleName))
            {
                return ServiceResult<List<NotificationDto>>.Error("Hedef Belirtilmedi", "TargetUserId veya TargetRoleName belirtilmelidir.", HttpStatusCode.BadRequest);
            }

            var targetUserIds = new List<string>();

            if (!string.IsNullOrEmpty(dto.TargetRoleName))
            {
                var userIds = await notificationRepository.GetUserIdsByRoleAsync(dto.TargetRoleName.ToUpperInvariant());
                targetUserIds.AddRange(userIds);
            }
            else
            {
                targetUserIds.Add(dto.TargetUserId!);
            }

            if (!targetUserIds.Any())
            {
                logger.LogWarning("Bildirim gönderilecek kullanıcı bulunamadı. TargetRoleName: {TargetRoleName}, TargetUserId: {TargetUserId}",
                    dto.TargetRoleName, dto.TargetUserId);
                return ServiceResult<List<NotificationDto>>.SuccessAsCreated(notifications, string.Empty);
            }

            var now = DateTime.Now;

            foreach (var userId in targetUserIds)
            {
                var notificationId = NewId.NextGuid();

                await notificationRepository.InsertNotificationAsync(
                    notificationId, userId, dto.Title, dto.Message, dto.Type, dto.RedirectUrl, now, createdBy);

                notifications.Add(new NotificationDto
                {
                    Id = notificationId,
                    UserId = userId,
                    Title = dto.Title,
                    Message = dto.Message,
                    Type = dto.Type,
                    RedirectUrl = dto.RedirectUrl,
                    IsRead = false,
                    CreatedAt = now
                });
            }

            logger.LogInformation("Bildirim oluşturuldu. Hedef: {Target}, Tip: {Type}, Adet: {Count}",
                dto.TargetRoleName ?? dto.TargetUserId, dto.Type, notifications.Count);

            // DB insert sonrası anlık push (aynı scope içinde)
            try
            {
                await realtimeNotifier.SendNotificationsAsync(notifications);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Anlık bildirim push başarısız oldu ama DB kaydı oluşturuldu");
            }

            return ServiceResult<List<NotificationDto>>.SuccessAsCreated(notifications, string.Empty);
        }

        public async Task<ServiceResult<PagedResult<NotificationDto>>> GetNotificationsByUserAsync(string userId, int pageNumber, int pageSize)
        {
            var offset = (pageNumber - 1) * pageSize;
            var (notifications, totalCount) = await notificationRepository.GetNotificationsByUserAsync(userId, offset, pageSize);

            var pagedResult = new PagedResult<NotificationDto>(notifications, pageNumber, pageSize, totalCount);
            return ServiceResult<PagedResult<NotificationDto>>.SuccessAsOk(pagedResult);
        }

        public async Task<ServiceResult<UnreadCountDto>> GetUnreadCountAsync(string userId)
        {
            var count = await notificationRepository.GetUnreadCountAsync(userId);
            return ServiceResult<UnreadCountDto>.SuccessAsOk(new UnreadCountDto { Count = count });
        }

        public async Task<ServiceResult> MarkAsReadAsync(Guid notificationId, string userId)
        {
            var affected = await notificationRepository.MarkAsReadAsync(notificationId, userId, DateTime.Now);

            if (affected == 0)
            {
                return ServiceResult.ErrorAsNotFound();
            }

            return ServiceResult.SuccessAsNoContent();
        }

        public async Task<ServiceResult> MarkAllAsReadAsync(string userId)
        {
            await notificationRepository.MarkAllAsReadAsync(userId, DateTime.Now);
            return ServiceResult.SuccessAsNoContent();
        }
    }
}
