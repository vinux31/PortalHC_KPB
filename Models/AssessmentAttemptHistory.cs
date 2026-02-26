using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
    public class AssessmentAttemptHistory
    {
        [Key]
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public int? Score { get; set; }
        public bool? IsPassed { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int AttemptNumber { get; set; }
        public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
