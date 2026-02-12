using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HcPortal.Models
{
    public class AssessmentQuestion
    {
        [Key]
        public int Id { get; set; }

        public int AssessmentSessionId { get; set; }
        [ForeignKey("AssessmentSessionId")]
        public virtual AssessmentSession AssessmentSession { get; set; }

        [Required]
        public string QuestionText { get; set; } = "";

        public string QuestionType { get; set; } = "MultipleChoice"; // "MultipleChoice", "TrueFalse", "Essay"

        public int ScoreValue { get; set; } = 10;

        public int Order { get; set; }

        // Navigation Property for Options
        public virtual ICollection<AssessmentOption> Options { get; set; } = new List<AssessmentOption>();
    }

    public class AssessmentOption
    {
        [Key]
        public int Id { get; set; }

        public int AssessmentQuestionId { get; set; }
        [ForeignKey("AssessmentQuestionId")]
        public virtual AssessmentQuestion Question { get; set; }

        [Required]
        public string OptionText { get; set; } = "";

        public bool IsCorrect { get; set; }
    }
}
