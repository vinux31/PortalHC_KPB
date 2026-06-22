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
        // Phase 414 (D-01): nilai EFEKTIF "boleh lihat tinjauan jawaban" = AllowAnswerReview || non-owner.
        // AllowAnswerReview (di atas) TETAP raw toggle — view butuh membedakan "OFF tapi admin tetap lihat" untuk nota admin.
        public bool CanReviewAnswers { get; set; }
        public bool GenerateCertificate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public List<QuestionReviewItem>? QuestionReviews { get; set; }
        public List<CompetencyGainItem>? CompetencyGains { get; set; }
        public List<ElemenTeknisScore>? ElemenTeknisScores { get; set; }
        public string? NomorSertifikat { get; set; }
        public bool IsPendingGrading { get; set; } = false; // Phase 309 SUB-01 — true saat assessment.Status == PendingGrading
    }

    public class QuestionReviewItem
    {
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; } = "";
        public string? UserAnswer { get; set; }
        public string CorrectAnswer { get; set; } = "";
        public bool IsCorrect { get; set; }
        public List<OptionReviewItem> Options { get; set; } = new List<OptionReviewItem>();
        public bool IsEssayPending { get; set; } = false; // Phase 309 OQ#3 D-08 — true saat status PendingGrading dan QuestionType Essay (label "Menunggu Penilaian" di view)

        // RND-03: gambar soal di Results review.
        public string? ImagePath { get; set; }
        public string? ImageAlt { get; set; }
    }

    public class OptionReviewItem
    {
        public string OptionText { get; set; } = "";
        public bool IsCorrect { get; set; }
        public bool IsSelected { get; set; }

        // RND-03: gambar opsi di Results review.
        public string? ImagePath { get; set; }
        public string? ImageAlt { get; set; }
    }

    public class ElemenTeknisScore
    {
        public string Name { get; set; } = "";
        public int Correct { get; set; }
        public int Total { get; set; }
        public double Percentage { get; set; }
    }

    public class CompetencyGainItem
    {
        public string CompetencyName { get; set; } = "";
        public int LevelGranted { get; set; }
    }
}
