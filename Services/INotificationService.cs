using HcPortal.Models;

namespace HcPortal.Services
{
    /// <summary>
    /// Notification service interface for managing in-app notifications.
    /// Provides CRUD operations for user notifications with async support.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Send a notification to a specific user.
        /// </summary>
        /// <param name="userId">User ID to receive notification</param>
        /// <param name="type">Notification type (e.g., "ASMT_ASSIGNED", "COACH_EVIDENCE_SUBMITTED")</param>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message</param>
        /// <param name="actionUrl">Optional deep link URL for notification click action</param>
        /// <returns>True if notification sent successfully, false otherwise</returns>
        Task<bool> SendAsync(string userId, string type, string title, string message, string? actionUrl = null);

        /// <summary>
        /// Get all notifications for a specific user, ordered by creation date (newest first).
        /// </summary>
        /// <param name="userId">User ID to fetch notifications for</param>
        /// <param name="count">Maximum number of notifications to return (default: 50)</param>
        /// <returns>List of UserNotification objects</returns>
        Task<List<UserNotification>> GetAsync(string userId, int count = 50);

        /// <summary>
        /// Mark a specific notification as read.
        /// </summary>
        /// <param name="notificationId">Notification ID to mark as read</param>
        /// <param name="userId">User ID (for authorization - users can only mark their own notifications)</param>
        /// <returns>True if marked as read successfully, false if notification not found or not owned by user</returns>
        Task<bool> MarkAsReadAsync(int notificationId, string userId);

        /// <summary>
        /// Mark all notifications for a user as read.
        /// </summary>
        /// <param name="userId">User ID to mark all notifications as read for</param>
        /// <returns>Number of notifications marked as read</returns>
        Task<int> MarkAllAsReadAsync(string userId);

        /// <summary>
        /// Get the count of unread notifications for a user.
        /// </summary>
        /// <param name="userId">User ID to count unread notifications for</param>
        /// <returns>Number of unread notifications</returns>
        Task<int> GetUnreadCountAsync(string userId);

        /// <summary>
        /// Send notification using predefined template with placeholder replacement.
        /// Templates ensure consistent messaging across all notification triggers (INFRA-08).
        /// </summary>
        /// <param name="userId">User ID to receive notification</param>
        /// <param name="type">Notification type (must match key in _templates dictionary)</param>
        /// <param name="context">Dictionary of placeholder values (e.g., { "AssessmentTitle": "Safety OJT", "AssessmentId": "123" })</param>
        /// <returns>True if notification sent successfully, false if template not found or send failed</returns>
        Task<bool> SendByTemplateAsync(string userId, string type, Dictionary<string, object>? context = null);

        /// <summary>
        /// Delete a specific notification (hard delete).
        /// </summary>
        /// <param name="notificationId">Notification ID to delete</param>
        /// <param name="userId">User ID (for authorization - users can only delete their own notifications)</param>
        /// <returns>True if deleted successfully, false if not found or not owned by user</returns>
        Task<bool> DeleteAsync(int notificationId, string userId);
    }
}
