namespace HcPortal.Models
{
    public class EditPesertaAnswersViewModel
    {
        public AssessmentSession Session { get; set; } = null!;
        public string FullName { get; set; } = "";
        public string NIP { get; set; } = "";
        public DateTime UpdatedAt { get; set; }
        public List<EditQuestionRow> Questions { get; set; } = new();
    }

    public class EditQuestionRow
    {
        public int PackageQuestionId { get; set; }
        public string QuestionText { get; set; } = "";
        public string QuestionType { get; set; } = ""; // MultipleChoice / MultipleAnswer / Essay
        public List<EditOptionRow> Options { get; set; } = new();
        public List<int> SelectedOptionIds { get; set; } = new();
        public List<int> CorrectOptionIds { get; set; } = new();
        public bool IsCurrentCorrect { get; set; }

        // RND-06: gambar soal di EditPesertaAnswers.
        public string? ImagePath { get; set; }
        public string? ImageAlt { get; set; }
    }

    public class EditOptionRow
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = "";
        public bool IsCorrect { get; set; }

        // RND-06: gambar opsi di EditPesertaAnswers.
        public string? ImagePath { get; set; }
        public string? ImageAlt { get; set; }
    }
}
