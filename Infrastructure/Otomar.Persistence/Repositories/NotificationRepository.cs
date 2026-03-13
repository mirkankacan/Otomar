using Dapper;
using Otomar.Application.Interfaces;
using Otomar.Application.Interfaces.Repositories;
using Otomar.Shared.Dtos.Notification;
using Otomar.Shared.Enums;

namespace Otomar.Persistence.Repositories
{
    /// <summary>
    /// Provides Dapper-based data access for notification records.
    /// </summary>
    public class NotificationRepository : INotificationRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetUserIdsByRoleAsync(string normalizedRoleName)
        {
            var query = @"
                SELECT u.Id
                FROM AspNetUsers u WITH (NOLOCK)
                INNER JOIN AspNetUserRoles ur WITH (NOLOCK) ON u.Id = ur.UserId
                INNER JOIN AspNetRoles r WITH (NOLOCK) ON ur.RoleId = r.Id
                WHERE r.NormalizedName = @NormalizedRoleName;";

            return await _unitOfWork.Connection.QueryAsync<string>(query, new { NormalizedRoleName = normalizedRoleName });
        }

        /// <inheritdoc />
        public async Task InsertNotificationAsync(Guid id, string userId, string title, string message, NotificationType type, string? redirectUrl, DateTime createdAt, string createdBy)
        {
            var query = @"
                INSERT INTO IdtNotifications (Id, UserId, Title, Message, Type, RedirectUrl, IsRead, CreatedAt, CreatedBy)
                VALUES (@Id, @UserId, @Title, @Message, @Type, @RedirectUrl, 0, @CreatedAt, @CreatedBy);";

            var parameters = new DynamicParameters();
            parameters.Add("Id", id);
            parameters.Add("UserId", userId);
            parameters.Add("Title", title);
            parameters.Add("Message", message);
            parameters.Add("Type", (int)type);
            parameters.Add("RedirectUrl", redirectUrl);
            parameters.Add("CreatedAt", createdAt);
            parameters.Add("CreatedBy", createdBy);

            await _unitOfWork.Connection.ExecuteAsync(query, parameters);
        }

        /// <inheritdoc />
        public async Task<(IEnumerable<NotificationDto> Notifications, int TotalCount)> GetNotificationsByUserAsync(string userId, int offset, int pageSize)
        {
            var countQuery = "SELECT COUNT(*) FROM IdtNotifications WITH (NOLOCK) WHERE UserId = @UserId;";
            var totalCount = await _unitOfWork.Connection.ExecuteScalarAsync<int>(countQuery, new { UserId = userId });

            var query = @"
                SELECT Id, UserId, Title, Message, Type, RedirectUrl, IsRead, ReadAt, CreatedAt
                FROM IdtNotifications WITH (NOLOCK)
                WHERE UserId = @UserId
                ORDER BY CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            var notifications = await _unitOfWork.Connection.QueryAsync<NotificationDto>(
                query,
                new { UserId = userId, Offset = offset, PageSize = pageSize });

            return (notifications, totalCount);
        }

        /// <inheritdoc />
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            var query = "SELECT COUNT(*) FROM IdtNotifications WITH (NOLOCK) WHERE UserId = @UserId AND IsRead = 0;";
            return await _unitOfWork.Connection.ExecuteScalarAsync<int>(query, new { UserId = userId });
        }

        /// <inheritdoc />
        public async Task<int> MarkAsReadAsync(Guid notificationId, string userId, DateTime readAt)
        {
            var query = @"
                UPDATE IdtNotifications
                SET IsRead = 1, ReadAt = @ReadAt
                WHERE Id = @Id AND UserId = @UserId AND IsRead = 0;";

            return await _unitOfWork.Connection.ExecuteAsync(query, new { Id = notificationId, UserId = userId, ReadAt = readAt });
        }

        /// <inheritdoc />
        public async Task MarkAllAsReadAsync(string userId, DateTime readAt)
        {
            var query = @"
                UPDATE IdtNotifications
                SET IsRead = 1, ReadAt = @ReadAt
                WHERE UserId = @UserId AND IsRead = 0;";

            await _unitOfWork.Connection.ExecuteAsync(query, new { UserId = userId, ReadAt = readAt });
        }
    }
}
