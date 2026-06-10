using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Services
{
    /// <summary>Request bypass tahun (spec §5). Mode in-memory saja — TIDAK dipersistensikan (W-05).</summary>
    public record BypassRequest(
        string CoacheeId, int SourceProtonTrackId, int TargetProtonTrackId,
        string TargetUnit, string? TargetCoachId, string Reason, string Mode,  // "CL-A"|"CL-B(a)"|"CL-B(b)"|"CL-C"
        int? DurationMinutes, string InitiatedById);

    public record BypassResult(bool Success, string Message, int? PendingId = null, bool ShowAttachPackageReminder = false);

    /// <summary>
    /// Input validasi pure §5 — semua nilai SUDAH di-resolve caller (tanpa DB di predikat).
    /// SourceComplete = allApproved deliverable tahun asal; SourceHasFinal = penanda ProtonFinalAssessment ada.
    /// </summary>
    public record BypassValidationInput(string Reason, int ActiveSourceTrackId, int TargetTrackId,
        int SourceTahun, int TargetTahun, string Mode, bool SourceComplete, bool SourceHasFinal);

    /// <summary>
    /// Phase 360 (PBYP-02) — predikat MURNI validasi bypass §5. Tanpa DbContext/IO (pola ProtonYearGate).
    /// E8 (tepat 1 assignment aktif) butuh DB → dicek di ExecuteInstantBypassAsync + BypassSaveAsync (B-04),
    /// BUKAN di sini.
    /// </summary>
    public static class BypassValidator
    {
        private static readonly string[] ValidModes = { "CL-A", "CL-B(a)", "CL-B(b)", "CL-C" };

        public static (bool Valid, string Message) Validate(BypassValidationInput v)
        {
            if (string.IsNullOrWhiteSpace(v.Reason))
                return (false, "Alasan wajib diisi.");

            if (!ValidModes.Contains(v.Mode))
                return (false, "Mode bypass tidak dikenal.");

            // E14: target tidak boleh sama dengan track aktif source.
            if (v.TargetTrackId == v.ActiveSourceTrackId)
                return (false, "Target sama dengan track aktif.");

            // D-B: |Δtahun| ≤ 1 (naik/turun/lateral 1 langkah).
            if (Math.Abs(v.SourceTahun - v.TargetTahun) > 1)
                return (false, "Lompat tahun maksimal 1 langkah.");

            // B-03 (spec §4/§5): CL-A WAJIB allApproved DAN penanda final ada.
            if (v.Mode == "CL-A")
            {
                if (!v.SourceComplete)
                    return (false, "Tahun asal belum komplit, gunakan CL-B.");
                if (!v.SourceHasFinal)
                    return (false, "Penanda Lulus tahun asal belum terbit, gunakan CL-B(a).");
            }

            // D-D: CL-B(a)/(b) hanya bila final tahun asal BELUM ada.
            if ((v.Mode == "CL-B(a)" || v.Mode == "CL-B(b)") && v.SourceHasFinal)
                return (false, "Final tahun asal sudah ada.");

            return (true, "");
        }
    }
}
