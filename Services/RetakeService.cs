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
    /// <para><b>Review 405 fixes:</b>
    /// WR-01 — reset sesi ber-status <c>Open</c> (assigned-not-started / baru di-reset) = <b>SUCCESS no-op</b>
    /// (selaras controller <c>ResetAssessment</c> yang mengizinkan status Open), bukan error "sudah terbuka".
    /// WR-02 — seluruh urutan mutasi (claim → insert AttemptHistory → snapshot → delete) dibungkus SATU
    /// <c>BeginTransactionAsync</c>+<c>CommitAsync</c>; audit + SignalR di LUAR commit (warn-only).
    /// WR-03 — <c>AttemptHistory</c> di-insert HANYA bila snapshot non-empty (deferred-insert) → tidak ada
    /// baris childless/orphan saat Completed tanpa assignment/soal.</para>
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

            // Cancelled = final, TIDAK resettable (selaras controller ResetAssessment guard :4234-4235).
            if (assessment.Status == "Cancelled")
                return new RetakeResult(false, "Sesi tidak dapat direset (sudah dibatalkan).");

            // Tangkap status SEBELUM claim — menentukan apakah perlu archive snapshot.
            bool wasCompleted = assessment.Status == "Completed";

            // WR-01 (review 405): sesi yang SUDAH Open (assigned-not-started ATAU baru di-reset) tidak punya
            // skor/response untuk diarsip/dihapus — controller mengizinkan reset status Open (:4234-4235), jadi
            // perlakukan sebagai SUCCESS no-op. Ini juga menutup double-click: request ke-2 (sesi sudah di-Open
            // oleh request ke-1) tak membuat archive kedua (anti double-archive, Pitfall 1) — invariant histCount==1
            // tetap dijaga oleh wasCompleted==false + deferred-insert (WR-03) di bawah.
            if (assessment.Status == "Open")
                return new RetakeResult(true, null);

            // v32.7 RTH-01 (RTK-LOGIC-02 HIGH) — ABORT-BEFORE-DESTROY: bila masa ujian sudah tutup, retake/reset
            // mustahil berhasil (StartExam akan blok window) → JANGAN hancurkan sesi live jadi shell kosong.
            // Defense-in-depth lapis-kedua D-01 (lapis-pertama = eligibility RetakeRules.CanRetake window-aware).
            // +7h WIB byte-identik StartExam (CMPController:956). Berlaku juga jalur HC (delegasi) — cegah dead shell.
            if (assessment.ExamWindowCloseDate.HasValue && DateTime.UtcNow.AddHours(7) > assessment.ExamWindowCloseDate.Value)
                return new RetakeResult(false, "Masa ujian sudah ditutup — ujian ulang tidak bisa dijalankan.");

            // WR-02 (review 405): bungkus claim → snapshot → archive-insert → delete dalam SATU transaksi eksplisit
            // (mirror pola bulk-assign AssessmentAdminController :2196). ExecuteUpdateAsync ikut enlist di transaksi
            // pada koneksi yang sama. Audit + SignalR (langkah 5-6) SENGAJA di luar commit (warn-only side effect).
            await using var tx = await _context.Database.BeginTransactionAsync();

            // 2. CLAIM-ATOMIK DULU (anti double-archive — Pitfall 1).
            //    WHERE Status NOT IN (Cancelled, Open): Cancelled = final tak resettable;
            //    Open = sudah di-claim oleh request lain (cegah double-click double-archive) — sudah di-handle
            //    sebagai SUCCESS no-op di atas, di sini hanya defensif terhadap race antar-koneksi.
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
                    // v32.7 RTH-02 (RTK-LOGIC-01) D-03: nol-kan NomorSertifikat agar sesi non-lulus pasca-reset
                    // tidak menyandang nomor sertifikat menggantung (inflasi unique index + certCount proxy).
                    // Jalur HC ResetAssessment tercakup via delegasi (Don't Hand-Roll di controller).
                    .SetProperty(r => r.NomorSertifikat, (string?)null)
                    .SetProperty(r => r.UpdatedAt, DateTime.UtcNow));

            if (rows == 0)
            {
                // Race antar-koneksi: sesi sudah di-claim (Open) oleh request lain antara load & claim.
                // Perlakukan sebagai no-op sukses (selaras WR-01) — tak ada archive kedua dibuat.
                await tx.RollbackAsync();
                return new RetakeResult(true, null);
            }

            // 3. Jika sesi sebelumnya Completed: SNAPSHOT per-soal SEBELUM delete (D-04 retain).
            //    WR-03 (review 405): AttemptHistory di-INSERT HANYA bila snapshot non-empty (defer insert sampai
            //    questions terbukti ada) → tidak ada baris AttemptHistory childless (orphan) yang persist.
            if (wasCompleted)
            {
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
                    // Counting era-retake (UserId, Title, Category) — must-fix #3 anti-konflasi Pre/Post +
                    // D-01 snapshot-presence. v32.7 RTH-03 (D-05): via satu sumber RetakeCountingRules (kill-drift).
                    int eraRetakeArchives = await RetakeCountingRules.CountForUserAsync(
                        _context, assessment.UserId, assessment.Title, assessment.Category);

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
                    await _context.SaveChangesAsync();   // assign attemptHistory.Id sebelum builder snapshot (DALAM tx)

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
            await tx.CommitAsync();              // WR-02: commit claim+archive+delete sebagai satu unit atomik

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

            // v32.7 RTH-03 (D-05): satu sumber counting era-retake snapshot-presence (kill-drift).
            int eraRetakeArchives = await RetakeCountingRules.CountForUserAsync(
                _context, s.UserId, s.Title, s.Category);

            return RetakeRules.CanRetake(
                s.AllowRetake, s.AssessmentType, s.IsManualEntry, s.Status, s.IsPassed,
                attemptsUsed: eraRetakeArchives + 1, s.MaxAttempts, s.RetakeCooldownHours,
                s.CompletedAt, DateTime.UtcNow, s.ExamWindowCloseDate);   // v32.7 RTH-01: suplai window ke gate eligibility
        }
    }
}
