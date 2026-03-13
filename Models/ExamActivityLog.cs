using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HcPortal.Models
{
    /// <summary>
    /// Records significant events during an exam session for HC audit trail.
    /// EventType values: "started", "page_nav", "disconnected", "reconnected", "submitted"
    /// </summary>
    public class ExamActivityLog
    {
        public int Id { get; set; }

        [Required]
        public int SessionId { get; set; }

        [ForeignKey(nameof(SessionId))]
        public AssessmentSession? Session { get; set; }

        /// <summary>
        /// One of: started, page_nav, disconnected, reconnected, submitted
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = "";

        /// <summary>
        /// Optional detail, e.g. "Halaman 3" for page_nav events
        /// </summary>
        [MaxLength(255)]
        public string? Detail { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
