namespace HcPortal.Models
{
    /// <summary>
    /// Model untuk Laporan Coaching Proton
    /// </summary>
    public class CoachingLog
    {
        public int Id { get; set; }

        // Coach Information
        public string CoachId { get; set; } = "";
        public string CoachName { get; set; } = "";
        public string CoachPosition { get; set; } = "";  // Jabatan Coach
        
        // Coachee Information
        public string CoacheeId { get; set; } = "";
        public string CoacheeName { get; set; } = "";
        
        // Form Fields (Auto-fill)
        public string SubKompetensi { get; set; } = "";    // Auto-fill dari tabel
        public string Deliverables { get; set; } = "";      // Auto-fill dari tabel
        public DateTime Tanggal { get; set; }               // Auto-fill hari ini
        
        // Form Fields (Manual Input)
        public string CoacheeCompetencies { get; set; } = "";  // Textarea
        public string CatatanCoach { get; set; } = "";         // Textarea
        
        // Form Fields (Radio Buttons)
        public string Kesimpulan { get; set; } = "";  
        // "Mandiri" = Sudah bisa mengerjakan tugas secara mandiri
        // "PerluDikembangkan" = Masih perlu dikembangkan
        
        public string Result { get; set; } = "";      
        // "NeedImprovement" / "Suitable" / "Good" / "Excellence"
        
        // Status & Metadata
        public string Status { get; set; } = "Draft";  // "Draft", "Submitted"
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}