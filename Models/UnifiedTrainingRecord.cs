namespace HcPortal.Models
{
    /// <summary>
    /// Flat ViewModel bridging AssessmentSession and TrainingRecord for unified table rendering.
    /// Non-applicable fields are null — rendered as em dash (—) in the view.
    /// </summary>
    public class UnifiedTrainingRecord
    {
        // Common fields (always populated)
        // For assessments: CompletedAt ?? Schedule; for trainings: Tanggal
        public DateTime Date { get; set; }

        // "Assessment Online" or "Training Manual" — used by view for badge rendering
        public string RecordType { get; set; } = "";

        // AssessmentSession.Title or TrainingRecord.Judul
        public string Title { get; set; } = "";

        // Assessment-only fields (null for Training Manual rows)
        public int? Score { get; set; }
        public bool? IsPassed { get; set; }

        // Training Manual-only fields (null for Assessment rows)
        public string? Penyelenggara { get; set; }
        public string? CertificateType { get; set; }
        public DateTime? ValidUntil { get; set; }

        // Status:
        //   Assessment rows: "Passed" if IsPassed==true, else "Failed"
        //   Training rows: value as-is from TrainingRecord.Status
        public string? Status { get; set; }

        // Certificate download URL (from TrainingRecord.SertifikatUrl) — null for Assessment rows
        public string? SertifikatUrl { get; set; }

        // Sort tie-break: 0 for Assessment Online (sorts first), 1 for Training Manual
        public int SortPriority { get; set; }

        // Computed: true only when ValidUntil is in the past — no lookahead window
        public bool IsExpired => ValidUntil.HasValue && ValidUntil.Value < DateTime.Now;

        // TrainingRecord.Id — null for Assessment Online rows; used for Edit/Delete actions
        public int? TrainingRecordId { get; set; }

        // Training Manual-only fields for Edit modal pre-population (null for Assessment rows)
        public string? Kategori { get; set; }
        public string? Kota { get; set; }
        public string? NomorSertifikat { get; set; }
        public DateTime? TanggalMulai { get; set; }
        public DateTime? TanggalSelesai { get; set; }
    }
}
