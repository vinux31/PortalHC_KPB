using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
    /// <summary>
    /// Per-user notification instance created when a notification is delivered to a specific user.
    /// Tracks read status, delivery status, and audit trail for each notification recipient.
    /// </summary>
    public class UserNotification
    {
        public int Id { get; set; }

        /// <summary>
        /// User ID who receives this notification (foreign key to ApplicationUser.Id).
        /// </summary>
        [Required]
        public string UserId { get; set; } = "";

        /// <summary>
        /// Notification type identifier (matches Notification.Type).
        /// Denormalized for query performance - avoids join with Notification table.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "";

        /// <summary>
        /// Notification title (denormalized from Notification.Title).
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = "";

        /// <summary>
        /// Formatted message with placeholders replaced (denormalized from Notification.MessageTemplate).
        /// Example: "You have been assigned to assessment: Safety OJT"
        /// </summary>
        [Required]
        public string Message { get; set; } = "";

        /// <summary>
        /// Deep link URL with placeholders replaced (denormalized from Notification.ActionUrlTemplate).
        /// Example: "/CMP/AssessmentDetails/123"
        /// </summary>
        [MaxLength(500)]
        public string? ActionUrl { get; set; }

        /// <summary>
        /// Whether the user has read this notification.
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// When the user marked this notification as read (null if unread).
        /// </summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// Delivery status tracking. Values: "Pending", "Delivered", "Failed"
        /// In v3.3, all notifications are "Delivered" immediately (no async queue).
        /// Field reserved for v3.4 when background job queue is added.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string DeliveryStatus { get; set; } = "Delivered";

        /// <summary>
        /// When this notification was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ApplicationUser? User { get; set; }
    }
}
