namespace HcPortal.Models
{
    /// <summary>
    /// Model untuk mapping relasi Coach ke Coachee
    /// Satu Coach bisa membimbing banyak Coachee
    /// </summary>
    public class CoachCoacheeMapping
    {
        public int Id { get; set; }
        
        /// <summary>
        /// User ID dari Coach (ApplicationUser.Id)
        /// </summary>
        public string CoachId { get; set; } = "";
        
        /// <summary>
        /// User ID dari Coachee (ApplicationUser.Id)
        /// </summary>
        public string CoacheeId { get; set; } = "";
        
        /// <summary>
        /// Status aktif/non-aktif
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Tanggal mulai coaching
        /// </summary>
        public DateTime StartDate { get; set; }
        
        /// <summary>
        /// Tanggal selesai coaching (opsional)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Section penugasan (bisa beda dari section pekerja). Null = fallback ke section pekerja.
        /// </summary>
        public string? AssignmentSection { get; set; }

        /// <summary>
        /// Unit penugasan (bisa beda dari unit pekerja). Null = fallback ke unit pekerja.
        /// </summary>
        public string? AssignmentUnit { get; set; }
    }
}
