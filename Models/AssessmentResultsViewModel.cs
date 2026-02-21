namespace HcPortal.Models
{
    public class AssessmentResultsViewModel
    {
        public int AssessmentId { get; set; }
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string UserFullName { get; set; } = "";
        public int Score { get; set; }
        public int PassPercentage { get; set; }
        public bool IsPassed { get; set; }
        public bool AllowAnswerReview { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public List<QuestionReviewItem>? QuestionReviews { get; set; }
        public List<CompetencyGainItem>? CompetencyGains { get; set; }
    }

    public class QuestionReviewItem
    {
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; } = "";
        public string? UserAnswer { get; set; }
        public string CorrectAnswer { get; set; } = "";
        public bool IsCorrect { get; set; }
        public List<OptionReviewItem> Options { get; set; } = new List<OptionReviewItem>();
    }

    public class OptionReviewItem
    {
        public string OptionText { get; set; } = "";
        public bool IsCorrect { get; set; }
        public bool IsSelected { get; set; }
    }

    public class CompetencyGainItem
    {
        public string CompetencyName { get; set; } = "";
        public int LevelGranted { get; set; }
    }
}
