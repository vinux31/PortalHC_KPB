using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HcPortal.Models
{
    public class PackageUserResponse
    {
        [Key]
        public int Id { get; set; }

        public int AssessmentSessionId { get; set; }
        [ForeignKey("AssessmentSessionId")]
        public virtual AssessmentSession AssessmentSession { get; set; } = null!;

        public int PackageQuestionId { get; set; }
        [ForeignKey("PackageQuestionId")]
        public virtual PackageQuestion PackageQuestion { get; set; } = null!;

        public int? PackageOptionId { get; set; }
        [ForeignKey("PackageOptionId")]
        public virtual PackageOption? PackageOption { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Jawaban teks untuk soal Essay. Null untuk soal MultipleChoice dan MultipleAnswer.
        /// Diisi saat worker submit jawaban Essay. Belum dinilai sampai HC melakukan manual grading.
        /// </summary>
        public string? TextAnswer { get; set; }

        /// <summary>Skor manual HC per soal Essay (0 s/d ScoreValue). Null = belum dinilai.</summary>
        public int? EssayScore { get; set; }
    }
}
