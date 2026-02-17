namespace HcPortal.Models
{
    public class CoachingSession
    {
        public int Id { get; set; }
        public string CoachId { get; set; } = "";      // string ID, no FK — matches CoachingLog pattern
        public string CoacheeId { get; set; } = "";    // string ID, no FK
        public DateTime Date { get; set; }
        public string Topic { get; set; } = "";
        public string? Notes { get; set; }
        public string Status { get; set; } = "Draft";  // "Draft", "Submitted"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<ActionItem> ActionItems { get; set; } = new List<ActionItem>();
    }
}
