namespace HcPortal.Models
{
    /// <summary>
    /// Carries per-user shuffled exam data from StartExam GET to the exam view.
    /// Question and option order reflects the user's individual randomization.
    /// </summary>
    public class PackageExamViewModel
    {
        // Assessment identity
        public int AssessmentSessionId { get; set; }
        public string Title { get; set; } = "";
        public int DurationMinutes { get; set; }

        // Package info
        public bool HasPackages { get; set; }      // false = legacy path (no packages)
        public int? AssignmentId { get; set; }     // UserPackageAssignment.Id (for package path)

        // Ordered list of questions in this user's shuffled sequence
        public List<ExamQuestionItem> Questions { get; set; } = new();

        // Total count (for header display: "7/30 answered")
        public int TotalQuestions => Questions.Count;
    }

    public class ExamQuestionItem
    {
        public int QuestionId { get; set; }         // PackageQuestion.Id or AssessmentQuestion.Id
        public string QuestionText { get; set; } = "";
        public int DisplayNumber { get; set; }      // 1-based, user-facing number (reflects shuffled position)
        public List<ExamOptionItem> Options { get; set; } = new();
    }

    public class ExamOptionItem
    {
        public int OptionId { get; set; }           // PackageOption.Id or AssessmentOption.Id
        public string OptionText { get; set; } = "";
        // Display letter (A/B/C/D) is assigned at render time by position index, NOT stored here
    }
}
