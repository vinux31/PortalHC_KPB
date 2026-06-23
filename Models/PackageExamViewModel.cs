namespace HcPortal.Models
{
    /// <summary>
    /// Carries per-user shuffled exam data from StartExam GET to the exam view.
    /// Question and option order reflects the user's individual randomization.
    /// </summary>
    public class PackageExamViewModel
    {
        // Assessment identity
        public int AssessmentSessionId { get; set; }
        public string Title { get; set; } = "";
        public int DurationMinutes { get; set; }

        // Package info
        public bool HasPackages { get; set; }      // false = legacy path (no packages)
        public int? AssignmentId { get; set; }     // UserPackageAssignment.Id (for package path)

        // Ordered list of questions in this user's shuffled sequence
        public List<ExamQuestionItem> Questions { get; set; } = new();

        // Total count (for header display: "7/30 answered")
        public int TotalQuestions => Questions.Count;
    }

    public class ExamQuestionItem
    {
        public int QuestionId { get; set; }         // PackageQuestion.Id or AssessmentQuestion.Id
        public string QuestionText { get; set; } = "";
        public int DisplayNumber { get; set; }      // 1-based, user-facing number (reflects shuffled position)
        public List<ExamOptionItem> Options { get; set; } = new();

        /// <summary>
        /// Tipe soal: "MultipleChoice" (default), "MultipleAnswer", atau "Essay".
        /// Null/blank dianggap MultipleChoice (backward compatible).
        /// Digunakan plan 03 untuk render UI worker per tipe (radio/checkbox/textarea).
        /// </summary>
        public string QuestionType { get; set; } = "MultipleChoice";

        /// <summary>Batas karakter untuk soal Essay. Default 2000.</summary>
        public int MaxCharacters { get; set; } = 2000;

        // RND-01: gambar soal (StartExam). Diisi controller dari PackageQuestion.ImagePath/ImageAlt.
        public string? ImagePath { get; set; }
        public string? ImageAlt { get; set; }

        // Phase 417 PAG-01/02/03: metadata pagination section-aware.
        // Dihitung saat render (SectionPaginator.ComputePages), TIDAK disimpan per-soal (D-11, migration=FALSE).
        /// <summary>Nomor Section soal ini; null = grup "Lainnya".</summary>
        public int? SectionNumber { get; set; }
        /// <summary>Nama Section (D-417-01, name-only). Null untuk "Lainnya".</summary>
        public string? SectionName { get; set; }
        /// <summary>Salinan AssessmentPackageSection.StartNewPage — apakah Section ini mulai halaman baru.</summary>
        public bool SectionStartNewPage { get; set; }
        /// <summary>Indeks halaman 0-based hasil ComputePages. Default 0.</summary>
        public int PageNumber { get; set; }
        /// <summary>True bila soal ini awal Section (Section berubah) → render header polos.</summary>
        public bool IsSectionStart { get; set; }
        /// <summary>True bila Section SAMA tapi soal pertama di halaman baru (auto-split) → header "(lanjutan)".</summary>
        public bool IsSectionContinuation { get; set; }
    }

    public class ExamOptionItem
    {
        public int OptionId { get; set; }           // PackageOption.Id or AssessmentOption.Id
        public string OptionText { get; set; } = "";
        // Display letter (A/B/C/D) is assigned at render time by position index, NOT stored here

        // RND-01: gambar opsi (StartExam). Diisi controller dari PackageOption.ImagePath/ImageAlt.
        public string? ImagePath { get; set; }
        public string? ImageAlt { get; set; }
    }

    public class ExamSummaryItem
    {
        public int DisplayNumber { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = "";
        public int? SelectedOptionId { get; set; }
        public string? SelectedOptionText { get; set; }

        /// <summary>
        /// Tipe soal: "MultipleChoice", "MultipleAnswer", atau "Essay".
        /// Null/blank dianggap MultipleChoice (backward compatible).
        /// </summary>
        public string? QuestionType { get; set; }

        /// <summary>Daftar teks opsi yang dipilih untuk soal MultipleAnswer (e.g. ["A", "C"]).</summary>
        public List<string> SelectedOptionTexts { get; set; } = new();

        /// <summary>Jawaban teks untuk soal Essay.</summary>
        public string? TextAnswer { get; set; }

        // RND-02: gambar soal di ExamSummary. Diisi controller (Plan 03) dari q.ImagePath/ImageAlt.
        public string? ImagePath { get; set; }
        public string? ImageAlt { get; set; }

        // RND-02: carrier opsi-dengan-gambar untuk render block-bawah di ExamSummary.
        // Diisi controller (Plan 03) dari q.Options. ExamSummaryItem text-only → list terpisah.
        public List<ExamSummaryOptionItem> OptionImages { get; set; } = new();

        public bool IsAnswered =>
            (QuestionType == "Essay")
                ? !string.IsNullOrWhiteSpace(TextAnswer)
                : (QuestionType == "MultipleAnswer")
                    ? SelectedOptionTexts.Any()
                    : SelectedOptionId.HasValue;
    }

    /// <summary>
    /// Carrier opsi-dengan-gambar untuk render gambar opsi block-bawah di ExamSummary (RND-02).
    /// </summary>
    public class ExamSummaryOptionItem
    {
        public int OptionId { get; set; }
        public string OptionText { get; set; } = "";
        public string? ImagePath { get; set; }
        public string? ImageAlt { get; set; }
    }
}
