namespace HcPortal.Models
{
    public static class AssessmentConstants
    {
        public static class AssessmentType
        {
            public const string Manual = "Manual";
            public const string Online = "Online";
            public const string PreTest = "PreTest";
            public const string PostTest = "PostTest";
        }

        public static class AssessmentStatus
        {
            public const string Open = "Open";
            public const string Upcoming = "Upcoming";
            public const string Completed = "Completed";
            public const string PendingGrading = "Menunggu Penilaian"; // Phase 309 D-04 — set by GradingService L199 untuk session ber-essay
            public const string InProgress = "InProgress"; // Phase 310 WR-04 — peserta sedang mengerjakan ujian
            public const string Cancelled = "Cancelled";   // Phase 310 WR-04 — session dibatalkan
        }

        public static class CertificateType
        {
            public const string Permanent = "Permanent";
            public const string Annual = "Annual";
            public const string ThreeYear = "3-Year";
        }

        public static class FileValidation
        {
            public const long MaxCertificateFileSizeBytes = 10 * 1024 * 1024; // 10MB

            /// <summary>
            /// Allowed certificate file extensions. Case-insensitive lookup.
            /// </summary>
            public static readonly HashSet<string> AllowedCertificateExtensions = new(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".jpg", ".jpeg", ".png"
            };

            // Phase 352 IMG-04 / D-03: cap khusus gambar 5MB (BUKAN 10MB cert).
            public const long MaxImageFileSizeBytes = 5 * 1024 * 1024; // 5MB

            // Phase 352 IMG-04 / D-01/D-02: image-only allowlist (JPG/PNG, termasuk .jpeg) — TANPA .pdf.
            public static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png"
            };

            // Phase 325 D-09: Magic byte signatures per extension (lowercase keys).
            // Value = array of valid byte prefixes (multiple = OR match).
            // Sumber: docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md §5.2.
            public static readonly Dictionary<string, byte[][]> MagicBytes = new(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"]  = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } },  // %PDF
                [".jpg"]  = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },         // JFIF/EXIF/SPIFF universal prefix
                [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },         // alias .jpg (share value)
                [".png"]  = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } }    // PNG signature
            };

            /// <summary>
            /// Phase 325 D-09: Cek apakah header byte sequence cocok dengan magic byte signature untuk extension.
            /// </summary>
            /// <param name="ext">File extension (lowercase, eg ".pdf")</param>
            /// <param name="header">Buffer byte hasil baca stream 8-byte awal</param>
            /// <returns>true kalau header match salah satu prefix terdaftar; false kalau ext tidak terdaftar atau no prefix match</returns>
            public static bool MatchesMagicByte(string ext, byte[] header)
            {
                if (!MagicBytes.TryGetValue(ext, out var prefixes)) return false;
                foreach (var prefix in prefixes)
                {
                    if (header.Length < prefix.Length) continue;
                    bool match = true;
                    for (int i = 0; i < prefix.Length; i++)
                    {
                        if (header[i] != prefix[i]) { match = false; break; }
                    }
                    if (match) return true;
                }
                return false;
            }
        }

        // Phase 309 D-05 — top-level helper untuk normalisasi status semantik "submitted"
        // Returns true untuk Completed (selesai dinilai) ATAU PendingGrading (essay belum dinilai HC)
        public static bool IsAssessmentSubmitted(string? status) =>
            status == AssessmentStatus.Completed || status == AssessmentStatus.PendingGrading;
    }
}
