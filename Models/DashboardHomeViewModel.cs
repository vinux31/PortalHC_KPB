namespace HcPortal.Models
{
    /// <summary>
    /// ViewModel for the dynamic homepage dashboard
    /// </summary>
    public class DashboardHomeViewModel
    {
        // User Info
        public ApplicationUser CurrentUser { get; set; } = null!;
        public string Greeting { get; set; } = string.Empty;

        // Dashboard Cards
        public int IdpTotalCount { get; set; }
        public int IdpCompletedCount { get; set; }
        public int IdpProgressPercentage { get; set; }

        public int PendingAssessmentCount { get; set; }
        public bool HasUrgentAssessments { get; set; }

        public TrainingStatusInfo MandatoryTrainingStatus { get; set; } = new();

        // Recent Activities & Deadlines
        public List<RecentActivityItem> RecentActivities { get; set; } = new();
        public List<DeadlineItem> UpcomingDeadlines { get; set; } = new();
    }

    /// <summary>
    /// Training status information for HSSE certification
    /// </summary>
    public class TrainingStatusInfo
    {
        public bool IsValid { get; set; }
        public string Status { get; set; } = string.Empty;  // "VALID", "EXPIRING SOON", "EXPIRED", "NO RECORDS"
        public DateTime? ValidUntil { get; set; }
        public string? CertificateUrl { get; set; }
        public int DaysUntilExpiry { get; set; }
    }

    /// <summary>
    /// Recent activity timeline item
    /// </summary>
    public class RecentActivityItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string TimeAgo { get; set; } = string.Empty;  // "2 hours ago", "1 day ago"
        public string IconClass { get; set; } = "fas fa-circle";  // "fas fa-clipboard-check"
    }

    /// <summary>
    /// Upcoming deadline item
    /// </summary>
    public class DeadlineItem
    {
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string DueDateFormatted { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
        public string UrgencyClass { get; set; } = "normal";  // "urgent", "normal"
        public string IconClass { get; set; } = "fas fa-circle";
        public string? ActionUrl { get; set; }  // Optional link
    }
}
