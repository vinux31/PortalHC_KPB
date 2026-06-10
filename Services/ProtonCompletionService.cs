using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Services
{
    /// <summary>
    /// Phase 358 (PCOMP-03) — sumber TUNGGAL pembuatan/penghapusan penanda kelulusan Proton
    /// (<see cref="ProtonFinalAssessment"/>). Mengekstrak inline create+dedup yang sebelumnya
    /// duplikat di AssessmentAdminController.SubmitInterviewResults (L3742-3765).
    ///
    /// Tiga jalur penanda dibedakan kolom <c>Origin</c>: "Exam" | "Interview" | "Bypass".
    /// Bypass (Phase 360) reuse helper yang sama → itu sebab <see cref="RemoveExamOriginAsync"/>
    /// selektif Exam-only (A-M9): re-grade exam TIDAK boleh hapus penanda Interview/Bypass.
    ///
    /// Helper murni — TANPA gate/blok eligibility (gate = Phase 359, D-02).
    /// </summary>
    public class ProtonCompletionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProtonCompletionService> _logger;

        public ProtonCompletionService(
            ApplicationDbContext context,
            ILogger<ProtonCompletionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Idempotent: terbitkan penanda kelulusan untuk assignment aktif (CoacheeId+ProtonTrackId+IsActive).
        /// Return false bila tidak ada assignment aktif, atau penanda sudah ada (tidak duplikat).
        /// Return true bila penanda baru dibuat.
        /// </summary>
        public async Task<bool> EnsureAsync(string coacheeId, int protonTrackId, string createdById, string origin, string? notes)
        {
            var assignment = await _context.ProtonTrackAssignments
                .FirstOrDefaultAsync(a => a.CoacheeId == coacheeId && a.ProtonTrackId == protonTrackId && a.IsActive);
            if (assignment == null)
            {
                _logger.LogWarning("ProtonCompletion.EnsureAsync: tidak ada assignment aktif untuk Coachee={CoacheeId} Track={TrackId} (Origin={Origin}). Penanda tidak dibuat.", coacheeId, protonTrackId, origin);
                return false;
            }

            var exists = await _context.ProtonFinalAssessments
                .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignment.Id);
            if (exists) return false;

            _context.ProtonFinalAssessments.Add(new ProtonFinalAssessment
            {
                CoacheeId = coacheeId,
                CreatedById = createdById,
                ProtonTrackAssignmentId = assignment.Id,
                Status = "Completed",
                CompetencyLevelGranted = 0, // dormant (A-3) — penanda Lulus murni
                Origin = origin,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Re-grade selektif (A-M9): hapus HANYA penanda <c>Origin=="Exam"</c> untuk assignment aktif cocok.
        /// Penanda Interview/Bypass KEBAL. Return true bila ada yang dihapus.
        /// </summary>
        public async Task<bool> RemoveExamOriginAsync(string coacheeId, int protonTrackId)
        {
            var assignmentIds = await _context.ProtonTrackAssignments
                .Where(a => a.CoacheeId == coacheeId && a.ProtonTrackId == protonTrackId && a.IsActive)
                .Select(a => a.Id)
                .ToListAsync();
            if (assignmentIds.Count == 0) return false;

            var penanda = await _context.ProtonFinalAssessments
                .Where(fa => assignmentIds.Contains(fa.ProtonTrackAssignmentId) && fa.Origin == "Exam")
                .ToListAsync();
            if (penanda.Count == 0) return false;

            _context.ProtonFinalAssessments.RemoveRange(penanda);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Query murni: daftar TahunKe yang sudah ber-penanda untuk (coacheeId, trackType).
        /// TANPA gate/enforce apa pun (D-02 — gate berurutan = Phase 359). Dipakai backfill/future.
        /// </summary>
        public async Task<List<string>> GetPassedYearsAsync(string coacheeId, string trackType)
        {
            return await (from fa in _context.ProtonFinalAssessments
                          join asg in _context.ProtonTrackAssignments on fa.ProtonTrackAssignmentId equals asg.Id
                          join trk in _context.ProtonTracks on asg.ProtonTrackId equals trk.Id
                          where fa.CoacheeId == coacheeId && trk.TrackType == trackType
                          select trk.TahunKe)
                         .Distinct()
                         .ToListAsync();
        }
    }

    /// <summary>
    /// Phase 359 (PCOMP-07) — predikat MURNI gate antar-tahun Proton. Tanpa DbContext/IO.
    /// "Tahun N-1 lulus" = TahunKe N-1 ada di daftar passedYears (penanda ProtonFinalAssessment),
    /// BUKAN sekadar deliverable Approved (D-03). Tahun 1 (prevTahunKe == null) selalu diizinkan.
    /// </summary>
    public static class ProtonYearGate
    {
        /// <param name="prevTahunKe">TahunKe tahun sebelumnya (mis. "Tahun 1"); null = tidak ada prasyarat (Tahun 1).</param>
        /// <param name="passedYears">Daftar TahunKe yang sudah ber-penanda (hasil GetPassedYearsAsync).</param>
        public static bool IsAllowed(string? prevTahunKe, IEnumerable<string>? passedYears)
        {
            if (string.IsNullOrWhiteSpace(prevTahunKe)) return true; // Tahun 1 — no prereq (D-03)
            if (passedYears == null) return false;
            var needle = prevTahunKe.Trim();
            return passedYears.Any(y => !string.IsNullOrWhiteSpace(y) && y.Trim() == needle);
        }
    }
}
