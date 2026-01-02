namespace HcPortal.Models
{
    public class CoachingLog
    {
        public int Id { get; set; }
        public string? CoachName { get; set; }     // Nama Mentor/Atasan
        public string? CoacheeName { get; set; }   // Nama Peserta
        public string? Topik { get; set; }         // Topik Bahasan
        public DateTime Tanggal { get; set; }
        public string? Status { get; set; }        // "Draft", "Submitted", "Verified"
        public string? Catatan { get; set; }       // Notes singkat
    }
}