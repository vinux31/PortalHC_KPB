using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HcPortal.Models
{
    public class UserResponse
    {
        [Key]
        public int Id { get; set; }

        public int AssessmentSessionId { get; set; }
        [ForeignKey("AssessmentSessionId")]
        public virtual AssessmentSession AssessmentSession { get; set; }

        public int AssessmentQuestionId { get; set; }
        [ForeignKey("AssessmentQuestionId")]
        public virtual AssessmentQuestion Question { get; set; }

        public int? SelectedOptionId { get; set; }
        [ForeignKey("SelectedOptionId")]
        public virtual AssessmentOption? SelectedOption { get; set; }

        public string? TextAnswer { get; set; } // For essay questions
    }
}
