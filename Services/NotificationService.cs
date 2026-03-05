using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Services
{
    /// <summary>
    /// Notification service for managing user notifications.
    /// Follows AuditLogService pattern: async methods, DbContext injection, SaveChangesAsync internal.
    /// All methods wrapped in try-catch to prevent notification failures from crashing main workflows (INFRA-09).
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Send a notification to a user.
        /// Creates UserNotification with proper field values. Returns false on failure (never throws).
        /// </summary>
        public async Task<bool> SendAsync(string userId, string type, string title, string message, string? actionUrl = null)
        {
            try
            {
                var notification = new UserNotification
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    ActionUrl = actionUrl,
                    IsRead = false,
                    DeliveryStatus = "Delivered",
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserNotifications.Add(notification);
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                // Log failure internally (could add AuditLog here in future)
                // Never throw - notification failures shouldn't crash main workflow
                // In production, consider logging to AuditLog with ActionType "NotificationFailure"
                return false;
            }
        }

        /// <summary>
        /// Get recent notifications for a user, ordered by most recent first.
        /// Supports pagination via count parameter.
        /// </summary>
        public async Task<List<UserNotification>> GetAsync(string userId, int count = 20)
        {
            try
            {
                return await _context.UserNotifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch
            {
                // Return empty list on failure - don't crash calling code
                return new List<UserNotification>();
            }
        }

        /// <summary>
        /// Get unread notification count for a user.
        /// </summary>
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            try
            {
                return await _context.UserNotifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);
            }
            catch
            {
                // Return 0 on failure - don't crash calling code
                return 0;
            }
        }

        /// <summary>
        /// Mark a single notification as read.
        /// Sets IsRead=true and ReadAt=DateTime.UtcNow.
        /// </summary>
        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _context.UserNotifications.FindAsync(notificationId);
                if (notification == null)
                    return false;

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                // Return false on failure - don't crash calling code
                return false;
            }
        }

        /// <summary>
        /// Mark all unread notifications for a user as read.
        /// Efficiently updates all notifications where UserId matches and IsRead=false.
        /// </summary>
        public async Task<int> MarkAllAsReadAsync(string userId)
        {
            try
            {
                var unreadNotifications = await _context.UserNotifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return unreadNotifications.Count;
            }
            catch
            {
                // Return 0 on failure - don't crash calling code
                return 0;
            }
        }
    }
}
