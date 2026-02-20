namespace HcPortal.Models
{
    public class MonitoringGroupViewModel
    {
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime Schedule { get; set; }          // representative Schedule (with time)
        public string GroupStatus { get; set; } = "";   // "Open", "Upcoming", or "Closed"
        public int TotalCount { get; set; }             // all sessions in this group
        public int CompletedCount { get; set; }         // sessions where IsCompleted == true
        public int PassedCount { get; set; }            // sessions where IsPassed == true
        public List<MonitoringSessionViewModel> Sessions { get; set; } = new();
    }

    public class MonitoringSessionViewModel
    {
        public int Id { get; set; }
        public string UserFullName { get; set; } = "";
        public string UserNIP { get; set; } = "";
        public string UserStatus { get; set; } = "";    // "Not started", "InProgress", or "Completed"
        public int? Score { get; set; }
        public bool? IsPassed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? StartedAt { get; set; }
    }
}
