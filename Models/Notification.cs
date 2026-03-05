using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
    /// <summary>
    /// Notification template defining the structure for a notification type.
    /// NOT used directly per user - see UserNotification for actual delivered notifications.
    /// In v3.3, this serves as a reference for notification type definitions.
    /// </summary>
    public class Notification
    {
        public int Id { get; set; }

        /// <summary>
        /// Machine-readable notification type identifier. Examples:
        /// ASMT_ASSIGNED, ASMT_RESULTS_READY, COACH_ASSIGNED, COACH_EVIDENCE_SUBMITTED,
        /// COACH_EVIDENCE_REJECTED, COACH_EVIDENCE_APPROVED_SRSPV, COACH_EVIDENCE_APPROVED_SH,
        /// COACH_EVIDENCE_APPROVED_HC, COACH_SESSION_COMPLETED
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "";

        /// <summary>
        /// Human-readable notification title. Example: "Assessment Assigned"
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = "";

        /// <summary>
        /// Message template with placeholders. Example: "You have been assigned to assessment: {AssessmentTitle}"
        /// Placeholders use {PropertyName} format for string replacement.
        /// </summary>
        [Required]
        public string MessageTemplate { get; set; } = "";

        /// <summary>
        /// Deep link URL for notification click action. Example: "/CMP/AssessmentDetails/{AssessmentId}"
        /// Supports placeholders for dynamic values.
        /// </summary>
        [MaxLength(500)]
        public string? ActionUrlTemplate { get; set; }

        /// <summary>
        /// Category for filtering/grouping. Values: "Assessment", "Coaching"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
