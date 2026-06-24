using System.ComponentModel.DataAnnotations;

namespace HcPortal.Models
{
    public class AssessmentSession
    {
        public int Id { get; set; }
        
        // Foreign Key to User
        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }
        
        public string Title { get; set; } = "";

        // Kategori utama: "Assessment OJ", "IHT", "Licencor", "OTS", "Mandatory HSSE Training"
        public string Category { get; set; } = "";

        public DateTime Schedule { get; set; }
        public int DurationMinutes { get; set; }
        public string Status { get; set; } = "";
        // Nilai kanonik (lihat AssessmentConstants.AssessmentStatus): Open, Upcoming, Completed,
        // "Menunggu Penilaian" (PendingGrading), InProgress, Cancelled, Abandoned (7 status).

        // New Visualization Props
        public int Progress { get; set; } = 0; // 0 - 100
        public string BannerColor { get; set; } = "bg-primary"; // Bootstrap color class or hex

        public int? Score { get; set; }

        [Range(0, 100)]
        [Display(Name = "Pass Percentage (%)")]
        public int PassPercentage { get; set; } = 70;

        [Display(Name = "Allow Answer Review")]
        public bool AllowAnswerReview { get; set; } = true;

        [Display(Name = "Terbitkan Sertifikat")]
        public bool GenerateCertificate { get; set; } = false;

        [Display(Name = "Acak Soal")]
        public bool ShuffleQuestions { get; set; } = true;

        [Display(Name = "Acak Pilihan Jawaban")]
        public bool ShuffleOptions { get; set; } = true;

        // ===== v32.4 Retake config (mirror Shuffle pattern) =====
        [Display(Name = "Izinkan Ujian Ulang")]
        public bool AllowRetake { get; set; } = false;

        [Range(1, 5)]
        [Display(Name = "Maksimal Percobaan")]
        public int MaxAttempts { get; set; } = 2;

        [Range(0, 168)] // 0 = tanpa jeda; cap 1 minggu
        [Display(Name = "Jeda Ujian Ulang (jam)")]
        public int RetakeCooldownHours { get; set; } = 24;

        public bool? IsPassed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Total seconds the worker has actively spent in the exam (excludes offline time).
        /// Updated on each page navigation and every 30 seconds via frontend polling.
        /// Default 0 on session start.
        /// </summary>
        public int ElapsedSeconds { get; set; } = 0;

        /// <summary>
        /// Last page (0-based index) the worker was viewing before disconnect.
        /// Null = never navigated (still on page 0). Used to resume on correct page.
        /// </summary>
        public int? LastActivePage { get; set; }

        /// <summary>
        /// Optional hard cutoff date for this exam window. Workers cannot start (or restart) the exam
        /// after this date. Null = no expiry enforced.
        /// </summary>
        public DateTime? ExamWindowCloseDate { get; set; }

        /// <summary>
        /// Optional certificate expiry date. Null = certificate has no expiry.
        /// Only relevant when GenerateCertificate = true.
        /// Phase 327 — DateOnly migrasi P04 (eliminasi tz drift permanen).
        /// </summary>
        [Display(Name = "Berlaku Sampai")]   // CLN-01 (FLD-5.2-06): satu sumber label; selaraskan cshtml ke istilah ini
        public DateOnly? ValidUntil { get; set; }

        /// <summary>
        /// Auto-generated certificate number in format KPB/{SEQ}/{ROMAN-MONTH}/{YEAR}.
        /// Generated at assessment creation time. Null for sessions created before Phase 192.
        /// UNIQUE constraint enforced at DB level (partial index excludes nulls).
        /// </summary>
        public string? NomorSertifikat { get; set; }

        public bool IsTokenRequired { get; set; }

        /// <summary>
        /// Token akses untuk masuk ke sesi ujian.
        /// DESAIN DISENGAJA: Token ini di-share ke semua peserta dalam satu batch ujian yang sama
        /// (common exam room pattern). Ini bukan security vulnerability — peserta ujian memang
        /// berada di ruangan yang sama dan mendapat token yang sama dari pengawas.
        /// Token hanya mengontrol akses masuk, bukan identitas peserta (identity ditangani ASP.NET Core Identity).
        /// </summary>
        public string AccessToken { get; set; } = "";

        /// <summary>
        /// EXSEC-01 (Phase 427): timestamp server-authoritative saat token ujian terverifikasi (VerifyToken).
        /// null = belum verifikasi. Di-reset null saat retake/reset (RetakeService.ExecuteAsync) → gate re-arm.
        /// Menggantikan TempData token gate yang rapuh round-trip.
        /// </summary>
        public DateTime? TokenVerifiedAt { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }

        // ===== Phase 53: Proton Assessment Exam =====
        /// <summary>
        /// FK to ProtonTrack (nullable). Only set when Category == "Assessment Proton".
        /// No EF navigation property — avoids cascade complications; query ProtonTracks separately when needed.
        /// </summary>
        public int? ProtonTrackId { get; set; }

        /// <summary>
        /// Proton year: "Tahun 1", "Tahun 2", or "Tahun 3". Only set when Category == "Assessment Proton".
        /// Tahun 1-2 = online multiple-choice exam (existing engine). Tahun 3 = offline interview (HC inputs manually).
        /// </summary>
        public string? TahunKe { get; set; }

