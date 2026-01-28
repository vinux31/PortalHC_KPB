namespace HcPortal.Models
{
    public class TrainingRecord
    {
        public int Id { get; set; }
        public string? Judul { get; set; }      // Nama Training
        public string? Kategori { get; set; }   // "PROTON", "OJT", "MANDATORY"
        public DateTime Tanggal { get; set; }
        public string? Penyelenggara { get; set; } // "Internal", "Licensor", "NSO"
        public string? Status { get; set; }     // "Passed", "Wait Certificate", "Valid"
        public string? SertifikatUrl { get; set; }
        
        // New fields for certificate expiry tracking
        public DateTime? ValidUntil { get; set; }  // Certificate validity end date
        public string? CertificateType { get; set; } // "Permanent", "Annual", "3-Year"
        
        // Computed property: Returns true if certificate expires within 30 days
        public bool IsExpiringSoon
        {
            get
            {
                if (ValidUntil.HasValue && Status == "Valid")
                {
                    var daysUntilExpiry = (ValidUntil.Value - DateTime.Now).Days;
                    return daysUntilExpiry <= 30 && daysUntilExpiry >= 0;
                }
                return false;
            }
        }
        
        // Helper property: Days until expiry
        public int? DaysUntilExpiry
        {
            get
            {
                if (ValidUntil.HasValue)
                {
                    return (ValidUntil.Value - DateTime.Now).Days;
                }
                return null;
            }
        }
    }
}