namespace HcPortal.Models
{
    public class CoachingSession
    {
        public int Id { get; set; }
        public string CoachId { get; set; } = "";      // string ID, no FK — matches CoachingLog pattern
        public string CoacheeId { get; set; } = "";    // string ID, no FK
        public DateTime Date { get; set; }
        public string Kompetensi { get; set; } = "";
        public string SubKompetensi { get; set; } = "";
        public string Deliverable { get; set; } = "";
        public string CoacheeCompetencies { get; set; } = "";  // multi-line text
        public string CatatanCoach { get; set; } = "";          // multi-line text
        public string Kesimpulan { get; set; } = "";             // "Kompeten" or "Perlu Pengembangan"
        public string Result { get; set; } = "";                 // "Need Improvement" / "Suitable" / "Good" / "Excellence"
        public string Status { get; set; } = "Draft";  // "Draft", "Submitted"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        /// <summary>Links coaching session to a specific deliverable progress record. No FK constraint — matches project pattern.</summary>
        public int? ProtonDeliverableProgressId { get; set; }
        public ICollection<ActionItem> ActionItems { get; set; } = new List<ActionItem>();
    }
}
