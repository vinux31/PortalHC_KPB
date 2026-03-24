using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
    public class TrainingRecord
    {
        public int Id { get; set; }

        // Foreign Key to User
        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }

        [MaxLength(200)]
        public string? Judul { get; set; }      // Nama Training
        [MaxLength(50)]
        public string? Kategori { get; set; }   // "PROTON", "OJT", "MANDATORY"
        public DateTime Tanggal { get; set; }
        [MaxLength(100)]
        public string? Penyelenggara { get; set; } // "Internal", "Licensor", "NSO"
        /// <summary>
        /// Status lifecycle TrainingRecord:
        ///
        /// Training Manual (Import/Add):
        ///   - "Passed" — training selesai, tanpa sertifikat (atau tanpa ValidUntil)
        ///   - "Valid" — training selesai dengan sertifikat (ValidUntil terisi)
        ///   - "Expired" — saat ValidUntil sudah lewat
        ///
        /// Assessment:
        ///   - "Failed" — peserta gagal ujian
        ///   - "Passed" — peserta lulus, tanpa sertifikat (GenerateCertificate = false)
        ///   - "Valid" — peserta lulus dengan sertifikat (GenerateCertificate = true, ValidUntil terisi)
        ///   - "Expired" — saat ValidUntil sudah lewat
        ///
        /// Status yang valid: Passed, Valid, Expired, Failed
        /// "Wait Certificate" sudah dihapus dan dimigrasikan ke "Passed".
        /// </summary>
        [MaxLength(20)]
        public string? Status { get; set; }
        [MaxLength(500)]
        public string? SertifikatUrl { get; set; }

        // New fields for certificate expiry tracking
        public DateTime? ValidUntil { get; set; }  // Certificate validity end date
        [MaxLength(20)]
        public string? CertificateType { get; set; } // "Permanent", "Annual", "3-Year"

        // v1.6 fields — start date, end date, certificate number
        public DateTime? TanggalMulai { get; set; }    // Training start date
        public DateTime? TanggalSelesai { get; set; }   // Training end date
        [MaxLength(100)]
        public string? NomorSertifikat { get; set; }    // Certificate number
        [MaxLength(100)]
        public string? Kota { get; set; }               // City where training took place
        [MaxLength(100)]
        public string? SubKategori { get; set; }         // Sub category from AssessmentCategories

        // ===== Phase 200: Renewal Chain FKs =====
        /// <summary>
        /// FK ke TrainingRecord lain yang di-renew oleh record ini (self-FK).
        /// Nullable. Hanya salah satu dari RenewsTrainingId/RenewsSessionId yang boleh diisi.
        /// </summary>
        public int? RenewsTrainingId { get; set; }

        /// <summary>
        /// FK ke AssessmentSession yang di-renew oleh record ini.
        /// Nullable. Hanya salah satu dari RenewsTrainingId/RenewsSessionId yang boleh diisi.
        /// </summary>
        public int? RenewsSessionId { get; set; }

        // Computed property: Returns true if certificate expires within 30 days
        public bool IsExpiringSoon
        {
            get
            {
                if (ValidUntil.HasValue && Status == "Valid")
                {
                    var daysUntilExpiry = (ValidUntil.Value - DateTime.UtcNow).Days;
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
                    return (ValidUntil.Value - DateTime.UtcNow).Days;
                }
                return null;
            }
        }
    }
}
