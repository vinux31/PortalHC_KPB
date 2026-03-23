namespace HcPortal.Models
{
    public class DashboardHomeViewModel
    {
        public ApplicationUser CurrentUser { get; set; } = null!;
        public string Greeting { get; set; } = string.Empty;
        public List<UpcomingEventViewModel> UpcomingEvents { get; set; } = new();
        public ProgressViewModel Progress { get; set; } = new();
        public int ExpiredCount { get; set; }
        public int AkanExpiredCount { get; set; }
    }

    public class UpcomingEventViewModel
    {
        public string Type { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Date { get; set; }
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public class ProgressViewModel
    {
        public int CdpProgress { get; set; }
        public int CdpTotal { get; set; }
        public int CdpCompleted { get; set; }
        public string CdpTrackName { get; set; } = "";

        public int AssessmentTotal { get; set; }
        public int AssessmentCompleted { get; set; }
        public int AssessmentProgress { get; set; }

        public int CoachingTotal { get; set; }
        public int CoachingCompleted { get; set; }
        public int CoachingProgress { get; set; }
    }
}
