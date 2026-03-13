using Otomar.Shared.Dtos.Notification;
using Otomar.Shared.Enums;

namespace Otomar.Application.Interfaces.Repositories
{
    /// <summary>
    /// Provides data access operations for notification records.
    /// </summary>
    public interface INotificationRepository
    {
        /// <summary>
        /// Gets user IDs that belong to the specified role.
        /// </summary>
        /// <param name="normalizedRoleName">The uppercase-normalized role name.</param>
        /// <returns>A collection of user ID strings.</returns>
        Task<IEnumerable<string>> GetUserIdsByRoleAsync(string normalizedRoleName);

        /// <summary>
        /// Inserts a single notification record into the database.
        /// </summary>
        /// <param name="id">The notification identifier.</param>
        /// <param name="userId">The target user identifier.</param>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message body.</param>
        /// <param name="type">The notification type.</param>
        /// <param name="redirectUrl">An optional URL to redirect the user.</param>
        /// <param name="createdAt">The creation timestamp.</param>
        /// <param name="createdBy">The identifier of the user who created the notification.</param>
        Task InsertNotificationAsync(Guid id, string userId, string title, string message, NotificationType type, string? redirectUrl, DateTime createdAt, string createdBy);

        /// <summary>
        /// Gets notifications for a specific user with pagination support.
        /// </summary>
        /// <param name="userId">The target user identifier.</param>
        /// <param name="offset">The number of records to skip.</param>
        /// <param name="pageSize">The number of records to return.</param>
        /// <returns>A tuple containing the notifications and the total count.</returns>
        Task<(IEnumerable<NotificationDto> Notifications, int TotalCount)> GetNotificationsByUserAsync(string userId, int offset, int pageSize);

        /// <summary>
        /// Gets the count of unread notifications for a specific user.
        /// </summary>
        /// <param name="userId">The target user identifier.</param>
        /// <returns>The number of unread notifications.</returns>
        Task<int> GetUnreadCountAsync(string userId);

        /// <summary>
        /// Marks a single notification as read for a specific user.
        /// </summary>
        /// <param name="notificationId">The notification identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="readAt">The timestamp when the notification was read.</param>
        /// <returns>The number of affected rows.</returns>
        Task<int> MarkAsReadAsync(Guid notificationId, string userId, DateTime readAt);

        /// <summary>
        /// Marks all unread notifications as read for a specific user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="readAt">The timestamp when the notifications were read.</param>
        Task MarkAllAsReadAsync(string userId, DateTime readAt);
    }
}
