using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Hubs;
using HcPortal.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HcPortal.Services
{
    /// <summary>
    /// Hasil ExecuteAsync — success + pesan error opsional (untuk PRG/redirect caller).
    /// </summary>
    public readonly record struct RetakeResult(bool Success, string? Error);

    /// <summary>
    /// v32.4 RTK-07/13 — mesin ujian ulang (Attempt/Retake) BERSAMA, dipanggil dua jalur:
    /// HC <c>ResetAssessment</c> (plan 405-04) dan worker self-service (Phase 407).
    ///
    /// <para><b>Urutan WAJIB (3 koreksi vs <c>ResetAssessment</c> existing):</b>
    /// (1) <b>CLAIM-ATOMIK DULU</b> (anti double-archive, Pitfall 1): <c>ExecuteUpdateAsync</c>
    /// <c>WHERE Status NOT IN (Cancelled, Open)</c> → <c>rows==0</c> ⇒ abort SEBELUM menyentuh archive.
    /// <c>Status != "Open"</c> mencegah re-claim sesi yang sudah di-Open oleh request pertama (double-click).
    /// (2) <b>SNAPSHOT per-soal SEBELUM delete</b> via <see cref="RetakeArchiveBuilder.Build"/> (hanya bila
    /// sesi sebelumnya Completed). (3) counting <c>(UserId, Title, Category)</c> + snapshot-presence (D-01).</para>
    ///
    /// <para><b>TempData token clear (must-fix #1)</b> BUKAN tanggung jawab service (HTTP-scoped) — caller WAJIB
    /// <c>TempData.Remove($"TokenVerified_{id}")</c> setelah ExecuteAsync sukses (controller plan 405-04 / Phase 407).</para>
    /// </summary>
    public class RetakeService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLog;
        private readonly IHubContext<AssessmentHub> _hubContext;
        private readonly ILogger<RetakeService> _logger;

        public RetakeService(
            ApplicationDbContext context,
            AuditLogService auditLog,
            IHubContext<AssessmentHub> hubContext,
            ILogger<RetakeService> logger)
        {
            _context = context;
            _auditLog = auditLog;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Jalankan ujian ulang/reset untuk satu sesi. Urutan: claim-atomik → snapshot+archive (jika Completed)
        /// → delete live → audit → SignalR <c>sessionReset {{ reason }}</c>.
        /// </summary>
        /// <param name="sessionId">Id <see cref="AssessmentSession"/> target.</param>
        /// <param name="actorUserId">Id aktor (HC/worker) untuk audit.</param>
        /// <param name="actorName">Nama tampil aktor (NIP - Nama) untuk audit.</param>
        /// <param name="actionType">"RetakeAssessment" (worker) | "ResetAssessment" (HC) — must-fix #6.</param>
        /// <param name="reason">Alasan parameterized untuk SignalR payload (Pitfall 7) + deskripsi audit.</param>
        public async Task<RetakeResult> ExecuteAsync(int sessionId, string actorUserId, string actorName, string actionType, string reason)
        {
            // 1. Load sesi target.
            var assessment = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == sessionId);
            if (assessment == null)
                return new RetakeResult(false, "Sesi tidak ditemukan.");

            // Tangkap status SEBELUM claim — menentukan apakah perlu archive snapshot.
            bool wasCompleted = assessment.Status == "Completed";

            // 2. CLAIM-ATOMIK DULU (anti double-archive — Pitfall 1).
            //    WHERE Status NOT IN (Cancelled, Open): Cancelled = final tak resettable;
            //    Open = sudah di-claim oleh request lain (cegah double-click double-archive).
            var rows = await _context.AssessmentSessions
                .Where(s => s.Id == sessionId && s.Status != "Cancelled" && s.Status != "Open")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, "Open")
                    .SetProperty(r => r.Score, (int?)null)
                    .SetProperty(r => r.IsPassed, (bool?)null)
                    .SetProperty(r => r.Progress, 0)
                    .SetProperty(r => r.StartedAt, (DateTime?)null)
                    .SetProperty(r => r.CompletedAt, (DateTime?)null)
                    .SetProperty(r => r.ElapsedSeconds, (int)0)
                    .SetProperty(r => r.LastActivePage, (int?)null)
                    .SetProperty(r => r.UpdatedAt, DateTime.UtcNow));

            if (rows == 0)
                return new RetakeResult(false, "Sesi tidak dapat direset (sudah dibatalkan atau sudah terbuka).");

            // 3. Jika sesi sebelumnya Completed: SNAPSHOT per-soal SEBELUM delete (D-04 retain).
            if (wasCompleted)
            {
                // Counting era-retake (UserId, Title, Category) — must-fix #3 anti-konflasi Pre/Post +
                // D-01 snapshot-presence (arsip ber-child saja; legacy HC-reset natural-excluded).
                int eraRetakeArchives = await _context.AssessmentAttemptHistory
                    .Where(h => h.UserId == assessment.UserId
                             && h.Title == assessment.Title
                             && h.Category == assessment.Category
                             && _context.AssessmentAttemptResponseArchives.Any(a => a.AttemptHistoryId == h.Id))
                    .CountAsync();

                var attemptHistory = new AssessmentAttemptHistory
                {
                    SessionId     = assessment.Id,
                    UserId        = assessment.UserId,
                    Title         = assessment.Title ?? "",
                    Category      = assessment.Category ?? "",
                    Score         = assessment.Score,
                    IsPassed      = assessment.IsPassed,
                    StartedAt     = assessment.StartedAt,
                    CompletedAt   = assessment.CompletedAt,
                    AttemptNumber = eraRetakeArchives + 1,   // A1/D-01: era-retake count (bukan termasuk HC-reset legacy)
                    ArchivedAt    = DateTime.UtcNow
                };
                _context.AssessmentAttemptHistory.Add(attemptHistory);
                await _context.SaveChangesAsync();   // assign attemptHistory.Id sebelum builder snapshot

                // Muat soal (urutan beku via assignment shuffle) + responses, lalu Build SEBELUM RemoveRange.
                var assignment = await _context.UserPackageAssignments
                    .FirstOrDefaultAsync(a => a.AssessmentSessionId == sessionId);
                var questionIds = assignment?.GetShuffledQuestionIds() ?? new System.Collections.Generic.List<int>();

                var questions = await _context.PackageQuestions
                    .Include(q => q.Options)
                    .Where(q => questionIds.Contains(q.Id))
                    .ToListAsync();

                var responses = await _context.PackageUserResponses
                    .Where(r => r.AssessmentSessionId == sessionId)
                    .ToListAsync();

                if (questions.Count > 0)
                {
                    var snapshot = RetakeArchiveBuilder.Build(attemptHistory.Id, questions, responses);
                    if (snapshot.Count > 0)
                        _context.AssessmentAttemptResponseArchives.AddRange(snapshot);
                }
            }

            // 4. DELETE live (mirror ResetAssessment existing :4262-4282) — SETELAH snapshot ter-stage.
            var packageResponses = await _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == sessionId)
                .ToListAsync();
            if (packageResponses.Any())
                _context.PackageUserResponses.RemoveRange(packageResponses);

            var liveAssignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == sessionId);
            if (liveAssignment != null)
                _context.UserPackageAssignments.Remove(liveAssignment);

            // #22 D-07: bersihkan ET scores stale agar retake regenerasi ET BARU (anti unique-index violation).
            var etScores = await _context.SessionElemenTeknisScores
                .Where(e => e.AssessmentSessionId == sessionId)
                .ToListAsync();
            if (etScores.Any())
                _context.SessionElemenTeknisScores.RemoveRange(etScores);

            await _context.SaveChangesAsync();   // flush snapshot AddRange + semua delete dalam satu batch

            // 5. AUDIT (try/catch warn-only — gagal audit tak boleh batalkan reset yang sudah commit).
            try
            {
                await _auditLog.LogAsync(
                    actorUserId,
                    actorName,
                    actionType,
                    $"{actionType} assessment '{assessment.Title}' for user {assessment.UserId} [ID={sessionId}] (reason={reason})",
                    sessionId,
                    "AssessmentSession");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RetakeService: gagal tulis audit untuk session {SessionId} (reset tetap berhasil)", sessionId);
            }

            // 6. SignalR — reason PARAMETERIZED (Pitfall 7; client StartExam.cshtml abaikan reason di 405, backward-compat aman).
            try
            {
                await _hubContext.Clients.User(assessment.UserId).SendAsync("sessionReset", new { reason });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RetakeService: gagal broadcast sessionReset untuk user {UserId} (reset tetap berhasil)", assessment.UserId);
            }

            return new RetakeResult(true, null);
        }

        /// <summary>
        /// Apakah sesi ini layak untuk ujian ulang (worker path). Membungkus pure
        /// <see cref="RetakeRules.CanRetake"/> dengan counting DB-aware: <c>attemptsUsed = eraRetakeArchives + 1</c>
        /// dimana eraRetakeArchives = jumlah <see cref="AssessmentAttemptHistory"/> ber-child
        /// <see cref="AssessmentAttemptResponseArchive"/> (D-01 snapshot-presence) untuk
        /// <c>(UserId, Title, Category)</c> (must-fix #3). Arsip HC-reset legacy (tanpa snapshot) TIDAK menghitung cap.
        /// </summary>
        public async Task<bool> CanRetakeAsync(int sessionId)
        {
            var s = await _context.AssessmentSessions.FirstOrDefaultAsync(a => a.Id == sessionId);
            if (s == null) return false;

            int eraRetakeArchives = await _context.AssessmentAttemptHistory
                .Where(h => h.UserId == s.UserId
                         && h.Title == s.Title
                         && h.Category == s.Category
                         && _context.AssessmentAttemptResponseArchives.Any(a => a.AttemptHistoryId == h.Id))
                .CountAsync();

            return RetakeRules.CanRetake(
                s.AllowRetake, s.AssessmentType, s.IsManualEntry, s.Status, s.IsPassed,
                attemptsUsed: eraRetakeArchives + 1, s.MaxAttempts, s.RetakeCooldownHours,
                s.CompletedAt, DateTime.UtcNow);
        }
    }
}
