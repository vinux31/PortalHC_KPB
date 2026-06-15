namespace HcPortal.Models
{
    public class MonitoringGroupViewModel
    {
        public int RepresentativeId { get; set; }
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime Schedule { get; set; }          // representative Schedule (with time)
        public string GroupStatus { get; set; } = "";   // "Open", "Upcoming", or "Closed"
        public int TotalCount { get; set; }             // all sessions in this group
        public int CompletedCount { get; set; }         // sessions where IsCompleted == true
        public int PassedCount { get; set; }            // sessions where IsPassed == true
        public bool IsPackageMode { get; set; }         // true when the assessment group has packages attached
        public int PendingCount { get; set; }           // count of "Not started" sessions (for Reshuffle All confirmation dialog)
        public int CancelledCount { get; set; }          // count of "Cancelled" sessions (bulk close: not-started workers)
        public int InProgressCount { get; set; }         // count of "InProgress" sessions
        public bool IsTokenRequired { get; set; }
        public string AccessToken { get; set; } = "";
        public List<MonitoringSessionViewModel> Sessions { get; set; } = new();

        // Essay grading badge support (Phase 298-05)
        public int MenungguPenilaianCount { get; set; }
        public int AbandonedCount { get; set; }          // MAP-10: count "Abandoned" sessions (summary card, Total = sum invariant)
        public int EssayPendingTotal { get; set; }

        // Pre-Post Test support (Phase 297)
        public bool IsPrePostGroup { get; set; } = false;
        public int? LinkedGroupId { get; set; }
        public MonitoringSubRowViewModel? PreSubRow { get; set; }
        public MonitoringSubRowViewModel? PostSubRow { get; set; }
    }

    public class MonitoringSubRowViewModel
    {
        public int RepresentativeId { get; set; }
        public string Phase { get; set; } = "";  // "PreTest" atau "PostTest"
        public DateTime Schedule { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public int PassedCount { get; set; }
        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int CancelledCount { get; set; }
        public string GroupStatus { get; set; } = "";
    }

    public class MonitoringSessionViewModel
    {
        public int Id { get; set; }
        public string UserFullName { get; set; } = "";
        public string UserNIP { get; set; } = "";
        public string UserStatus { get; set; } = "";    // "Not started", "In Progress", "Abandoned", or "Completed"
        public int? Score { get; set; }
        public bool? IsPassed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public int QuestionCount { get; set; }             // number of questions assigned to this session
        public int DurationMinutes { get; set; }            // exam duration from AssessmentSession (0 = no timer / interview mode)

        // Essay grading support (Phase 298-05)
        public bool HasManualGrading { get; set; }
        public int EssayPendingCount { get; set; }

        // Phase 310 D-02 — gate button finalize berdasarkan Status assessment session
        public string Status { get; set; } = "";               // mirror AssessmentSession.Status (raw value, BUKAN UserStatus yang sudah remap ke "Not started"/"InProgress"/"Completed"/"Dibatalkan")
        public string? NomorSertifikat { get; set; }           // mirror AssessmentSession.NomorSertifikat (nullable)
    }

    /// <summary>
    /// ViewModel for a single Essay question in the grading UI (Phase 298-05)
    /// </summary>
    public class EssayGradingItemViewModel
    {
        public int QuestionId { get; set; }
        public int DisplayNumber { get; set; }
        public string QuestionText { get; set; } = "";
        public string? Rubrik { get; set; }
        public string? TextAnswer { get; set; }
        public int? EssayScore { get; set; }
        public int ScoreValue { get; set; }

        // RND-05: gambar SOAL saja (essay tak punya opsi). Diisi controller dari PackageQuestion.ImagePath/ImageAlt.
        public string? ImagePath { get; set; }
        public string? ImageAlt { get; set; }
    }

    /// <summary>
    /// ViewModel page penilaian essay per-worker (Phase 384 UIG-02/03). Membungkus identitas single session +
    /// essay items + flag finalized (D-10) + 4 nav param untuk back-link ke AssessmentMonitoringDetail (Pitfall 3).
    /// </summary>
    public class EssayGradingPageViewModel
    {
        public int SessionId { get; set; }
        public string UserFullName { get; set; } = "";
        public string UserNIP { get; set; } = "";
        public int EssayPendingCount { get; set; }
        public bool IsFinalized { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<EssayGradingItemViewModel> EssayItems { get; set; } = new();
        // 4 nav param back-link (tz-safe: ScheduleDate string yyyy-MM-dd)
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string ScheduleDate { get; set; } = "";
        public string? AssessmentType { get; set; }
    }
}
