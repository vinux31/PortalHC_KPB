using HcPortal.Models;

namespace HcPortal.Services
{
    /// <summary>
    /// Notification service interface for managing user notifications.
    /// Provides methods for sending, retrieving, and managing notification read status.
    /// All operations are async to prevent thread blocking.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Send a notification to a user.
        /// Creates a UserNotification record with IsRead=false and DeliveryStatus="Delivered".
        /// Wraps in try-catch - notification failures should not crash main workflows.
        /// </summary>
        /// <param name="userId">User ID to receive the notification</param>
        /// <param name="type">Notification type (e.g., "ASMT_ASSIGNED", "COACH_EVIDENCE_SUBMITTED")</param>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message with placeholders replaced</param>
        /// <param name="actionUrl">Deep link URL for notification click (optional)</param>
        /// <returns>True if notification delivered successfully, false if failed</returns>
        Task<bool> SendAsync(string userId, string type, string title, string message, string? actionUrl = null);

        /// <summary>
        /// Get recent notifications for a user, ordered by most recent first.
        /// Supports pagination via count parameter.
        /// </summary>
        /// <param name="userId">User ID to get notifications for</param>
        /// <param name="count">Number of notifications to retrieve (default: 20)</param>
        /// <returns>List of UserNotifications ordered by CreatedAt DESC</returns>
        Task<List<UserNotification>> GetAsync(string userId, int count = 20);

        /// <summary>
        /// Get unread notification count for a user.
        /// </summary>
        /// <param name="userId">User ID to count unread notifications for</param>
        /// <returns>Number of unread notifications (IsRead == false)</returns>
        Task<int> GetUnreadCountAsync(string userId);

        /// <summary>
        /// Mark a single notification as read.
        /// Sets IsRead=true and ReadAt=DateTime.UtcNow.
        /// </summary>
        /// <param name="notificationId">UserNotification ID to mark as read</param>
        /// <returns>True if marked successfully, false if notification not found or update failed</returns>
        Task<bool> MarkAsReadAsync(int notificationId);

        /// <summary>
        /// Mark all unread notifications for a user as read.
        /// Efficiently updates all notifications where UserId matches and IsRead=false.
        /// </summary>
        /// <param name="userId">User ID to mark all notifications as read for</param>
        /// <returns>Number of notifications marked as read</returns>
        Task<int> MarkAllAsReadAsync(string userId);
    }
}
