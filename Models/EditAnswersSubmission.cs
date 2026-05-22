namespace HcPortal.Models
{
    public class EditAnswersSubmission
    {
        public int SessionId { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Dictionary<int, List<int>> Answers { get; set; } = new();   // qId -> optionIds
        public Dictionary<int, EditReason> Reasons { get; set; } = new();  // qId -> reason
    }

    public class EditReason
    {
        public string Code { get; set; } = "";
        public string? Text { get; set; }
    }

    // Dry-run DTOs (untuk PreviewEditScore endpoint Task 9 PLAN 03)
    public class EditDraft
    {
        public int QuestionId { get; set; }
        public List<int> Options { get; set; } = new();
    }

    public class EditDraftSubmission
    {
        public List<EditDraft> Drafts { get; set; } = new();
    }
}
