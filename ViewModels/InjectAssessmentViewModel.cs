using HcPortal.Models;

namespace HcPortal.ViewModels
{
    /// <summary>
    /// Phase 394 — bentuk POST wizard Inject Assessment Manual. Mirror permukaan
    /// <see cref="HcPortal.Models.InjectRequest"/> (DTO Phase 393, JANGAN diubah) tanpa field
    /// image/maxCharacters/Proton. Pemetaan VM → InjectRequest + UserId→NIP dilakukan controller
    /// (Plan 394-04). Tak ada write DB di 394 (D-07) — semua data ditahan di form-state.
    /// </summary>
    public class InjectAssessmentViewModel
    {
        // ── Setup Room (INJ-04) ──
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string AssessmentType { get; set; } = "Standard";   // "Standard"/"PreTest"/"PostTest" — NEVER "Manual"
        public DateTime CompletedAt { get; set; }                  // backdate ≤ today (D-06)
        public int DurationMinutes { get; set; } = 60;
        public int PassPercentage { get; set; } = 70;
        public bool AllowAnswerReview { get; set; } = true;

        // ── Sertifikat (INJ-07) ──
        public InjectCertMode CertMode { get; set; } = InjectCertMode.None;
        public string? ManualCertNumber { get; set; }              // dipakai hanya bila CertMode==Manual (D-09)
        public DateTime? CertValidUntil { get; set; }              // null/permanent = tanpa batas (D-10)
        public bool CertPermanent { get; set; }

        // ── Pekerja penerima (INJ-06) — picker checkbox name="UserIds" value=user.Id ──
        public List<string> UserIds { get; set; } = new();

        // ── Soal authored (INJ-05) — diisi Plan 03 (bound-list ATAU QuestionsJson) ──
        public List<InjectQuestionVM> Questions { get; set; } = new();
        public string? QuestionsJson { get; set; }                 // jalur alternatif capture hidden-JSON (Plan 03 pilih satu)

        // ── Jawaban per-pekerja (INJ-08/INJ-09) — Phase 395 ──
        // Hidden-JSON paralel QuestionsJson: per-worker { UserId, Mode, TargetScore, Answers[] }.
        // Mode/TargetScore = lapisan VM/controller saja (D-02) — TIDAK masuk InjectRequest/service.
        public string? AnswersJson { get; set; }

        /// <summary>Phase 396 — metode pengisian jawaban room-level (D-01/D-03): "form" (default) | "excel".
        /// Hanya memengaruhi isi #AnswersJson klien; tidak masuk InjectRequest/service.</summary>
        public string? Step5Method { get; set; }

        /// <summary>Soal authored (pre-persist) — mirror <see cref="InjectQuestionSpec"/>.</summary>
        public class InjectQuestionVM
        {
            public string QuestionText { get; set; } = "";
            public string QuestionType { get; set; } = "MultipleChoice";   // MultipleChoice/MultipleAnswer/Essay
            public int ScoreValue { get; set; } = 10;
            public int Order { get; set; }
            public string? ElemenTeknis { get; set; }
            public string? Rubrik { get; set; }
            public int TempId { get; set; }
            public List<InjectOptionVM> Options { get; set; } = new();
        }

        /// <summary>Opsi soal (pre-persist) — mirror <see cref="InjectOptionSpec"/>.</summary>
        public class InjectOptionVM
        {
            public string OptionText { get; set; } = "";
            public bool IsCorrect { get; set; }
            public int TempId { get; set; }
        }

        /// <summary>Jawaban 1 worker untuk 1 soal (Phase 395) — mirror <see cref="InjectAnswerSpec"/>.</summary>
        public class InjectAnswerVM
        {
            public int QuestionTempId { get; set; }
            public List<int> SelectedOptionTempIds { get; set; } = new();   // MC: 1; MA: ≥1; Essay: kosong
            public string? TextAnswer { get; set; }   // Essay
            public int? EssayScore { get; set; }       // Essay
        }

        /// <summary>
        /// Payload jawaban per-pekerja (Phase 395) — key = checkbox value = user.Id (sama dgn <see cref="UserIds"/>).
        /// Mode/TargetScore = INPUT auto-gen saja (D-02), di-resolve controller; tak masuk DTO/service.
        /// Skip = OMIT spec (D-05) — soal di-skip TIDAK ada di <see cref="Answers"/>.
        /// </summary>
        public class InjectWorkerAnswersVM
        {
            public string UserId { get; set; } = "";
            public string Mode { get; set; } = "manual";   // "manual" | "auto"
            public int TargetScore { get; set; }
            public List<InjectAnswerVM> Answers { get; set; } = new();
        }
    }
}
