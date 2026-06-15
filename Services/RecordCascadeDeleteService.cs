using HcPortal.Data;
using HcPortal.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Services
{
    /// <summary>
    /// Phase 367 — Cascade engine penghapusan record worker dari tab Input Records.
    /// ORKESTRASI (bukan primitif baru): traversal renewal lintas TrainingRecords↔AssessmentSessions
    /// (kolom RenewsTrainingId/RenewsSessionId) dengan cycle guard. Plan 01 = bagian READ-ONLY
    /// (CascadeNode, CollectCascadeIds, BuildPreviewAsync, mirror heuristik). Plan 02 menambah ExecuteAsync (mutasi).
    ///
    /// Invariant KRITIS: preview set == execute set — keduanya pakai <see cref="CollectCascadeIds"/> yang SAMA,
    /// jadi admin lihat X di preview, hapus X persis (memperbaiki kasus Rino #3: turunan renewal tak ikut terhapus).
    ///
    /// Catatan visibilitas: CollectCascadeIds/FindMirrorCandidates public (konvensi proyek — reachable dari
    /// HcPortal.Tests tanpa InternalsVisibleTo, sama dengan CMPController:3969).
    /// </summary>
    public class RecordCascadeDeleteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecordCascadeDeleteService> _logger;
        private readonly ProtonCompletionService _protonCompletion;
        private readonly AuditLogService _auditLog;
        private readonly IWebHostEnvironment _env;

        public RecordCascadeDeleteService(
            ApplicationDbContext context,
            ILogger<RecordCascadeDeleteService> logger,
            ProtonCompletionService protonCompletion,
            AuditLogService auditLog,
            IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _protonCompletion = protonCompletion;
            _auditLog = auditLog;
            _env = env;
        }

        /// <summary>
        /// Node korban cascade untuk preview. Type ∈ {"session","training"}.
        /// IsMirrorCandidate=true → kandidat mirror legacy (#15) — read-only di preview, default tercentang
        /// (opt-out), BUKAN turunan renewal yang ter-traverse.
        /// </summary>
        public record CascadeNode(string Type, int Id, string Title, DateTime Date, string OwnerName, bool IsRoot, bool IsMirrorCandidate);

        /// <summary>
        /// Traversal BFS BERSAMA (dipakai preview DAN execute — invariant preview==execute) lintas tabel
        /// via Renews*Id, dengan cycle guard (HashSet visited). Mengembalikan (Type,Id) terurut BFS, root dulu.
        /// GOTCHA (Pitfall 2): node session → anak via RenewsSessionId; node training → anak via RenewsTrainingId.
        /// </summary>
        public async Task<List<(string Type, int Id)>> CollectCascadeIds(string rootType, int rootId)
        {
            if (rootType != "session" && rootType != "training")
                throw new ArgumentException($"rootType harus 'session' atau 'training', bukan '{rootType}'.", nameof(rootType));

            HashSet<(string, int)> visited = new();
            var ordered = new List<(string, int)>();
            var queue = new Queue<(string Type, int Id)>();
            queue.Enqueue((rootType, rootId));
            visited.Add((rootType, rootId));
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                ordered.Add(node);

                List<int> childTrainingIds;
                List<int> childSessionIds;
                if (node.Type == "session")
                {
                    childTrainingIds = await _context.TrainingRecords
                        .Where(t => t.RenewsSessionId == node.Id).Select(t => t.Id).ToListAsync();
                    childSessionIds = await _context.AssessmentSessions
                        .Where(a => a.RenewsSessionId == node.Id).Select(a => a.Id).ToListAsync();
                }
                else // "training"
                {
                    childTrainingIds = await _context.TrainingRecords
                        .Where(t => t.RenewsTrainingId == node.Id).Select(t => t.Id).ToListAsync();
                    childSessionIds = await _context.AssessmentSessions
                        .Where(a => a.RenewsTrainingId == node.Id).Select(a => a.Id).ToListAsync();
                }

                foreach (var tid in childTrainingIds)
                    if (visited.Add(("training", tid))) queue.Enqueue(("training", tid));
                foreach (var sid in childSessionIds)
                    if (visited.Add(("session", sid))) queue.Enqueue(("session", sid));
            }
            return ordered;
        }

        /// <summary>
        /// Bangun pohon korban cascade untuk preview konfirmasi (L-03). READ-ONLY invariant: ZERO
        /// RemoveRange/Add/SaveChanges — hanya query. Turunan renewal (ter-traverse) + kandidat mirror
        /// legacy (#15, IsMirrorCandidate=true). Memakai <see cref="CollectCascadeIds"/> yang SAMA dengan
        /// execute (02) → preview set == execute set.
        /// </summary>
        public async Task<List<CascadeNode>> BuildPreviewAsync(string rootType, int rootId)
        {
            var ids = await CollectCascadeIds(rootType, rootId);
            var inCascade = new HashSet<(string, int)>(ids);
            var nodes = new List<CascadeNode>();

            for (int i = 0; i < ids.Count; i++)
            {
                var (type, id) = ids[i];
                if (type == "session")
                {
                    var s = await _context.AssessmentSessions.FirstOrDefaultAsync(a => a.Id == id);
                    if (s == null) continue;
                    var owner = await _context.Users.Where(u => u.Id == s.UserId).Select(u => u.FullName).FirstOrDefaultAsync() ?? "";
                    nodes.Add(new CascadeNode("session", s.Id, s.Title, s.Schedule, owner, i == 0, false));
                }
                else
                {
                    var t = await _context.TrainingRecords.FirstOrDefaultAsync(x => x.Id == id);
                    if (t == null) continue;
                    var owner = await _context.Users.Where(u => u.Id == t.UserId).Select(u => u.FullName).FirstOrDefaultAsync() ?? "";
                    nodes.Add(new CascadeNode("training", t.Id, t.Judul ?? "", t.Tanggal, owner, i == 0, false));
                }
            }

            // Heuristik mirror legacy (#15): untuk SETIAP session korban, cari TrainingRecord mirror ±1 hari
            // milik user yang sama yang BELUM ikut cascade (bukan turunan renewal). Tampil sebagai opt-out checkbox.
            foreach (var session in await SessionsOf(ids))
            {
                foreach (var m in await FindMirrorCandidates(session))
                {
                    if (inCascade.Contains(("training", m.Id))) continue;
                    if (nodes.Any(n => n.Type == "training" && n.Id == m.Id)) continue;
                    var owner = await _context.Users.Where(u => u.Id == m.UserId).Select(u => u.FullName).FirstOrDefaultAsync() ?? "";
                    nodes.Add(new CascadeNode("training", m.Id, m.Judul ?? "", m.Tanggal, owner, false, true));
                }
            }

            return nodes;
        }

        private async Task<List<AssessmentSession>> SessionsOf(List<(string Type, int Id)> ids)
        {
            var sessionIds = ids.Where(n => n.Type == "session").Select(n => n.Id).ToList();
            if (sessionIds.Count == 0) return new List<AssessmentSession>();
            return await _context.AssessmentSessions.Where(a => sessionIds.Contains(a.Id)).ToListAsync();
        }

        /// <summary>
        /// Kandidat mirror legacy (#15) untuk sebuah session: TrainingRecord milik user yang sama, judul match
        /// (sama persis ATAU "Assessment: " + Title), tanggal ±1 hari. BEDA dari guard duplikat #12 yang EXACT.
        /// Dipakai preview DAN nanti execute (02) untuk validasi mirror-ID milik user yang sama (server-side, V5/IDOR —
        /// jangan percaya checkbox client).
        /// </summary>
        public async Task<List<TrainingRecord>> FindMirrorCandidates(AssessmentSession session)
        {
            return await _context.TrainingRecords
                .Where(t => t.UserId == session.UserId
                    && (t.Judul == session.Title || t.Judul == "Assessment: " + session.Title)
                    && t.Tanggal >= session.Schedule.AddDays(-1)
                    && t.Tanggal <= session.Schedule.AddDays(1))
                .ToListAsync();
        }

        /// <summary>Hasil eksekusi cascade. Success=false → ErrorMessage GENERIK (no info leak, V7).</summary>
        public record CascadeResult(bool Success, int DeletedCount, List<int> DeletedSessionIds, List<int> DeletedTrainingIds, string? ErrorMessage);

        /// <summary>
        /// EKSEKUSI cascade (mutasi, 1-transaction). Hapus SEMUA node hasil <see cref="CollectCascadeIds"/>
        /// (SAMA dengan preview → invariant preview==execute) + seluruh artefak per node.
        /// Per node AssessmentSession = parity verbatim gold-standard DeleteAssessment (urutan RemoveRange Restrict-FK)
        /// + 4 delta 367: #8 LinkedSessionId null-clear, L-04 PendingProtonBypass soft-cancel, #9 RemoveExamOriginAsync,
        /// #6 UserNotifications eksak-match. File sertifikat (#19) dihapus POST-commit warn-only (L-08). AuditLog 1 entri.
        /// </summary>
        /// <param name="mirrorTrainingIdsToInclude">Mirror-ID dari checkbox client — DIVALIDASI server-side (V5/IDOR).</param>
        public async Task<CascadeResult> ExecuteAsync(string rootType, int rootId, IEnumerable<int> mirrorTrainingIdsToInclude, string actorId, string actorName)
        {
            var nodes = await CollectCascadeIds(rootType, rootId);
            var sessionNodeIds = nodes.Where(n => n.Type == "session").Select(n => n.Id).ToList();
            var trainingNodeIds = nodes.Where(n => n.Type == "training").Select(n => n.Id).ToList();

            // Capture root title SEBELUM hapus (untuk audit post-commit).
            string rootTitle = rootType == "session"
                ? (await _context.AssessmentSessions.Where(a => a.Id == rootId).Select(a => a.Title).FirstOrDefaultAsync() ?? "")
                : (await _context.TrainingRecords.Where(t => t.Id == rootId).Select(t => t.Judul).FirstOrDefaultAsync() ?? "");

            // Validasi mirror-ID (V5/IDOR): hanya yang benar-benar kandidat mirror untuk session node milik user sama.
            var requestedMirrors = (mirrorTrainingIdsToInclude ?? Enumerable.Empty<int>()).Distinct().ToHashSet();
            var validMirrorIds = new HashSet<int>();
            if (requestedMirrors.Count > 0)
            {
                var sessionsForMirror = await _context.AssessmentSessions.Where(a => sessionNodeIds.Contains(a.Id)).ToListAsync();
                foreach (var session in sessionsForMirror)
                    foreach (var c in await FindMirrorCandidates(session))
                        if (requestedMirrors.Contains(c.Id) && !trainingNodeIds.Contains(c.Id))
                            validMirrorIds.Add(c.Id);
            }

            var certPaths = new List<string>();
            var deletedSessionIds = new List<int>();
            var deletedTrainingIds = new List<int>();

            try
            {
                using var tx = await _context.Database.BeginTransactionAsync();

                var sessionEntities = await _context.AssessmentSessions.Where(a => sessionNodeIds.Contains(a.Id)).ToListAsync();
                foreach (var session in sessionEntities)
                {
                    int id = session.Id;

                    // --- Gold-standard artefak per node (urutan Restrict-FK, parity DeleteAssessment) ---
                    var editLogs = await _context.AssessmentEditLogs.Where(e => e.AssessmentSessionId == id).ToListAsync();
                    if (editLogs.Any()) _context.AssessmentEditLogs.RemoveRange(editLogs);

                    var pkgResponses = await _context.PackageUserResponses.Where(r => r.AssessmentSessionId == id).ToListAsync();
                    if (pkgResponses.Any()) _context.PackageUserResponses.RemoveRange(pkgResponses);

                    var attemptHistory = await _context.AssessmentAttemptHistory.Where(h => h.SessionId == id).ToListAsync();
                    if (attemptHistory.Any()) _context.AssessmentAttemptHistory.RemoveRange(attemptHistory);

                    var pkgAssignments = await _context.UserPackageAssignments.Where(a => a.AssessmentSessionId == id).ToListAsync();
                    if (pkgAssignments.Any()) _context.UserPackageAssignments.RemoveRange(pkgAssignments);

                    var packages = await _context.AssessmentPackages
                        .Include(p => p.Questions).ThenInclude(q => q.Options)
                        .Where(p => p.AssessmentSessionId == id).ToListAsync();
                    foreach (var pkg in packages)
                    {
                        foreach (var q in pkg.Questions) _context.PackageOptions.RemoveRange(q.Options);
                        _context.PackageQuestions.RemoveRange(pkg.Questions);
                    }
                    if (packages.Any()) _context.AssessmentPackages.RemoveRange(packages);
                    // CATATAN: gambar SOAL (ImagePath) = ranah Phase 366 (endpoint tab 1, plan 05) — Opsi B separasi. TIDAK di-collect di engine.

                    // --- Delta #8: null-clear LinkedSessionId pasangan (gain-score CMP tidak putus) ---
                    var partners = await _context.AssessmentSessions.Where(a => a.LinkedSessionId == id).ToListAsync();
                    foreach (var p in partners) p.LinkedSessionId = null;

                    // --- Delta L-04: PendingProtonBypass soft-cancel (BUKAN Remove — jejak audit) ---
                    var pendingToCancel = await _context.PendingProtonBypasses
                        .Where(p => p.LinkedAssessmentSessionId == id && p.Status != "Dibatalkan").ToListAsync();
                    foreach (var p in pendingToCancel) { p.Status = "Dibatalkan"; p.ResolvedAt = DateTime.UtcNow; }

                    // --- Delta #6: UserNotifications eksak-match (OQ-1 konservatif — pola terbukti aktif + 2 dormant) ---
                    var notifUrls = new[] { $"/CMP/StartExam/{id}", $"/CMP/AssessmentResults/{id}", $"/CMP/AssessmentDetails/{id}" };
                    var orphanNotifs = await _context.UserNotifications.Where(n => n.ActionUrl != null && notifUrls.Contains(n.ActionUrl)).ToListAsync();
                    if (orphanNotifs.Any()) _context.UserNotifications.RemoveRange(orphanNotifs);

                    // --- #19: collect file sertifikat manual SEBELUM Remove ---
                    if (!string.IsNullOrEmpty(session.ManualSertifikatUrl)) certPaths.Add(session.ManualSertifikatUrl!);

                    // --- Delta #9: cabut penanda Proton Origin='Exam' (Interview/Bypass kebal). A4: helper SaveChanges
                    //     internal → flush partial DALAM tx (belum commit → rollback tetap utuh). ---
                    if (session.ProtonTrackId.HasValue)
                        await _protonCompletion.RemoveExamOriginAsync(session.UserId, session.ProtonTrackId.Value);

                    _context.AssessmentSessions.Remove(session);
                    deletedSessionIds.Add(id);
                }

                // --- Node training (notif = 0: tidak ada pola ActionUrl record-bound) ---
                var trainingEntities = await _context.TrainingRecords.Where(t => trainingNodeIds.Contains(t.Id)).ToListAsync();
                foreach (var record in trainingEntities)
                {
                    if (!string.IsNullOrEmpty(record.SertifikatUrl)) certPaths.Add(record.SertifikatUrl!);
                    _context.TrainingRecords.Remove(record);
                    deletedTrainingIds.Add(record.Id);
                }

                // --- Mirror training valid (opt-in, ter-validasi) ---
                if (validMirrorIds.Count > 0)
                {
                    var mirrorEntities = await _context.TrainingRecords.Where(t => validMirrorIds.Contains(t.Id)).ToListAsync();
                    foreach (var m in mirrorEntities)
                    {
                        if (!string.IsNullOrEmpty(m.SertifikatUrl)) certPaths.Add(m.SertifikatUrl!);
                        _context.TrainingRecords.Remove(m);
                        deletedTrainingIds.Add(m.Id);
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                // using var tx → auto-rollback saat dispose. Pesan GENERIK ke caller (V7), detail hanya ke logger.
                _logger.LogError(ex, "Cascade delete gagal untuk root [{RootType}:{RootId}] — rollback.", rootType, rootId);
                return new CascadeResult(false, 0, new List<int>(), new List<int>(), "Gagal menghapus record. Silakan coba lagi.");
            }

            // --- POST-commit: hapus file sertifikat fisik (confined webroot, warn-only per file, L-08/V12) ---
            foreach (var url in certPaths.Distinct())
            {
                try
                {
                    var path = Path.Combine(_env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Gagal hapus file sertifikat {Url} post-commit (warn-only).", url); }
            }

            // --- POST-commit: AuditLog 1 entri/operasi (warn-only, pola gold-standard) ---
            var allDeletedIds = deletedSessionIds.Concat(deletedTrainingIds).ToList();
            try
            {
                await _auditLog.LogAsync(actorId, actorName, "CascadeDelete",
                    $"Cascade delete root [{rootType}:{rootId} '{rootTitle}'] — {allDeletedIds.Count} record dihapus (root + turunan/mirror): [{string.Join(",", allDeletedIds)}]",
                    rootId, rootType == "session" ? "AssessmentSession" : "TrainingRecord");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log cascade delete gagal (warn-only)."); }

            return new CascadeResult(true, allDeletedIds.Count, deletedSessionIds, deletedTrainingIds, null);
        }
    }
}
