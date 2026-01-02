namespace HcPortal.Models
{
    public class AttendanceLog
    {
        public int Id { get; set; }
        public DateTime Tanggal { get; set; }
        public string? JamMasuk { get; set; }  // Format string "07:30" biar mudah
        public string? JamPulang { get; set; }
        public string? Status { get; set; }    // "Present", "Sick", "Leave", "Alpha"
        public int OvertimeHours { get; set; } // Jumlah jam lembur
        public string? Keterangan { get; set; }
    }
}