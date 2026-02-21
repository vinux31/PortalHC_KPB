using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HcPortal.Models
{
    public class PackageUserResponse
    {
        [Key]
        public int Id { get; set; }

        public int AssessmentSessionId { get; set; }
        [ForeignKey("AssessmentSessionId")]
        public virtual AssessmentSession AssessmentSession { get; set; } = null!;

        public int PackageQuestionId { get; set; }
        [ForeignKey("PackageQuestionId")]
        public virtual PackageQuestion PackageQuestion { get; set; } = null!;

        public int? PackageOptionId { get; set; }
        [ForeignKey("PackageOptionId")]
        public virtual PackageOption? PackageOption { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