        /// <summary>
        /// JSON-serialized InterviewResultsDto. Only populated for Tahun 3 sessions after HC inputs interview results.
        /// Null until HC submits via SubmitInterviewResults action.
        /// </summary>
        public string? InterviewResultsJson { get; set; }

        // ===== Phase 200: Renewal Chain FKs =====
        /// <summary>
        /// FK ke AssessmentSession lain yang di-renew oleh session ini.
        /// Nullable. Hanya salah satu dari RenewsSessionId/RenewsTrainingId yang boleh diisi.
        /// CATATAN (PA-04): FK dikonfigurasi DeleteBehavior.NoAction (BUKAN ON DELETE SET NULL —
        /// SQL Server memblokir SET NULL pada cross-FK siklus). Null-clear saat sertifikat asal
        /// dihapus dilakukan di level aplikasi (ApplicationDbContext.cs:243-246).
        /// </summary>
        public int? RenewsSessionId { get; set; }

        /// <summary>
        /// FK ke TrainingRecord yang di-renew oleh session ini.
        /// Nullable. Hanya salah satu dari RenewsSessionId/RenewsTrainingId yang boleh diisi.
        /// CATATAN (PA-04): FK dikonfigurasi DeleteBehavior.NoAction (BUKAN ON DELETE SET NULL —
        /// SQL Server memblokir SET NULL pada cross-FK siklus). Null-clear saat sertifikat asal
        /// dihapus dilakukan di level aplikasi (ApplicationDbContext.cs:248-251).
        /// </summary>
        public int? RenewsTrainingId { get; set; }

        // Legacy navigation properties (AssessmentQuestion, UserResponse) removed in Phase 227 (CLEN-02).

        // ===== Manual Entry fields =====
        /// <summary>
        /// True jika session ini di-input manual oleh HC (bukan online assessment).
        /// </summary>
        public bool IsManualEntry { get; set; } = false;

        /// <summary>
        /// URL file sertifikat yang di-upload untuk assessment manual.
        /// </summary>
        public string? ManualSertifikatUrl { get; set; }

        /// <summary>Penyelenggara assessment manual (institusi/vendor).</summary>
        public string? Penyelenggara { get; set; }

        /// <summary>Kota tempat assessment dilaksanakan.</summary>
        public string? Kota { get; set; }

        /// <summary>Sub-kategori assessment (misal: Welding Inspector, K3 Umum).</summary>
        public string? SubKategori { get; set; }

        /// <summary>Tipe sertifikat: Kompetensi, Profesi, Pelatihan, dll.</summary>
        public string? CertificateType { get; set; }

        // ===== v14.0 Assessment Enhancement columns =====
        /// <summary>
        /// Tipe assessment (kolom DB): 'PreTest', 'PostTest', 'Standard', 'Manual', atau null = tidak ditentukan (backward compat).
        /// Digunakan untuk linking pre-post test pair dan grading logic.
        /// Catatan (FORM-10): JANGAN rancukan dengan penanda mode form 'CreationMode' (Standard/PrePostTest)
        /// — yang itu hanya parameter input wizard Create, bukan kolom tersimpan.
        /// </summary>
        public string? AssessmentType { get; set; }

        /// <summary>
        /// RESERVED — tidak dipakai. Dideklarasikan di v14 (AddAssessmentV14Columns) untuk konsep
        /// 'Phase1'/'Phase2' yang TIDAK PERNAH diimplementasikan. Linking Pre/Post nyata bertumpu pada
        /// AssessmentType + LinkedGroupId + LinkedSessionId. 0 referensi di app (FLOW-06). Dipertahankan
        /// di skema (kolom nullable, aman) untuk hindari migration destruktif. Jangan baca/tulis.
        /// </summary>
        public string? AssessmentPhase { get; set; }

        /// <summary>
        /// FK ke grup assessment (jika session ini bagian dari grup ujian terkait).
        /// Null = session berdiri sendiri, tidak terhubung ke grup.
        /// </summary>
        public int? LinkedGroupId { get; set; }

        /// <summary>
        /// FK ke AssessmentSession lain yang terhubung (misal: PreTest terhubung ke PostTest-nya).
        /// CATATAN (PA-04): TIDAK ada FK cascade terkonfigurasi di DB. Null-clear saat pasangan
        /// dihapus dilakukan di level aplikasi — RecordCascadeDeleteService.cs:235-237 (Delta #8).
        /// </summary>
        public int? LinkedSessionId { get; set; }

        /// <summary>
        /// True jika session ini memiliki soal Essay yang butuh grading manual oleh HC.
        /// Default false — hanya true jika ada soal Essay dalam package.
        /// </summary>
        public bool HasManualGrading { get; set; } = false;

        /// <summary>
        /// True jika Post-Test ini menggunakan paket soal yang sama dengan Pre-Test linked-nya.
        /// Hanya relevan untuk session dengan AssessmentType == "PostTest".
        /// Saat true, ManagePackages di-lock dan package otomatis disalin dari Pre-Test.
        /// </summary>
        public bool SamePackage { get; set; } = false;

        // ===== Phase 302: Extra Time Accessibility =====
        /// <summary>
        /// Akumulasi extra time (menit) yang ditambahkan HC untuk sesi ini.
        /// Null = tidak ada extra time. Nilai ini ditambahkan ke DurationMinutes
        /// saat menghitung timer server-side dan pada update timer real-time via SignalR.
        /// </summary>
        public int? ExtraTimeMinutes { get; set; }
    }
}