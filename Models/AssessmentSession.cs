using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
    public class AssessmentSession
    {
        public int Id { get; set; }
        
        // Foreign Key to User
        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }
        
        public string Title { get; set; } = "";

        // Kategori utama: "Assessment OJ", "IHT", "Licencor", "OTS", "Mandatory HSSE Training"
        public string Category { get; set; } = "";

        public DateTime Schedule { get; set; }
        public int DurationMinutes { get; set; }
        public string Status { get; set; } = "";   // "Open", "Upcoming", "Completed"

        // New Visualization Props
        public int Progress { get; set; } = 0; // 0 - 100
        public string BannerColor { get; set; } = "bg-primary"; // Bootstrap color class or hex

        public int? Score { get; set; }

        [Range(0, 100)]
        [Display(Name = "Pass Percentage (%)")]
        public int PassPercentage { get; set; } = 70;

        [Display(Name = "Allow Answer Review")]
        public bool AllowAnswerReview { get; set; } = true;

        public bool? IsPassed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Total seconds the worker has actively spent in the exam (excludes offline time).
        /// Updated on each page navigation and every 30 seconds via frontend polling.
        /// Default 0 on session start.
        /// </summary>
        public int ElapsedSeconds { get; set; } = 0;

        /// <summary>
        /// Last page (0-based index) the worker was viewing before disconnect.
        /// Null = never navigated (still on page 0). Used to resume on correct page.
        /// </summary>
        public int? LastActivePage { get; set; }

        /// <summary>
        /// Optional hard cutoff date for this exam window. Workers cannot start (or restart) the exam
        /// after this date. Null = no expiry enforced.
        /// </summary>
        public DateTime? ExamWindowCloseDate { get; set; }

        public bool IsTokenRequired { get; set; }
        public string AccessToken { get; set; } = "";

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation Properties for Exam Engine
        public virtual ICollection<AssessmentQuestion> Questions { get; set; } = new List<AssessmentQuestion>();
        public virtual ICollection<UserResponse> Responses { get; set; } = new List<UserResponse>();
    }
}