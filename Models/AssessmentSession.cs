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
        public string Type { get; set; } = ""; // Sama dengan Category, untuk filtering
        
        public DateTime Schedule { get; set; }
        public int DurationMinutes { get; set; }
        public string Status { get; set; } = "";   // "Open", "Upcoming", "Completed"
        
        // New Visualization Props
        public int Progress { get; set; } = 0; // 0 - 100
        public string BannerColor { get; set; } = "bg-primary"; // Bootstrap color class or hex

        public int? Score { get; set; }
        public bool IsTokenRequired { get; set; }
    }
}