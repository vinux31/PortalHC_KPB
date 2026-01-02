namespace HcPortal.Models
{
    public class AssessmentSession
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Category { get; set; } = ""; // "Technical", "Soft Skill", "PROTON"
        public DateTime Schedule { get; set; }
        public int DurationMinutes { get; set; }
        public string Status { get; set; } = "";   // "Open", "Upcoming", "Completed"
        public int? Score { get; set; }            // Nullable (bisa kosong kalau belum selesai)
        public bool IsTokenRequired { get; set; }  // True jika butuh token NSO
    }
}