using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;

namespace HcPortal.Services
{
    /// <summary>
    /// Notification service implementation for managing in-app notifications.
    /// Follows AuditLogService pattern: async, scoped DI, try-catch wrapped.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        /// <summary>
        /// Notification template structure for consistent messaging.
        /// </summary>
        private class NotificationTemplate
        {
            public string Title { get; set; } = "";
            public string MessageTemplate { get; set; } = "";
            public string? ActionUrlTemplate { get; set; }
        }

        private readonly Dictionary<string, NotificationTemplate> _templates;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;

            // Initialize notification templates for all v3.3 trigger types
            _templates = new Dictionary<string, NotificationTemplate>
            {
                // Assessment Notifications (Phase 101)
                ["ASMT_ASSIGNED"] = new NotificationTemplate
                {
                    Title = "Assessment Assigned",
                    MessageTemplate = "You have been assigned to assessment: {AssessmentTitle}",
                    ActionUrlTemplate = "/CMP/AssessmentDetails/{AssessmentId}"
                },
                ["ASMT_RESULTS_READY"] = new NotificationTemplate
                {
                    Title = "Assessment Results Ready",
                    MessageTemplate = "Your results for {AssessmentTitle} are ready. Score: {Score}%",
                    ActionUrlTemplate = "/CMP/AssessmentResults/{AssessmentId}"
                },

                // Coaching Proton Notifications (Phase 102)
                ["COACH_ASSIGNED"] = new NotificationTemplate
                {
                    Title = "Coach Assigned",
                    MessageTemplate = "Your coach {CoachName} has been assigned for coaching program",
                    ActionUrlTemplate = "/CDP/ProtonProgress"
                },
                ["COACH_EVIDENCE_SUBMITTED"] = new NotificationTemplate
                {
                    Title = "Evidence Submitted for Review",
                    MessageTemplate = "Coach {CoachName} has submitted evidence for {CoacheeName}. Please review.",
                    ActionUrlTemplate = "/CDP/ProtonProgress"
                },
                ["COACH_EVIDENCE_REJECTED"] = new NotificationTemplate
                {
                    Title = "Evidence Rejected",
                    MessageTemplate = "Your evidence was rejected. Reason: {RejectionReason}. Please resubmit.",
                    ActionUrlTemplate = "/CDP/ProtonProgress"
                },
                ["COACH_EVIDENCE_APPROVED_SRSPV"] = new NotificationTemplate
                {
                    Title = "Evidence Approved by SrSpv",
                    MessageTemplate = "Evidence for {CoacheeName} has been approved by Senior Supervisor. Forwarded to Section Head.",
                    ActionUrlTemplate = "/CDP/ProtonProgress"
                },
                ["COACH_EVIDENCE_APPROVED_SH"] = new NotificationTemplate
                {
                    Title = "Evidence Approved by Section Head",
                    MessageTemplate = "Evidence for {CoacheeName} has been approved by Section Head. Forwarded to HC.",
                    ActionUrlTemplate = "/CDP/ProtonProgress"
                },
                ["COACH_EVIDENCE_APPROVED_HC"] = new NotificationTemplate
                {
                    Title = "Evidence Approved by HC",
                    MessageTemplate = "Evidence for {CoacheeName} has been approved by HC. Coaching session completed.",
                    ActionUrlTemplate = "/CDP/ProtonProgress"
                },
                ["COACH_SESSION_COMPLETED"] = new NotificationTemplate
                {
                    Title = "Coaching Session Completed",
                    MessageTemplate = "Your coaching session with {CoachName} has been completed successfully.",
                    ActionUrlTemplate = "/CDP/ProtonProgress"
                }
            };
        }

        /// <summary>
        /// Send a notification to a specific user.
        /// Creates a UserNotification record with delivery status "Delivered" (v3.3: synchronous, no queue).
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send notification type={Type} to user={UserId}", type, userId);
                return false;
            }
        }

        /// <summary>
        /// Get all notifications for a specific user, ordered by creation date (newest first).
        /// </summary>
        public async Task<List<UserNotification>> GetAsync(string userId, int count = 50)
        {
            try
            {
                return await _context.UserNotifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get notifications for user={UserId}", userId);
                return new List<UserNotification>();
            }
        }

        /// <summary>
        /// Mark a specific notification as read.
        /// Includes authorization check - users can only mark their own notifications.
        /// </summary>
        public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
        {
            try
            {
                var notification = await _context.UserNotifications.FindAsync(notificationId);

                if (notification == null || notification.UserId != userId)
                {
                    return false;
                }

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to mark notification={NotificationId} as read for user={UserId}", notificationId, userId);
                return false;
            }
        }

        /// <summary>
        /// Mark all notifications for a user as read.
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to mark all notifications as read for user={UserId}", userId);
                return 0;
            }
        }

        /// <summary>
        /// Get the count of unread notifications for a user.
        /// Used for notification badge count in UI.
        /// </summary>
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            try
            {
                return await _context.UserNotifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get unread count for user={UserId}", userId);
                return 0;
            }
        }

        /// <summary>
        /// Send notification using predefined template with placeholder replacement.
        /// Templates ensure consistent messaging across all notification triggers (INFRA-08).
        /// </summary>
        /// <param name="userId">User ID to receive notification</param>
        /// <param name="type">Notification type (must match key in _templates dictionary)</param>
        /// <param name="context">Dictionary of placeholder values (e.g., { "AssessmentTitle": "Safety OJT", "AssessmentId": "123" })</param>
        /// <returns>True if notification sent successfully, false if template not found or send failed</returns>
        public async Task<bool> SendByTemplateAsync(string userId, string type, Dictionary<string, object>? context = null)
        {
            try
            {
                if (!_templates.TryGetValue(type, out var template))
                {
                    // Unknown notification type - fail silently
                    return false;
                }

                // Replace placeholders in message template
                var message = template.MessageTemplate;
                var actionUrl = template.ActionUrlTemplate;

                if (context != null)
                {
                    foreach (var kvp in context)
                    {
                        var placeholder = $"{{{kvp.Key}}}";
                        message = message.Replace(placeholder, kvp.Value?.ToString() ?? "");
                        if (actionUrl != null)
                        {
                            actionUrl = actionUrl.Replace(placeholder, kvp.Value?.ToString() ?? "");
                        }
                    }
                }

                return await SendAsync(userId, type, template.Title, message, actionUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send template notification type={Type} to user={UserId}", type, userId);
                return false;
            }
        }

        /// <summary>
        /// Delete a specific notification (hard delete).
        /// </summary>
        public async Task<bool> DeleteAsync(int notificationId, string userId)
        {
            try
            {
                var notification = await _context.UserNotifications.FindAsync(notificationId);

                if (notification == null || notification.UserId != userId)
                {
                    return false;
                }

                _context.UserNotifications.Remove(notification);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete notification={NotificationId} for user={UserId}", notificationId, userId);
                return false;
            }
        }
    }
}
