using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HcPortal.Models
{
    public class AssessmentPackage
    {
        [Key]
        public int Id { get; set; }

        public int AssessmentSessionId { get; set; }
        [ForeignKey("AssessmentSessionId")]
        public virtual AssessmentSession AssessmentSession { get; set; } = null!;

        /// <summary>Display name for this package, e.g. "Paket A", "Paket B", "Paket C".</summary>
        public string PackageName { get; set; } = "";

        /// <summary>Numeric ordering key for display (1, 2, 3...).</summary>
        public int PackageNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<PackageQuestion> Questions { get; set; } = new List<PackageQuestion>();
    }

    public class PackageQuestion
    {
        [Key]
        public int Id { get; set; }

        public int AssessmentPackageId { get; set; }
        [ForeignKey("AssessmentPackageId")]
        public virtual AssessmentPackage AssessmentPackage { get; set; } = null!;

        public string QuestionText { get; set; } = "";

        /// <summary>Original import order; used as stable sort key before per-user shuffle is applied.</summary>
        public int Order { get; set; }

        public int ScoreValue { get; set; } = 10;

        /// <summary>
        /// Tipe soal: "MultipleChoice" (default), "MultipleAnswer", atau "Essay".
        /// Null berarti "MultipleChoice" (backward compatible untuk data lama sebelum Phase 296).
        /// Per D-06: disimpan sebagai string konsisten dengan pattern Status field.
        /// </summary>
        public string? QuestionType { get; set; }

        /// <summary>Optional elemen teknis tag for analysis grouping (e.g. "Pengetahuan Proses").</summary>
        public string? ElemenTeknis { get; set; }

        /// <summary>Rubrik/kunci jawaban untuk soal Essay. Referensi HC saat grading manual. Null untuk MC/MA.</summary>
        public string? Rubrik { get; set; }

        /// <summary>Batas karakter jawaban Essay per soal. Default 2000. Diabaikan untuk MC/MA.</summary>
        public int MaxCharacters { get; set; } = 2000;

        // Navigation
        public virtual ICollection<PackageOption> Options { get; set; } = new List<PackageOption>();
    }

    public class PackageOption
    {
        [Key]
        public int Id { get; set; }

        public int PackageQuestionId { get; set; }
        [ForeignKey("PackageQuestionId")]
        public virtual PackageQuestion PackageQuestion { get; set; } = null!;

        public string OptionText { get; set; } = "";

        /// <summary>
        /// True if this option is the correct answer key.
        /// NOTE: No Letter field — letters (A/B/C/D) are display-only, assigned at render time based on
        /// the shuffled position. Grading uses PackageOption.Id exclusively.
        /// </summary>
        public bool IsCorrect { get; set; }
    }
}
