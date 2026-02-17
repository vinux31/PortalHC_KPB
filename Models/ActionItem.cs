namespace HcPortal.Models
{
    public class ActionItem
    {
        public int Id { get; set; }
        public int CoachingSessionId { get; set; }
        public CoachingSession? CoachingSession { get; set; }
        public string Description { get; set; } = "";
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "Open";   // "Open", "In Progress", "Done"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
