namespace HcPortal.Models
{
    public class CoachingHistoryViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? StatusFilter { get; set; }
        public List<CoachingSession> Sessions { get; set; } = new();
        public int TotalSessions => Sessions.Count;
        public int DraftSessions => Sessions.Count(s => s.Status == "Draft");
        public int SubmittedSessions => Sessions.Count(s => s.Status == "Submitted");
        public int TotalActionItems => Sessions.Sum(s => s.ActionItems?.Count ?? 0);
        public int OpenActionItems => Sessions
            .SelectMany(s => s.ActionItems ?? new List<ActionItem>())
            .Count(a => a.Status == "Open");
    }

    public class CreateSessionViewModel
    {
        public string CoacheeId { get; set; } = "";
        public DateTime Date { get; set; }
        public string Topic { get; set; } = "";
        public string? Notes { get; set; }
    }

    public class AddActionItemViewModel
    {
        public string Description { get; set; } = "";
        public DateTime DueDate { get; set; }
    }
}
