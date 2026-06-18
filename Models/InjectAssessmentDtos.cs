namespace HcPortal.Models
{
    /// <summary>Mode sertifikat per-room inject (D-07/D-08/D-09/D-10).</summary>
    public enum InjectCertMode { None = 0, Auto = 1, Manual = 2 }

    /// <summary>Spesifikasi opsi soal authored (POCO — belum ter-persist).</summary>
    public class InjectOptionSpec
    {
        public string OptionText { get; set; } = "";
        public bool IsCorrect { get; set; }
        public int TempId { get; set; }   // id sementara untuk peta jawaban worker → opsi (pre-persist)
    }

    /// <summary>Spesifikasi soal authored (POCO — belum ter-persist).</summary>
    public class InjectQuestionSpec
    {
        public string QuestionText { get; set; } = "";
        public string QuestionType { get; set; } = "MultipleChoice";   // "MultipleChoice"/"MultipleAnswer"/"Essay"
        public int ScoreValue { get; set; } = 10;
        public int Order { get; set; }
        public string? ElemenTeknis { get; set; }
        public string? Rubrik { get; set; }
        public int TempId { get; set; }   // id sementara untuk peta jawaban worker → soal (pre-persist)
        public List<InjectOptionSpec> Options { get; set; } = new();
    }

    /// <summary>Jawaban satu worker untuk satu soal (model "input asli", D-08/INJ-08).</summary>
    public class InjectAnswerSpec
    {
        public int QuestionTempId { get; set; }
        public List<int> SelectedOptionTempIds { get; set; } = new();   // MC: 1 elemen; MA: ≥1; Essay: kosong
        public string? TextAnswer { get; set; }   // Essay
        public int? EssayScore { get; set; }       // Essay — wajib (D-05), divalidasi 0..ScoreValue (D-07)
    }

    /// <summary>Satu worker penerima inject + jawabannya + optional nomor cert manual.</summary>
    public class InjectWorkerSpec
    {
        public string Nip { get; set; } = "";
        public List<InjectAnswerSpec> Answers { get; set; } = new();
        public string? ManualCertNumber { get; set; }   // dipakai hanya bila CertMode==Manual (D-09)
        public DateOnly? CertValidUntil { get; set; }    // null = permanent (D-10)
    }

    /// <summary>Request inject batch (1 room, 1 paket, banyak worker).</summary>
    public class InjectRequest
    {
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string AssessmentType { get; set; } = "Standard";   // BUKAN "Manual" — D-deviation
        public DateTime CompletedAt { get; set; }   // backdate (D-06: ≤ today)
        public DateTime? StartedAt { get; set; }     // backdate; null → pakai CompletedAt
        public DateTime? Schedule { get; set; }      // backdate; null → pakai CompletedAt
        public int DurationMinutes { get; set; } = 60;
        public int PassPercentage { get; set; } = 70;
        public bool AllowAnswerReview { get; set; } = true;

        /// <summary>true (default, jalur Form 395) = teks essay wajib bila skor diisi (PreflightValidate :396).
        /// false (jalur Excel 396 D-05) = teks essay OPSIONAL — essay graded murni by EssayScore.</summary>
        public bool EssayTextRequired { get; set; } = true;

        public InjectCertMode CertMode { get; set; } = InjectCertMode.None;
        public int? LinkedGroupId { get; set; }
        public int? LinkedSessionId { get; set; }
        public List<InjectQuestionSpec> Questions { get; set; } = new();
        public List<InjectWorkerSpec> Workers { get; set; } = new();
    }

    /// <summary>Satu error per-baris (NIP) untuk D-03 reject-all.</summary>
    public class InjectRowError
    {
        public string Nip { get; set; } = "";
        public string Message { get; set; } = "";   // Bahasa Indonesia
    }

    /// <summary>Hasil inject batch.</summary>
    public class InjectResult
    {
        public bool Success { get; set; }            // true bila ter-commit (boleh ada skip)
        public bool Rejected { get; set; }            // true bila pre-flight tolak-semua (D-03)
        public List<int> SuccessSessionIds { get; set; } = new();
        public List<string> SkippedNips { get; set; } = new();   // duplikat di-skip (D-01)
        public List<InjectRowError> PerRowErrors { get; set; } = new();
        public string? Message { get; set; }          // ringkasan Bahasa Indonesia (mis. pesan rollback)
    }

    /// <summary>
    /// Phase 395 INJ-09/D-09 — Request dry-run preview skor 1 worker (pra-persist, NO cert#, NO write DB).
    /// Mode/TargetScore = INPUT auto-gen saja (D-02) — TIDAK pernah masuk <see cref="InjectRequest"/>/service.
    /// Controller resolve pola: mode=auto → BuildAutoGenAnswers(seed); mode=manual → Answers apa adanya.
    /// </summary>
    public class InjectPreviewRequest
    {
        public int PassPercentage { get; set; } = 70;
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime CompletedAt { get; set; }
        public string Nip { get; set; } = "";
        public string Mode { get; set; } = "manual";   // "manual" | "auto"
        public int TargetScore { get; set; }            // auto-gen only
        public List<InjectQuestionSpec> Questions { get; set; } = new();
        public List<InjectAnswerSpec> Answers { get; set; } = new();   // manual/essay manual; auto MC/MA dihitung server
    }

    /// <summary>
    /// Phase 395 INJ-09/D-09 — Hasil preview skor final (TANPA nomor sertifikat; engine sama dengan commit
    /// <see cref="HcPortal.Helpers.AssessmentScoreAggregator"/> → preview == commit).
    /// </summary>
    public class InjectPreviewResult
    {
        public int Percentage { get; set; }
        public bool IsPassed { get; set; }
        public int TotalScore { get; set; }
        public int MaxScore { get; set; }
        public int CeilingPercent { get; set; }       // auto-gen ceiling MC/MA-only (D-08.3)
        public bool TargetReachable { get; set; } = true;
        public int Overshoot { get; set; }            // actual - target (auto-gen; >=0 karena bias jamin-lulus)
        public bool Blocked { get; set; }             // true bila target>ceiling (BLOCKING, D-08.3)
        public string? BlockingMessage { get; set; }  // Bahasa Indonesia
    }

    /// <summary>Hasil POST UploadInjectExcel (396 D-08/D-09): ok=false + Errors (daftar LENGKAP, atomic)
    /// ATAU ok=true + AnswersJson (di-set ke #AnswersJson klien) + Previews (tabel pratinjau, NO cert#).</summary>
    public class InjectExcelUploadResult
    {
        public bool Ok { get; set; }
        public List<InjectRowError> Errors { get; set; } = new();
        public string AnswersJson { get; set; } = "";          // serialized List<InjectWorkerAnswersVM> (Mode="manual")
        public List<InjectExcelPreviewRow> Previews { get; set; } = new();
        public int SkippedBlankCount { get; set; }              // sel kosong di-skip → warn-but-allow (D-06)
    }

    /// <summary>1 baris tabel pratinjau batch (D-08): NIP+Nama+skor final+lulus+terjawab. TANPA nomor sertifikat.</summary>
    public class InjectExcelPreviewRow
    {
        public string Nip { get; set; } = "";
        public string Name { get; set; } = "";
        public int Percentage { get; set; }
        public bool IsPassed { get; set; }
        public int Answered { get; set; }
        public int TotalQuestions { get; set; }
    }
}
