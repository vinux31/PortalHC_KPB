using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
    /// <summary>
    /// v32.4 RTK-02 — Snapshot per-soal dari SATU attempt assessment yang diarsipkan.
    /// Dibekukan saat retake/reset SEBELUM <see cref="PackageUserResponse"/> dihapus, sehingga
    /// verdict (benar/salah) + jawaban worker tetap utuh ("frozen verdict before delete") dan
    /// tahan terhadap edit/hapus soal kemudian — BUKAN recompute. Anak dari
    /// <see cref="AssessmentAttemptHistory"/> (FK cascade).
    /// </summary>
    public class AssessmentAttemptResponseArchive
    {
        [Key]
        public int Id { get; set; }

        /// <summary>FK ke <see cref="AssessmentAttemptHistory.Id"/> (ON DELETE CASCADE).</summary>
        public int AttemptHistoryId { get; set; }

        /// <summary>Navigasi ke parent attempt (nullable — diisi EF saat load).</summary>
        public AssessmentAttemptHistory? AttemptHistory { get; set; }

        /// <summary>Id soal sumber. Plain int, NO FK — soal bisa terhapus kemudian.</summary>
        public int PackageQuestionId { get; set; }

        /// <summary>Snapshot teks soal beku saat archive.</summary>
        public string QuestionText { get; set; } = "";

        /// <summary>
        /// Jawaban worker untuk display (essay full-text per Pitfall 2; diisi plan 405-02).
        /// Null = tidak dijawab.
        /// </summary>
        public string? AnswerText { get; set; }

        /// <summary>Verdict beku: true=Benar, false=Salah, null=essay pending.</summary>
        public bool? IsCorrect { get; set; }

        /// <summary>Skor yang didapat soal ini saat archive.</summary>
        public int AwardedScore { get; set; }

        public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
    }
}
