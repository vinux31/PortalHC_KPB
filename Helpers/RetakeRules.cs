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
            DateTime nowUtc)
        {
            if (!allowRetake) return false;
            if (assessmentType == "PreTest") return false;          // D6 — diagnostik tak retakeable
            if (isManualEntry) return false;                        // RTK-13 — hasil inject tak retakeable
            if (status != "Completed") return false;                // exclude InProgress/Abandoned/Cancelled/Open
            if (isPassed != false) return false;                    // null=PendingGrading & true=Lulus → tak eligible
            if (attemptsUsed >= maxAttempts) return false;          // D7 — cap percobaan habis

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
    }
}
