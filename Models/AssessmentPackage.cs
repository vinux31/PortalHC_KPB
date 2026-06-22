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

    /// <summary>
    /// Phase 415 SEC-01: Section per-paket untuk mengelompokkan soal (per area/equipment).
    /// SectionNumber menentukan urutan tampil; Name opsional (label). StartNewPage + ShuffleEnabled
    /// adalah toggle per-section yang disimpan di 415 dan dikonsumsi oleh fase 416 (scoped shuffle)
    /// dan 417 (pagination). Section bersifat opsional: soal tanpa Section (SectionId=null) tetap
    /// memakai perilaku global lama (grup "Lainnya").
    /// </summary>
    public class AssessmentPackageSection
    {
        [Key]
        public int Id { get; set; }

        public int AssessmentPackageId { get; set; }
        [ForeignKey("AssessmentPackageId")]
        public virtual AssessmentPackage AssessmentPackage { get; set; } = null!;

        /// <summary>Numeric ordering key for this section within its package (HC ketik 1, 2, 3...). Unik per paket.</summary>
        public int SectionNumber { get; set; }

        /// <summary>Nama/label tampilan Section. Opsional (boleh kosong) per spec §5.1.</summary>
        public string? Name { get; set; }

        /// <summary>Toggle "Mulai Halaman Baru" untuk section ini. Disimpan di 415, dikonsumsi 417 (pagination).</summary>
        public bool StartNewPage { get; set; } = false;

        /// <summary>Toggle "Acak" untuk section ini. Disimpan di 415, dikonsumsi 416 (scoped shuffle).</summary>
        public bool ShuffleEnabled { get; set; } = true;

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

        /// <summary>Phase 352 IMG-04: path relatif gambar soal (nullable). Diisi via FileUploadHelper.SaveFileAsync phase 353.</summary>
        public string? ImagePath { get; set; }

        /// <summary>Phase 352 IMG-04: teks alternatif gambar soal (aksesibilitas), maks 255 char. Nullable.</summary>
        [System.ComponentModel.DataAnnotations.MaxLength(255)]
        public string? ImageAlt { get; set; }

        /// <summary>Phase 415 SEC-03: nullable FK ke AssessmentPackageSection. null = soal tanpa Section (grup "Lainnya", perilaku global lama).</summary>
        public int? SectionId { get; set; }
        [ForeignKey("SectionId")]
        public virtual AssessmentPackageSection? Section { get; set; }

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

        /// <summary>Phase 352 IMG-04: path relatif gambar opsi (nullable).</summary>
        public string? ImagePath { get; set; }

        /// <summary>Phase 352 IMG-04: teks alternatif gambar opsi, maks 255 char. Nullable.</summary>
        [System.ComponentModel.DataAnnotations.MaxLength(255)]
        public string? ImageAlt { get; set; }
    }
}
