using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HcPortal.Models
{
    /// <summary>
    /// Granular audit log per question edit oleh Admin/HC.
    /// Snapshot text disimpan supaya audit tetap readable kalau soal/option dihapus.
    /// </summary>
    public class AssessmentEditLog
    {
        [Key]
        public int Id { get; set; }

        public int AssessmentSessionId { get; set; }
        [ForeignKey("AssessmentSessionId")]
        public virtual AssessmentSession? AssessmentSession { get; set; }

        public int PackageQuestionId { get; set; }

        public string QuestionTextSnapshot { get; set; } = "";
        public string OldAnswerJson { get; set; } = "[]";
        public string OldAnswerTextSnapshot { get; set; } = "";
        public string NewAnswerJson { get; set; } = "[]";
        public string NewAnswerTextSnapshot { get; set; } = "";

        public int? OldScore { get; set; }
        public int? NewScore { get; set; }
        public bool? OldIsPassed { get; set; }
        public bool? NewIsPassed { get; set; }

        public string ActorUserId { get; set; } = "";
        public string ActorName { get; set; } = "";
        public string ActorRole { get; set; } = "";

        public DateTime EditedAt { get; set; } = DateTime.UtcNow;

        public string ReasonCode { get; set; } = "";
        public string? ReasonText { get; set; }
    }
}
