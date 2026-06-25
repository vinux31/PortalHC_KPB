using System;

namespace HcPortal.Helpers
{
    /// <summary>
    /// v32.4 RTK-03/13 — keputusan kelayakan ujian ulang (Attempt/Retake) yang PURE (EF-free, sinkron).
    /// Dipakai DI DUA TEMPAT (RetakeService plan 405-03 + Phase 407 ViewModel/controller) sehingga
    /// keputusan tak divergen — pola kill-drift mirip <see cref="ShuffleToggleRules"/>.
    ///
    /// <para><b>Pure by design:</b> caller menyuplai FAKTA — termasuk <paramref name="attemptsUsed"/>.
    /// Counting era-retake yang DB-aware (D-01: hanya arsip ber-snapshot yang menghitung cap, arsip
    /// HC-reset legacy pre-v32.4 natural-excluded) hidup di <c>RetakeService.CanRetakeAsync</c> (plan 405-03),
    /// BUKAN di sini. Ini menjaga <see cref="CanRetake"/> unit-testable di SEMUA cabang.</para>
    ///
    /// <para><b>Waktu UTC:</b> <c>completedAt</c> dan <c>nowUtc</c> diasumsikan UTC. <c>nowUtc</c>
    /// di-inject (bukan <c>DateTime.UtcNow</c> internal) untuk determinisme test cooldown.</para>
    /// </summary>
    public static class RetakeRules
    {
        /// <summary>
        /// True HANYA bila semua syarat ujian ulang terpenuhi. Urutan guard (fail-fast → false):
        /// allowRetake OFF → !PreTest (D6 diagnostik) → !ManualEntry (RTK-13 inject tak retakeable) →
        /// status=="Completed" (exclude InProgress/Abandoned/Cancelled/Open) →
        /// isPassed==false (null=PendingGrading &amp; true=Lulus → tak eligible) →
        /// attemptsUsed&lt;maxAttempts (D7 cap) → cooldown lewat.
        /// Cooldown: <paramref name="retakeCooldownHours"/> &lt;= 0 → tak ada jeda (true);
        /// jika <paramref name="completedAt"/> null → false; else nowUtc &gt;= completedAt + jam.
        /// </summary>
        public static bool CanRetake(
            bool allowRetake,
            string? assessmentType,
            bool isManualEntry,
            string status,
            bool? isPassed,
            int attemptsUsed,
            int maxAttempts,
            int retakeCooldownHours,
            DateTime? completedAt,
            DateTime nowUtc,
            DateTime? examWindowCloseDate)
        {
            if (!allowRetake) return false;
            if (assessmentType == "PreTest") return false;          // D6 — diagnostik tak retakeable
            if (isManualEntry) return false;                        // RTK-13 — hasil inject tak retakeable
            if (status != "Completed") return false;                // exclude InProgress/Abandoned/Cancelled/Open
            if (isPassed != false) return false;                    // null=PendingGrading & true=Lulus → tak eligible
            if (attemptsUsed >= maxAttempts) return false;          // D7 — cap percobaan habis

            // v32.7 RTH-01 (RTK-LOGIC-02 HIGH) — window gate: retake mustahil bila masa ujian tutup.
            // SEBELUM cooldown (window = hard-close fundamental). +7h WIB byte-identik StartExam (CMPController:956).
            // EWCD null → tak ada gate (backward-compat sesi tanpa window).
            if (examWindowCloseDate.HasValue && nowUtc.AddHours(7) > examWindowCloseDate.Value) return false;

            // Cooldown
            if (retakeCooldownHours <= 0) return true;              // 0 = no jeda
            if (completedAt == null) return false;                  // butuh basis waktu untuk hitung jeda
            return nowUtc >= completedAt.Value.AddHours(retakeCooldownHours);
        }

        /// <summary>
        /// Sembunyikan toggle "Izinkan Ujian Ulang" untuk PreTest ATAU Manual entry.
        /// (Mirror <see cref="ShuffleToggleRules.ShouldHideShuffleToggle"/> — tapi Proton TETAP retakeable,
        /// beda dari shuffle yang sembunyi untuk Proton Tahun 3.)
        /// </summary>
        public static bool ShouldHideRetakeToggle(string? assessmentType, bool isManualEntry)
            => assessmentType == "PreTest" || isManualEntry;

        /// <summary>
        /// v32.7 RTH-01/D-02 — peringatan dini HC: apakah masa jeda (cooldown) bisa mendorong
        /// eligibility ujian ulang MELEWATI batas tutup ujian (ExamWindowCloseDate)? PURE, EF-free.
        /// +7h WIB hidup di SATU tempat (byte-identik StartExam CMPController:956) supaya controller &amp; test
        /// memanggil kode yang SAMA (kill-drift — drift +7h→+8h pasti ketahuan test). NON-blocking warning only.
        /// False bila: tak ada window, cooldown ≤ 0, atau window SUDAH tutup (kasus itu = gate D-01, bukan D-02).
        /// </summary>
        public static bool CooldownMayExceedWindow(DateTime nowUtc, DateTime? examWindowCloseDate, int retakeCooldownHours)
        {
            if (examWindowCloseDate == null) return false;
            if (retakeCooldownHours <= 0) return false;
            var nowWib = nowUtc.AddHours(7);                              // +7h WIB verbatim — JANGAN +8h/DateTime.Now/TimeZoneInfo
            if (nowWib > examWindowCloseDate.Value) return false;        // window sudah tutup → urusan gate D-01
            return nowWib.AddHours(retakeCooldownHours) > examWindowCloseDate.Value;
        }

        /// <summary>
        /// v32.4 RTK-11 (Phase 407) — tier feedback PURE 3-state, leak-safe.
        /// LOCKED orchestrator (A1): sembunyikan kunci selama retake MASIH MUNGKIN — yaitu belum-lulus
        /// (isPassed != true: failed ATAU pending null) DAN attemptsRemaining → ShowWrongFlagsOnly.
        /// Pending (null) diperlakukan SAMA dengan failed: bisa transisi ke failed+retake, jadi kunci
        /// yang tampil saat pending akan bocor untuk retake soal yang sama (D-03). Hanya saat lulus, atau
        /// belum-lulus tapi attempt habis, kunci boleh tampil (ShowFullReview).
        /// Caller pakai assessment.IsPassed (bool?) BUKAN VM.IsPassed (bool non-nullable) — Pitfall 5.
        /// </summary>
        public static RetakeReviewMode ResolveReviewMode(bool allowAnswerReview, bool? isPassed, bool attemptsRemaining)
        {
            if (!allowAnswerReview) return RetakeReviewMode.ShowScoreOnly;
            // belum-lulus (failed ATAU pending) & retake masih mungkin → tahan kunci
            if (isPassed != true && attemptsRemaining) return RetakeReviewMode.ShowWrongFlagsOnly;
            return RetakeReviewMode.ShowFullReview;   // passed | (belum-lulus & exhausted) — retake tak mungkin lagi
        }
    }

    /// <summary>
    /// v32.4 RTK-11 (Phase 407) — 3-state tier feedback hasil <see cref="RetakeRules.ResolveReviewMode"/>.
    /// View Results.cshtml (407-03) men-suppress leak-site berdasar nilai ini (server-authoritative).
    /// </summary>
    public enum RetakeReviewMode { ShowFullReview, ShowWrongFlagsOnly, ShowScoreOnly }
}
