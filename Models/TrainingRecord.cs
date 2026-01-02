namespace HcPortal.Models
{
    public class TrainingRecord
    {
        public int Id { get; set; }
        public string? Judul { get; set; }      // Nama Training
        public string? Kategori { get; set; }   // "PROTON", "OJT", "MANDATORY"
        public DateTime Tanggal { get; set; }
        public string? Penyelenggara { get; set; } // "Internal", "Licensor", "NSO"
        public string? Status { get; set; }     // "Passed", "Wait Certificate"
        public string? SertifikatUrl { get; set; }
    }
}