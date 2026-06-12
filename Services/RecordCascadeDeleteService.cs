using HcPortal.Data;
using HcPortal.Models;
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

        public RecordCascadeDeleteService(
            ApplicationDbContext context,
            ILogger<RecordCascadeDeleteService> logger)
        {
            _context = context;
            _logger = logger;
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
    }
}
