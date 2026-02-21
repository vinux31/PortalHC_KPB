using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        /// <summary>
        /// ASP.NET Identity user ID of the actor who performed the action.
        /// </summary>
        [Required]
        public string ActorUserId { get; set; } = "";

        /// <summary>
        /// Display-friendly actor name (NIP + FullName), captured at write time
        /// so the log remains readable even if the user is later deleted.
        /// </summary>
        [Required]
        public string ActorName { get; set; } = "";

        /// <summary>
        /// Machine-readable action type. Values:
        /// CreateAssessment, EditAssessment, BulkAssign,
        /// DeleteAssessment, DeleteAssessmentGroup,
        /// ForceCloseAssessment, ResetAssessment
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } = "";

        /// <summary>
        /// Human-readable description of what happened, e.g.
        /// "Created assessment 'Safety OJT' for 5 users"
        /// </summary>
        [Required]
        public string Description { get; set; } = "";

        /// <summary>
        /// Optional: primary key of the target entity (e.g., AssessmentSession.Id).
        /// Nullable because group deletes have no single target.
        /// </summary>
        public int? TargetId { get; set; }

        /// <summary>
        /// Optional: type name of the target entity (e.g., "AssessmentSession").
        /// </summary>
        [MaxLength(100)]
        public string? TargetType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
