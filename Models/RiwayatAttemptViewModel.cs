using System.Collections.Generic;

namespace HcPortal.Models
{
    /// <summary>
    /// v32.4 RTK-08 — satu baris "percobaan" (attempt) untuk modal Riwayat HC.
    /// Menyatukan attempt ter-arsip (<see cref="AssessmentAttemptHistory"/>) dan attempt LIVE saat ini
    /// (<see cref="AssessmentSession"/>) dalam shape seragam. Per-soal (<see cref="Rows"/>) memakai
    /// shape IDENTIK <see cref="AssessmentAttemptResponseArchive"/> (arsip beku ATAU live via
    /// <c>RetakeArchiveBuilder.Build(0,...)</c>) → satu render-path di partial.
    /// </summary>
    public class RiwayatAttemptViewModel
    {
        /// <summary>Nomor percobaan (1-based). Current attempt = max(arsip)+1 (atau 1 bila belum ada arsip).</summary>
        public int AttemptNumber { get; set; }

        /// <summary>Skor persen attempt ini. Null bila belum dinilai.</summary>
        public int? ScorePercent { get; set; }

        /// <summary>Lulus/Gagal attempt ini. Null bila belum dinilai (pending grading).</summary>
        public bool? IsPassed { get; set; }

        /// <summary>Tanggal selesai attempt ini. Null bila belum selesai.</summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>True hanya untuk attempt LIVE saat ini ("Percobaan saat ini").</summary>
        public bool IsCurrent { get; set; }

        /// <summary>Rincian per-soal (QuestionText/AnswerText/IsCorrect tri-state/AwardedScore).</summary>
        public List<AssessmentAttemptResponseArchive> Rows { get; set; } = new();
    }
}
